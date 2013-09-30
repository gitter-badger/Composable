using System;
using Composable.DDD;

namespace Manpower.Applications.JobCommunication.ViewModels
{
    public class JobCommunicationCandidateViewModel : PersistentEntity<JobCommunicationCandidateViewModel>
    {
        public JobCommunicationCandidateViewModel(Guid aggregateRootId) : base(aggregateRootId)
        {            
        }

        public Guid CVId { get; set; }
    }
}