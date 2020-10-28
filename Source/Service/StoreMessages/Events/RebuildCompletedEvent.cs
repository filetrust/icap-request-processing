using System;

namespace Service.StoreMessages.Events
{
    public class RebuildCompletedEvent : Event
    {
        public RebuildCompletedEvent(string outcome, string fileId, DateTime timestamp) : base(Enums.EventId.RebuildCompleted, fileId, timestamp)
        {
            this.GwOutcome = outcome;
        }

        public string GwOutcome { get; }
    }
}
