using System;

namespace Service.StoreMessages.Events
{
    public class NcfsCompletedEvent : Event
    {
        public NcfsCompletedEvent(string ncfsOutcome, string fileId, DateTime timestamp) : base(Enums.EventId.NcfsCompletedEvent, fileId, timestamp)
        {
            this.NCFSOutcome = ncfsOutcome;
        }

        public string NCFSOutcome { get; }
    }
}
