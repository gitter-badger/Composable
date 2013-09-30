using System;
using Composable.CQRS.EventSourcing;
using Composable.DomainEvents;
using Manpower.Applications.JobCommunication.GlobalEvents;
using NServiceBus;

namespace Manpower.Applications.JobCommunication.GlobalEvents
{
    public interface IJobCommunicationCandidateEvent : IAggregateRootEvent
    {
        Guid CVId { get; set; }
    }

    public interface IJobCommunicationCandidateCreatedEvent : IJobCommunicationCandidateEvent
    {
    }
}

namespace Manpower.Applications.JobCommunication.Events
{
    public class JobCommunicationCandidateEvent : AggregateRootEvent, IJobCommunicationCandidateEvent
    {
        public Guid CVId { get; set; }

        protected JobCommunicationCandidateEvent(Guid jobCommunicationCandidateId, Guid cvId) : base(jobCommunicationCandidateId) {
            CVId = cvId;
        }

        protected JobCommunicationCandidateEvent() {}
    }

    public class JobCommunicationCandidateCreatedEvent : JobCommunicationCandidateEvent, IJobCommunicationCandidateCreatedEvent
    {
        public JobCommunicationCandidateCreatedEvent(Guid jobCommunicationCandidateId, Guid cvId) : base(jobCommunicationCandidateId, cvId)
        {
            
        }
    }
}