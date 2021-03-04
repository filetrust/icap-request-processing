using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Glasswall.Core.Engine.Common.PolicyConfig;
using Glasswall.Core.Engine.Messaging;

namespace Service.TransactionEvent
{
    public interface IFileProcessor
    {
        byte[] HandleNewFileRead(string fileId, string policyId, string inputPath, DateTime timestamp);
        FileType HandleFileTypeDetection(byte[] file, string fileId, DateTime timestamp);
        void HandleAnalysis(byte[] file, string fileId, FileType fileType,  DateTime timestamp);
        string HandleRebuild(byte[] file, string fileId, FileType fileType, string outputPath, ContentManagementFlags contentManagementFlags, DateTime timestamp);
        Task<string> HandleUnmanagedFile(byte[] file, string fileId, FileType fileType, Dictionary<string, string> optionalHeaders, DateTime timestamp);
        Task<string> HandleBlockedFile(byte[] file, string fileId, FileType fileType, Dictionary<string, string> optionalHeaders, DateTime timestamp);
    } 
}