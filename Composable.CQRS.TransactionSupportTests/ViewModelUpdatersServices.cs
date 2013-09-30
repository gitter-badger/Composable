using Composable.KeyValueStorage;
using Composable.SystemExtensions.Threading;
using JetBrains.Annotations;

namespace Manpower.Applications.JobCommunication.ViewModelsUpdaters.Services
{
    public interface IViewModelsUpdaterSession : IDocumentDbSession {}

    [UsedImplicitly]
    public class ViewModelsUpdaterSession : DocumentDbSession, IViewModelsUpdaterSession
    {
        public ViewModelsUpdaterSession(IDocumentDb backingStore, ISingleContextUseGuard usageGuard, IDocumentDbSessionInterceptor interceptor)
            : base(backingStore, usageGuard, interceptor) {}
    }
}
