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
        public string ReplyTo { get; }
        public Guid PolicyId { get; }
        public ContentManagementFlags ContentManagementFlags { get; }
        public NcfsOption UnprocessableFileTypeAction { get; }
        public NcfsOption GlasswallBlockedFilesAction { get; }
        public string NcfsRoutingUrl { get; }
    }
}