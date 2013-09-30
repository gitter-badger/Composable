using System;
using Composable.CQRS.EventSourcing;
using Manpower.Applications.JobCommunication.Events;
using Manpower.Applications.JobCommunication.GlobalEvents;

namespace Manpower.Applications.JobCommunication.Domain
{
#pragma warning disable 618
    public class JobCommunicationCandidate : AggregateRoot<JobCommunicationCandidate>
#pragma warning restore 618
    {

        public Guid CVId { get; private set; }

        public static JobCommunicationCandidate Create(Guid id, Guid cvId)
        {
            var result = new JobCommunicationCandidate();
            result.ApplyEvent(new JobCommunicationCandidateCreatedEvent(id, cvId));
            return result;
        }
        public void Apply(IJobCommunicationCandidateCreatedEvent evt)
        {
            SetIdBeVerySureYouKnowWhatYouAreDoing(evt.AggregateRootId);
            CVId = evt.CVId;
        }
    }
}
