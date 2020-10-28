using System;

namespace Service.StoreMessages.Events
{
    public class NcfsStartedEvent : Event
    {
        public NcfsStartedEvent(string fileId, DateTime timestamp) : base(Enums.EventId.NcfsStartedEvent, fileId, timestamp)
        {
        }
    }
}
