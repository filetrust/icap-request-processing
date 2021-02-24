using System;
using System.Threading.Tasks;
using Glasswall.Core.Engine.Common.PolicyConfig;
using Glasswall.Core.Engine.Messaging;

namespace Service.TransactionEvent
{
    public interface IFileProcessor
    {
        Task<TransactionOutcome> HandleFileTypeDetection(byte[] file, string fileId, DateTime timestamp);
        void HandleAnalysis(byte[] file, string fileId, FileType fileType,  DateTime timestamp);
        Task HandleRebuild(byte[] file, string fileId, TransactionOutcome outcome, string outputPath, ContentManagementFlags contentManagementFlags, DateTime timestamp);
    } 
}