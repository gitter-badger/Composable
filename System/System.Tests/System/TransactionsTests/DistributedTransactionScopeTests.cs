using System.Transactions;
using Composable.System.Transactions;
using NUnit.Framework;

namespace Composable.Tests.System.TransactionsTests
{
    [TestFixture]
    public class DistributedTransactionScopeTests
    {
        [Test]
        public void DoesNotFlipWhenNested()
        {
            using(var t1 = new DistributedTransactionScope())
            {
                using (var t2 = new DistributedTransactionScope())
                {
                    using (var t3 = new DistributedTransactionScope())
                    {
                        t3.Complete();
                    }
                    t2.Complete();
                }
                t1.Complete();
            }

            using (var t1 = new TransactionScope())
            {
                using (var t2 = new DistributedTransactionScope())
                {
                    using (var t3 = new DistributedTransactionScope())
                    {
                        t3.Complete();
                    }
                    t2.Complete();
                }
                t1.Complete();
            }
        }
    }
}