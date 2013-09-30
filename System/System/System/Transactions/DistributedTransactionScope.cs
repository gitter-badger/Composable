using System;
using System.Transactions;

namespace Composable.System.Transactions
{
    public class DistributedTransactionScope : IDisposable
    {
        private readonly TransactionScope _scope;

        public DistributedTransactionScope()
        {
            _scope = new TransactionScope();
            var forceDistributed1 = new ForceDistributionParticipant();
            Transaction.Current.EnlistDurable(forceDistributed1.Id, forceDistributed1, EnlistmentOptions.None);
        }
        public void Dispose()
        {
            _scope.Dispose();
        }

        public void Complete()
        {
            _scope.Complete();
        }

        private class ForceDistributionParticipant : IEnlistmentNotification
        {
            public Guid Id { get; set; }

            public ForceDistributionParticipant()
            {
                Id = Guid.NewGuid();
            }
            public void Prepare(PreparingEnlistment preparingEnlistment)
            {
                preparingEnlistment.Done();
            }

            public void Commit(Enlistment enlistment)
            {
                enlistment.Done();
            }

            public void Rollback(Enlistment enlistment)
            {
                enlistment.Done();
            }

            public void InDoubt(Enlistment enlistment)
            {
                enlistment.Done();
            }
        }
    }
}