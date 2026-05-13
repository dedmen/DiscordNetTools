using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordNetTools.Context
{
    public class SentryInteractionContext : InteractionContext.IInteractionContextWithExitHandler
    {
        ITransactionTracer Transaction;

        public void OnBeforeClose(InteractionContext.ContextCloseReason reason)
        {
            switch (reason)
            {
                case InteractionContext.ContextCloseReason.Finished:
                    Transaction.Finish(SpanStatus.Ok);
                    break;
                case InteractionContext.ContextCloseReason.Timeout:
                    Transaction.Finish(SpanStatus.DeadlineExceeded);
                    break;
            }
        }
        public ITransactionTracer GetOrCreate(string operationName)
        {
            if (Transaction != null) return Transaction;

            Transaction = SentrySdk.StartTransaction("UserContext", operationName);
            return Transaction;
        }
    }
}
