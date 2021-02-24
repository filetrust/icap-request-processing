using Glasswall.Core.Engine.Messaging;
using System.Collections.Generic;

namespace Service.TransactionEvent
{
    public class TransactionOutcome
    {
        public FileType FileType { get; set; }
        public string Status { get; set; }
        public Dictionary<string, string> OptionalHeaders => new Dictionary<string, string>();
        public bool Archive { get; set; }
    }
}
