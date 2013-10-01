using System;
using System.Collections.Generic;
using System.Transactions;
using Composable.DDD;
using Composable.System.Linq;
using System.Linq;
using Composable.SystemExtensions.Threading;
using Composable.UnitsOfWork;
using log4net;
using Composable.System;
using NServiceBus;

namespace Composable.KeyValueStorage
{
    public partial class DocumentDbSession : IDocumentDbSession, IUnitOfWorkParticipant, IEnlistmentNotification
    {
        [ThreadStatic]
        internal static bool UseUpdateLock;

        private readonly InMemoryObjectStore _idMap = new InMemoryObjectStore();

        public readonly IDocumentDb BackingStore;
        public readonly IDocumentDbSessionInterceptor Interceptor;        
        public readonly ISingleContextUseGuard UsageGuard;

        private readonly IDictionary<DocumentKey, DocumentItem> _handledDocuments = new Dictionary<DocumentKey, DocumentItem>(); 

        private static readonly ILog Log = LogManager.GetLogger(typeof(DocumentDbSession));

        public DocumentDbSession(IDocumentDb backingStore, ISingleContextUseGuard usageGuard, IDocumentDbSessionInterceptor interceptor)
        {
            UsageGuard = usageGuard;
            BackingStore = backingStore;
            Interceptor = interceptor;
            JoinAmbientTransactionIfRequired();
        }

        public IObservable<IDocumentUpdated> DocumentUpdated { get { return BackingStore.DocumentUpdated; } }

        public virtual bool TryGet<TValue>(object key, out TValue value)
        {
            return TryGetInternal(key, typeof(TValue), out value);
        }

        private bool TryGetInternal<TValue>(object key, Type documentType, out TValue value)
        {
            if(documentType.IsInterface)
            {
                throw new ArgumentException("You cannot query by id for an interface type. There is no guarantee of uniqueness");
            }
            CheckContextAndJoinAnyAmbientTransaction();

            if (_idMap.TryGet(key, out value) && documentType.IsAssignableFrom(value.GetType()))
            {
                return true;
            }

            var documentItem = GetDocumentItem(key, documentType);
            if(!documentItem.IsDeleted && BackingStore.TryGet(key, out value) && documentType.IsAssignableFrom(value.GetType()))
            {
                OnInitialLoad(key, value);
                return true;
            }

            return false;
        }

        private DocumentItem GetDocumentItem(object key, Type documentType)
        {
            DocumentItem doc;
            var documentKey = new DocumentKey(key, documentType);

            if (!_handledDocuments.TryGetValue(documentKey, out doc))
            {                
                doc = new DocumentItem(documentKey, BackingStore);
                _handledDocuments.Add(documentKey, doc);
            }
            return doc;
        }

        private void OnInitialLoad(object key, object value)
        {
            _idMap.Add(key, value);
            GetDocumentItem(key, value.GetType()).DocumentLoadedFromBackingStore(value);
            if (Interceptor != null)
                Interceptor.AfterLoad(value);
        }

        public virtual TValue GetForUpdate<TValue>(object key)
        {
            CheckContextAndJoinAnyAmbientTransaction();
            using(new UpdateLock())
            {
                return Get<TValue>(key);
            }
        }

        public virtual bool TryGetForUpdate<TValue>(object key, out TValue value)
        {
            CheckContextAndJoinAnyAmbientTransaction();
            using (new UpdateLock())
            {
                return TryGet(key, out value);
            }
        }

        private class UpdateLock : IDisposable
        {
            public UpdateLock()
            {
                UseUpdateLock = true;
            }

            public void Dispose()
            {
                UseUpdateLock = false;
            }
        }

        public IEnumerable<TValue> Get<TValue>(IEnumerable<Guid> ids) where TValue : IHasPersistentIdentity<Guid>
        {
            CheckContextAndJoinAnyAmbientTransaction();

            var stored = BackingStore.GetAll<TValue>(ids);
            
            stored.Where(document => !_idMap.Contains(typeof(TValue), document.Id))
                .ForEach(unloadedDocument => OnInitialLoad(unloadedDocument.Id, unloadedDocument));
            
            var results = _idMap.Select(pair => pair.Value).OfType<TValue>();
            var missingDocuments = ids.Where(id => !results.Any(result => result.Id == id)).ToArray();
            if (missingDocuments.Any())
            {
                throw new NoSuchDocumentException(missingDocuments.First(), typeof(TValue));
            }
            return results;
        }

        public virtual TValue Get<TValue>(object key)
        {
            CheckContextAndJoinAnyAmbientTransaction();
            TValue value;
            if(TryGet(key, out value))
            {
                return value;
            }

            throw new NoSuchDocumentException(key, typeof(TValue));
        }

        public virtual void Save<TValue>(object id, TValue value)
        {
            CheckContextAndJoinAnyAmbientTransaction();            

            TValue ignored;
            if (TryGetInternal(id, value.GetType(), out ignored))
            {
                throw new AttemptToSaveAlreadyPersistedValueException(id, value);
            }

            var documentItem = GetDocumentItem(id, value.GetType());
            documentItem.Save(value);

            _idMap.Add(id, value);
            if(!IsPartOfUnitOfWorkOrTransaction)
            {
                DebugLogWithUnitInformation("Save");
                documentItem.CommitChangesToBackingStore();
            }else
            {
                DebugLogWithUnitInformation("Postponed Save");
            }            
        }

        public virtual void Save<TEntity>(TEntity entity) where TEntity : IHasPersistentIdentity<Guid>
        {
            CheckContextAndJoinAnyAmbientTransaction();
            if(entity.Id.Equals(Guid.Empty))
            {
                throw new DocumentIdIsEmptyGuidException();
            }
            Save(entity.Id, entity);
        }

        public virtual void Delete<TEntity>(TEntity entity) where TEntity : IHasPersistentIdentity<Guid>
        {
            CheckContextAndJoinAnyAmbientTransaction();
            Delete<TEntity>(entity.Id);
        }

        public virtual void Delete<T>(object id)
        {
            CheckContextAndJoinAnyAmbientTransaction();
            T ignored;
            if(!TryGet(id, out ignored))
            {
                throw new NoSuchDocumentException(id, typeof(T));
            }

            var documentItem = GetDocumentItem(id, typeof(T));
            documentItem.Delete();

            _idMap.Remove<T>(id);
            if (!IsPartOfUnitOfWorkOrTransaction)
            {
                DebugLogWithUnitInformation("Delete");
                documentItem.CommitChangesToBackingStore();
            }
            else
            {
                DebugLogWithUnitInformation("Postponed Delete");
            }            
        }

        public virtual void SaveChanges()
        {
            CheckContextAndJoinAnyAmbientTransaction();
            if (!IsPartOfUnitOfWorkOrTransaction)
            {
                DebugLogWithUnitInformation("SaveChanges");
                InternalSaveChanges();
            }else
            {
                DebugLogWithUnitInformation("Postponed SaveChanges");
            }
        }

        private void InternalSaveChanges()
        {
            DebugLogWithUnitInformation("InternalSaveChanges");
            _handledDocuments.ForEach(p => p.Value.CommitChangesToBackingStore());
        }

        private void DebugLogWithUnitInformation(string message)
        {
            Log.DebugFormat("{0} Unit of work: {1}, _ambientTransaction: {2}, Transaction.Current {3} Id: {4}", message, _unitOfWork.LogText(), _ambientTransaction.LogText(), Transaction.Current.LogText(), _id);            
        }

        public virtual IEnumerable<T> GetAll<T>() where T : IHasPersistentIdentity<Guid>
        {
            CheckContextAndJoinAnyAmbientTransaction();
            var stored = BackingStore.GetAll<T>();
            stored.Where(document => !_idMap.Contains(typeof (T), document.Id))
                .ForEach(unloadedDocument => OnInitialLoad(unloadedDocument.Id, unloadedDocument));
            return _idMap.Select(pair => pair.Value).OfType<T>();
        }        


        public virtual void Dispose()
        {
            UsageGuard.AssertNoContextChangeOccurred(this);
            //Can be called before the transaction commits....
            //_idMap.Clear();
        }

        public override string ToString()
        {
            return "{0}: {1}".FormatWith(_id, GetType().FullName);
        }

        private IUnitOfWork _unitOfWork;
        private readonly Guid _id = Guid.NewGuid();        


        IUnitOfWork IUnitOfWorkParticipant.UnitOfWork { get { return _unitOfWork; } }
        Guid IUnitOfWorkParticipant.Id { get { return _id; } }

        void IUnitOfWorkParticipant.Join(IUnitOfWork unit)
        {
            CheckContextAndJoinAnyAmbientTransaction();
            _unitOfWork = unit;
        }

        void IUnitOfWorkParticipant.Commit(IUnitOfWork unit)
        {
            InternalSaveChanges();
            _unitOfWork = null;
        }

        void IUnitOfWorkParticipant.Rollback(IUnitOfWork unit)
        {
            _unitOfWork = null;
        }

        private bool _isInTransaction;
        private Transaction _ambientTransaction;

        private void CheckContextAndJoinAnyAmbientTransaction()
        {
            UsageGuard.AssertNoContextChangeOccurred(this);
            JoinAmbientTransactionIfRequired();
        }

        private void JoinAmbientTransactionIfRequired()
        {
            if(!_isInTransaction && Transaction.Current != null)
            {
                DebugLogWithUnitInformation("Joining Transaction {0}".FormatWith(Transaction.Current.LogText()));
                //Transaction.Current.EnlistDurable(_id, this, EnlistmentOptions.EnlistDuringPrepareRequired);
                //Transaction.Current.EnlistDurable(_id, this, EnlistmentOptions.None);
                Transaction.Current.EnlistVolatile(this, EnlistmentOptions.EnlistDuringPrepareRequired);
                //Transaction.Current.EnlistVolatile(this, EnlistmentOptions.None);
                _isInTransaction = true;
                //_ambientTransaction = Transaction.Current.Clone();
                _ambientTransaction = Transaction.Current.Clone();

                _ambientTransaction.TransactionCompleted += (sender, args) => DebugLogWithUnitInformation("ambient transaction completed: {0}".FormatWith(args.Transaction.LogText()));
                Transaction.Current.TransactionCompleted += (sender, args) => DebugLogWithUnitInformation("source transaction completed: {0}".FormatWith(args.Transaction.LogText()));

                DebugLogWithUnitInformation("Joined Transaction");
            }else if(_isInTransaction && Transaction.Current != _ambientTransaction)
            {
                throw new Exception("WTF");
            }
        }

        private bool IsPartOfUnitOfWorkOrTransaction{get { return _unitOfWork != null || _isInTransaction; }}
        

        void IEnlistmentNotification.Prepare(PreparingEnlistment preparingEnlistment)
        {
            DebugLogWithUnitInformation("IEnlistmentNotification.Prepare");
            try
            {
                DebugLogWithUnitInformation("Before new TransactionScope( _ambientTransaction)");
                using(var scope = new TransactionScope( _ambientTransaction))
                {
                    DebugLogWithUnitInformation("Within new TransactionScope( _ambientTransaction)");
                    InternalSaveChanges();
                    scope.Complete();
                }
                //preparingEnlistment.Prepared();
                preparingEnlistment.Done();
                _isInTransaction = false;
                //_ambientTransaction.Dispose();
                _ambientTransaction = null;
            }
            catch(Exception exception)
            {
                Log.Error("DTC transaction prepare phase failed", exception);
                DebugLogWithUnitInformation("Before preparingEnlistment.ForceRollback(exception)");
                preparingEnlistment.ForceRollback(exception);
                //preparingEnlistment.Done();
                 DebugLogWithUnitInformation("After preparingEnlistment.ForceRollback(exception)");
                _isInTransaction = false;
                //DebugLogWithUnitInformation("Before _ambientTransaction.Dispose()");
                //_ambientTransaction.Dispose();
                //DebugLogWithUnitInformation("After _ambientTransaction.Dispose()");
                _ambientTransaction = null;
            }
        }

        void IEnlistmentNotification.Commit(Enlistment enlistment)
        {
            DebugLogWithUnitInformation("IEnlistmentNotification.Commit");
            _isInTransaction = false;
            //_ambientTransaction.Dispose();
            _ambientTransaction = null;
            enlistment.Done();
        }

        void IEnlistmentNotification.Rollback(Enlistment enlistment)
        {
            DebugLogWithUnitInformation("IEnlistmentNotification.Rollback");
            ((IUnitOfWorkParticipant)this).Rollback(_unitOfWork);
            _isInTransaction = false;
            //_ambientTransaction.Dispose();
            _ambientTransaction = null;
            enlistment.Done();
        }

        void IEnlistmentNotification.InDoubt(Enlistment enlistment)
        {
            DebugLogWithUnitInformation("IEnlistmentNotification.InDoubt");
            _isInTransaction = false;
            //_ambientTransaction.Dispose();
            _ambientTransaction = null;
            enlistment.Done();
        }
    }
}