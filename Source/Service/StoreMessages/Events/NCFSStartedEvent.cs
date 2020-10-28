using System;

namespace Service.StoreMessages.Events
{
    public class NCFSStartedEvent : Event
    {
        public NCFSStartedEvent(string fileId, DateTime timestamp) : base(Enums.EventId.NCFSStartedEvent, fileId, timestamp)
        {
        }
    }
}
