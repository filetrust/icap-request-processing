using System;

namespace Service.StoreMessages.Events
{
    public class UnmanagedFileTypeActionEvent : Event
    {
        public UnmanagedFileTypeActionEvent(string action, string fileId, DateTime timestamp) : base(Enums.EventId.UnmanagedFiletypeAction, fileId, timestamp)
        {
            this.Action = action;
        }

        public string Action { get; }
    }
}