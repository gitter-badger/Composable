using System;
using System.Runtime.Remoting.Messaging;
using System.Transactions;

namespace Composable.System.Transactions
{
    public class DistributedTransactionScope : IDisposable
    {
        private static DistributedTransactionScopeImplBase CurrentScope
        {
            get
            {
                var result = (DistributedTransactionScopeImplBase)CallContext.GetData("DistributedTransactionScope_Current");
                if (result != null && result.IsActive)
                {
                    return result;
                }
                return CurrentScope = null;
            }
            set { CallContext.SetData("DistributedTransactionScope_Current", value); }
        }

        private readonly DistributedTransactionScopeImplBase _impl;

        public DistributedTransactionScope()
        {
            if(CurrentScope != null)
            {
                _impl = CurrentScope = new InnerDistributedTransactionScopeImpl(CurrentScope);
            }
            else
            {
                _impl = CurrentScope = new DistributedTransactionScopeImpl();
            }
        }

        public void Dispose()
        {
            _impl.Dispose();
        }

        public void Complete()
        {
            _impl.Complete();
        }

        private abstract class DistributedTransactionScopeImplBase : IDisposable
        {
            public abstract void Dispose();
            public abstract void Complete();
            public abstract bool IsActive { get; }
        }

        private class InnerDistributedTransactionScopeImpl : DistributedTransactionScopeImplBase
        {
            private readonly DistributedTransactionScopeImplBase _outer;

            public InnerDistributedTransactionScopeImpl(DistributedTransactionScopeImplBase outer)
            {
                _outer = outer;
            }

            override public void Dispose() {}

            override public void Complete() {}

            override public bool IsActive { get { return _outer.IsActive; } }
        }


        private class DistributedTransactionScopeImpl : DistributedTransactionScopeImplBase, IEnlistmentNotification
        {
            private readonly TransactionScope _scope;

            public DistributedTransactionScopeImpl()
            {
                _scope = new TransactionScope();
                Id = Guid.NewGuid();
                Transaction.Current.EnlistDurable(Id, this, EnlistmentOptions.None);
            }

            override public void Dispose()
            {
                _scope.Dispose();
            }

            override public void Complete()
            {
                _scope.Complete();
            }

            public Guid Id { get; set; }

            override public bool IsActive { get { return !CommitCalled && !RollBackCalled && !InDoubtCalled; } }

            private bool PrepareCalled { get; set; }
            private bool CommitCalled { get; set; }
            private bool RollBackCalled { get; set; }
            private bool InDoubtCalled { get; set; }

            public void Prepare(PreparingEnlistment preparingEnlistment)
            {
                PrepareCalled = true;
                preparingEnlistment.Prepared();
            }

            void IEnlistmentNotification.Commit(Enlistment enlistment)
            {
                CommitCalled = true;
                enlistment.Done();
            }

            void IEnlistmentNotification.Rollback(Enlistment enlistment)
            {
                RollBackCalled = true;
                enlistment.Done();
            }

            void IEnlistmentNotification.InDoubt(Enlistment enlistment)
            {
                InDoubtCalled = true;
                enlistment.Done();
            }
        }
    }
}
