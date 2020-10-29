using Service.StoreMessages.Enums;
using System;

namespace Service.StoreMessages.Events
{
    public class NewDocumentEvent : Event
    {
        public NewDocumentEvent(string policyId, RequestMode mode, string fileId, DateTime timestamp) : base(Enums.EventId.NewDocument, fileId, timestamp)
        {
            this.PolicyId = policyId;
            this.Mode = mode;
        }

        public string PolicyId { get; }
        public RequestMode Mode { get; }
    }
}
