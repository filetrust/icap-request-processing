using Glasswall.Core.Engine.Common.PolicyConfig;
using Service.StoreMessages.Enums;
using System;

namespace Service
{
    public interface IFileProcessorConfig
    {
        public string FileId { get; }
        public string InputPath { get; }
        public string OutputPath { get; }
        public string AmqpURL { get; }
        public string ReplyTo { get; }
        public TimeSpan ProcessingTimeoutDuration { get; set; }
        public Guid PolicyId { get; }
        public ContentManagementFlags ContentManagementFlags { get; }
        public NcfsOption UnprocessableFileTypeAction { get; }
        public NcfsOption GlasswallBlockedFilesAction { get; }
        public string NcfsRoutingUrl { get; }
        public string MessageBrokerUser { get; }
        public string MessageBrokerPassword { get; }
        public string AdaptationRequestQueueHostname { get; }
        public int AdaptationRequestQueuePort { get; }
        public string ArchiveAdaptationRequestQueueHostname { get; }
        public int ArchiveAdaptationRequestQueuePort { get; }
        public string TransactionEventQueueHostname { get; }
        public int TransactionEventQueuePort { get; }
    }
}