#region usings

using System;
using System.Diagnostics.Contracts;
using System.Transactions;

#endregion

namespace Composable.System.Transactions
{
    ///<summary>Simple utility class for executing a<see cref="Action"/> within a <see cref="TransactionScope"/></summary>
    public static class InTransaction
    {
        [ThreadStatic]
        private static DistributedTransactionScope _currentScope;

        ///<summary>Runs the supplied action within a <see cref="TransactionScope"/></summary>
        public static void Execute(Action action)
        {
            using(var transaction = new DistributedTransactionScope())
            {
                _currentScope = transaction;
                action();
                transaction.Complete();
            }
        }
    }
}