using System;
using System.Transactions;
using Composable.System;
using Composable.UnitsOfWork;

namespace Composable
{
    internal static class LogFormattingHelpers
    {
        public static string LogText(this Transaction me)
        {
            if(me == null)
            {
                return "null";
            }
            try
            {
                return "{{ localId: {0}, distributedId: {1}, status: {2} }}".FormatWith(me.TransactionInformation.LocalIdentifier,
                    me.TransactionInformation.DistributedIdentifier,
                    me.TransactionInformation.Status);
            }
            catch(ObjectDisposedException ex)
            {
                return "DISPOSED";
            }
        }

        public static string LogText(this IUnitOfWork me)
        {
            if (me == null)
            {
                return "null";
            }
            return "{{ id: {0} }}".FormatWith(me.Id);
        }
    }
}