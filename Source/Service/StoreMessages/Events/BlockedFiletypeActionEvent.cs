using System;

namespace Service.StoreMessages.Events
{
    public class BlockedFiletypeActionEvent : Event
    {
        public BlockedFiletypeActionEvent(string action, string fileId, DateTime timestamp) : base(Enums.EventId.BlockedFileTypeAction, fileId, timestamp)
        {
            this.Action = action;
        }

        public string Action { get; }
    }
}
