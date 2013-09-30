using System;
using System.Collections.Generic;
using System.Transactions;
using Composable.System.Linq;
using log4net;

namespace Composable.KeyValueStorage
{
    public partial class DocumentDbSession
    {        
        internal class DocumentItem
        {
            private static ILog Log = LogManager.GetLogger(typeof(DocumentItem));
            private readonly IDocumentDb _backingStore;
            private DocumentKey Key { get; set; }

            public DocumentItem(DocumentKey key, IDocumentDb backingStore)
            {
                _backingStore = backingStore;
                Key = key;
            }

            private Object Document { get; set; }
            public bool IsDeleted { get; private set; }
            private bool IsInBackingStore { get; set; }

            private bool ScheduledForAdding { get { return !IsInBackingStore && !IsDeleted && Document != null; } }
            private bool ScheduledForRemoval { get { return IsInBackingStore && IsDeleted; } }
            private bool ScheduledForUpdate { get { return IsInBackingStore && !IsDeleted; } }

            public void Delete()
            {
                IsDeleted = true;
            }

            public void Save(object document)
            {
                if(document == null)
                {
                    throw new ArgumentNullException("document");
                }
                Document = document;
                IsDeleted = false;
            }

            public void DocumentLoadedFromBackingStore(object document)
            {
                if (document == null)
                {
                    throw new ArgumentNullException("document");
                }
                Document = document;
                IsInBackingStore = true;
            }

            private bool IsCommitting { get; set; }
            public void CommitChangesToBackingStore()
            {
                //Avoid reentrancy issues.
                if(IsCommitting)
                {
                    Log.DebugFormat("Exiting to avoid reentrancy Transaction.Current: {0}", Transaction.Current.LogText());
                    return;
                }
                IsCommitting = true;
                Log.DebugFormat("Committing Transaction.Current: {0}", Transaction.Current.LogText());
                using(new DisposeAction(() => IsCommitting = false))//Reset IsCommitting to false once we are done committing.
                {
                    if(ScheduledForAdding)
                    {
                        Log.DebugFormat("Adding To Backing Store Transaction.Current: {0}", Transaction.Current.LogText());
                        IsInBackingStore = true;
                        _backingStore.Add(Key.Id, Document);
                    }
                    else if(ScheduledForRemoval)
                    {
                        Log.DebugFormat("Removing from Backing Store Transaction.Current: {0}", Transaction.Current.LogText());
                        var docType = Document.GetType();
                        Document = null;
                        IsInBackingStore = false;
                        _backingStore.Remove(Key.Id, docType);

                    }
                    else if(ScheduledForUpdate)
                    {
                        Log.DebugFormat("Updating in Backing Store Transaction.Current: {0}", Transaction.Current.LogText());
                        _backingStore.Update(Seq.Create(new KeyValuePair<string, object>(Key.Id, Document)));
                    }
                }
            }            

            //todo: (Rename?) and move to somewhere where it is usable for everyone. Not done now to avoid bumping everything that uses composable.core.
            private class DisposeAction : IDisposable
            {
                private readonly Action _action;

                public DisposeAction(Action action)
                {
                    _action = action;
                }

                public void Dispose()
                {
                    _action();
                }
            }
        }

    }
}
