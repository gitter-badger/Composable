using System;
using System.ComponentModel;
using Composable.DDD;
using Newtonsoft.Json;

namespace Composable.CQRS.EventSourcing
{
    //TODO: Not sure about making value object the base class, but about a gazillion tests in some parts of the code depends on several subclasses being ValueObjects
    public class AggregateRootEvent : ValueObject<AggregateRootEvent>, IAggregateRootEvent
    {
        protected AggregateRootEvent()
        {
            EventId = Guid.NewGuid();
            TimeStamp = DateTime.UtcNow;
        }

        protected AggregateRootEvent(Guid aggregateRootId)
            : this()
        {
            AggregateRootId = aggregateRootId;
        }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore), DefaultValue(typeof(Guid), "00000000-0000-0000-0000-000000000000")]
        public Guid EventId { get; set; }
        
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore), DefaultValue(0)]
        public int AggregateRootVersion { get; set; }
        
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore), DefaultValue(typeof(Guid), "00000000-0000-0000-0000-000000000000")]
        public Guid AggregateRootId { get; set; }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore), DefaultValue(typeof(DateTime), "0001-01-01 00:00:00")]
        public DateTime TimeStamp { get; set; }
    }
}