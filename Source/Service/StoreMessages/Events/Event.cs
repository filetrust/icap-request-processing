using Newtonsoft.Json;
using Service.StoreMessages.Enums;
using System;

namespace Service.StoreMessages.Events
{
    public class Event
    {
        public string FileId { get; }
        public EventId EventId { get; }
        public DateTime Timestamp { get; }

        public Event(EventId eventId, string fileId, DateTime timestamp)
        {
            this.EventId = eventId;
            this.FileId = fileId;
            this.Timestamp = timestamp;
        }

        public virtual string ToJson()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
