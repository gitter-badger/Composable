using Composable.CQRS.ViewModels;
using Manpower.Applications.JobCommunication.GlobalEvents;
using Manpower.Applications.JobCommunication.ViewModels;
using Manpower.Applications.JobCommunication.ViewModels.Services;

namespace Manpower.Applications.JobCommunication.ViewModelsUpdaters
{
    public class JobCommunicationCandidateViewModelUpdater : ViewModelUpdater<JobCommunicationCandidateViewModelUpdater, JobCommunicationCandidateViewModel, IJobCommunicationCandidateEvent, IViewModelsSession>
    {
        public JobCommunicationCandidateViewModelUpdater(IViewModelsSession session) :
            base(session, creationEvent: typeof(IJobCommunicationCandidateCreatedEvent))
        {
            RegisterHandlers()
                .For<IJobCommunicationCandidateCreatedEvent>(e => {
                                                                      Model = new JobCommunicationCandidateViewModel(e.AggregateRootId) { CVId = e.CVId };
                                                                  });
        }
    }
}