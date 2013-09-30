using Composable.KeyValueStorage;
using Composable.SystemExtensions.Threading;
using JetBrains.Annotations;

namespace Manpower.Applications.JobCommunication.ViewModels.Services
{
    public interface IViewModelsSession : IDocumentDbSession {}

    [UsedImplicitly]
    public class ViewModelsSession : DocumentDbSession, IViewModelsSession
    {
        public ViewModelsSession(IDocumentDb backingStore, ISingleContextUseGuard usageGuard, IDocumentDbSessionInterceptor interceptor)
            : base(backingStore, usageGuard, interceptor) {}
    }
}
