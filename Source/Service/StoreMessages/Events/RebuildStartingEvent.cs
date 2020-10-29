using System;

namespace Service.StoreMessages.Events
{
    public class RebuildStartingEvent : Event
    {
        public RebuildStartingEvent(string fileId, DateTime timestamp) : base(Enums.EventId.RebuildStarted, fileId, timestamp)
        {
        }
    }
}
