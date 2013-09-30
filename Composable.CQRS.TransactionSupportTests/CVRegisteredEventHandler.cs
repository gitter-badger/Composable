using System;
using Composable.CQRS.EventSourcing;
using Manpower.Applications.CVManagement.GlobalEvents;
using Manpower.Applications.JobCommunication.Domain;
using NServiceBus;

namespace Manpower.Applications.JobCommunication.ChannelAdapter.Inbound.CVManagement
{
    public class CVRegisteredEventHandler : IHandleMessages<ICVRegisteredEvent>
    {
        private readonly IEventStoreSession _session;

        public CVRegisteredEventHandler(IEventStoreSession session)
        {
            _session = session;
        }

        public void Handle(ICVRegisteredEvent evt)
        {
            var c = JobCommunicationCandidate.Create(Guid.NewGuid(), evt.AggregateRootId);
            _session.Save(c);
            _session.SaveChanges();
        }
    }
}

namespace Manpower.Applications.CVManagement.GlobalEvents
{
    public interface ICVRegisteredEvent : IAggregateRootEvent
    {
        
    }
}

namespace Manpower.Applications.CVManagement.Events
{
    public class CVRegisteredEvent : AggregateRootEvent, ICVRegisteredEvent
    {
        public Guid UserGroupId { get; set; }

        public Guid UserId { get; set; }

        public string Email { get; set; }

        public DateTime RegistrationDate { get; set; }

        public CVRegisteredEvent()
        {
        }

        public CVRegisteredEvent(Guid cvId, Guid userGroupId, Guid userId, string email, DateTime registrationDate)
            : base(cvId)
        {
            this.UserGroupId = userGroupId;
            this.UserId = userId;
            this.Email = email;
            this.RegistrationDate = registrationDate;
        }
    }
}
