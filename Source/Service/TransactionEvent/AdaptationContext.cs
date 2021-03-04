using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Glasswall.Core.Engine.Common.PolicyConfig;
using Glasswall.Core.Engine.Messaging;

namespace Service.TransactionEvent
{
    public class AdaptationContext
    {
        public string FileId { get; set; }
        public string PolicyId { get; set; }
        public ContentManagementFlags ContentManagementFlags { get; set; }
        public DateTime TimeStamp { get; set; }
        public string InputPath { get; set; }
        public string OutputPath { get; set; }
        public string ReplyTo { get; set; }
        public bool GenerateErrorReport { get; set; }
        public Dictionary<string, string> OptionalHeaders { get; set; }
        public Action<string, string, string, Dictionary<string, string>> OnFinishEvent { get; set; }
        public Action<string, string, bool> OnFailedEvent { get; set; }
        public Action<string, string, string, string, string> OnArchiveEvent { get; set; }
        public Func<byte[], string, FileType, Dictionary<string, string>, DateTime, Task<string>> OnUnmanagedEvent { get; set; }
        public Func<byte[], string, FileType, Dictionary<string, string>, DateTime, Task<string>> OnBlockedEvent { get; set; }
    }
}