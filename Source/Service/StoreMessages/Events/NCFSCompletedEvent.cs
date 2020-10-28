using System;

namespace Service.StoreMessages.Events
{
    public class NCFSCompletedEvent : Event
    {
        public NCFSCompletedEvent(string ncfsOutcome, string fileId, DateTime timestamp) : base(Enums.EventId.NCFSCompletedEvent, fileId, timestamp)
        {
            this.NCFSOutcome = ncfsOutcome;
        }

        public string NCFSOutcome { get; }
    }
}
