using Glasswall.Core.Engine.Messaging;
using Service.StoreMessages.Events;
using Service.Messaging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Glasswall.Core.Engine.Common.PolicyConfig;
using Microsoft.Extensions.Logging;
using Service.Engine;
using Service.Storage;
using Service.NCFS;
using Service.StoreMessages.Enums;

namespace Service.TransactionEvent
{
    public class FileProcessor : IFileProcessor
    {
        private readonly IGlasswallEngineService _glasswallEngineService;
        private readonly ITransactionEventSender _transactionEventSender;
        private readonly IFileManager _fileManager;
        private readonly INcfsProcessor _ncfsProcessor;
        private readonly ILogger<FileProcessor> _logger;

        private readonly Dictionary<NcfsDecision, string> _decisionMappings = new Dictionary<NcfsDecision, string>()
        {
            { NcfsDecision.Block, FileOutcome.Failed },
            { NcfsDecision.Relay, FileOutcome.Unmodified },
            { NcfsDecision.Replace, FileOutcome.Replace }
        };

        public FileProcessor(IGlasswallEngineService glasswallEngineService,ITransactionEventSender transactionEventSender, 
            IFileManager fileManager, INcfsProcessor ncfsProcessor, ILogger<FileProcessor> logger)
        {
            _glasswallEngineService = glasswallEngineService ?? throw new ArgumentNullException(nameof(glasswallEngineService));
            _transactionEventSender = transactionEventSender ?? throw new ArgumentNullException(nameof(transactionEventSender));
            _fileManager = fileManager ?? throw new ArgumentNullException(nameof(fileManager));
            _ncfsProcessor = ncfsProcessor ?? throw new ArgumentNullException(nameof(ncfsProcessor));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public byte[] HandleNewFileRead(string fileId, string policyId, string inputPath, DateTime timestamp)
        {
            _logger.LogInformation($"File Id:{fileId} Reading File from storage.");

            _transactionEventSender.Send(new NewDocumentEvent(policyId, RequestMode.Response, fileId, timestamp));

            if (!_fileManager.FileExists(inputPath))
            {
                throw new FileNotFoundException($"File Id: {inputPath} does not exist at {inputPath}.");
            }

            return _fileManager.ReadFile(inputPath);
        }

        public FileType HandleFileTypeDetection(byte[] file, string fileId, DateTime timestamp)
        {
            _logger.LogInformation($"File Id: {fileId} Using Glasswall Version: {_glasswallEngineService.GetGlasswallVersion()}");
            _logger.LogInformation($"File Id:{fileId} Getting file type from engine.");

            var fileType = _glasswallEngineService.GetFileType(file, fileId);
            _transactionEventSender.Send(new FileTypeDetectionEvent(fileType.FileTypeName, fileId, timestamp));

            _logger.LogInformation($"File Id:{fileId} File type {fileType.FileType} file type from engine.");

            return fileType.FileType;
        }

        public void HandleAnalysis(byte[] file, string fileId, FileType fileType, DateTime timestamp)
        {
            _logger.LogInformation($"File Id:{fileId} Getting Analysis Report.");

            var report = _glasswallEngineService.AnalyseFile(file, fileType.ToString(), fileId);
            _transactionEventSender.Send(new AnalysisCompletedEvent(report, fileId, timestamp));
        }

        public string HandleRebuild(byte[] file, string fileId, FileType fileType, string outputPath, ContentManagementFlags contentManagementFlags, DateTime timestamp)
        {
            string outcome;

            _logger.LogInformation($"File Id:{fileId} Rebuilding File.");

            _transactionEventSender.Send(new RebuildStartingEvent(fileId, timestamp));

            var rebuiltFile = _glasswallEngineService.RebuildFile(file, fileType.ToString(), fileId, contentManagementFlags);

            if (rebuiltFile == null || rebuiltFile.Length == 0)
            {
                outcome = FileOutcome.Failed;
            }
            else
            {
                _logger.LogInformation($"File Id:{fileId} Successfully rebuilt file, writing to output.");

                _fileManager.WriteFile(outputPath, rebuiltFile);
                outcome = FileOutcome.Replace;
            }

            _transactionEventSender.Send(new RebuildCompletedEvent(outcome, fileId, timestamp));

            return outcome;
        }

        public async Task<string> HandleUnmanagedFile(byte[] file, string fileId, FileType fileType, Dictionary<string, string> optionalHeaders, DateTime timestamp)
        {
            var base64File = Convert.ToBase64String(file);
            var ncfsOutcome = await _ncfsProcessor.GetUnmanagedActionAsync(timestamp, base64File, fileType);

            var status = _decisionMappings[ncfsOutcome.NcfsDecision];
            if (!string.IsNullOrEmpty(ncfsOutcome.ReplacementMimeType))
            {
                optionalHeaders.Add("outcome-header-Content-Type", ncfsOutcome.ReplacementMimeType);
            }

            _transactionEventSender.Send(new UnmanagedFileTypeActionEvent(status, fileId, timestamp));

            return status;
        }

        public async Task<string> HandleBlockedFile(byte[] file, string fileId, FileType fileType, Dictionary<string, string> optionalHeaders, DateTime timestamp)
        {
            var base64File = Convert.ToBase64String(file);
            var ncfsOutcome = await _ncfsProcessor.GetBlockedActionAsync(timestamp, base64File, fileType);

            var status = _decisionMappings[ncfsOutcome.NcfsDecision];
            if (!string.IsNullOrEmpty(ncfsOutcome.ReplacementMimeType))
            {
                optionalHeaders.Add("outcome-header-Content-Type", ncfsOutcome.ReplacementMimeType);
            }

            _transactionEventSender.Send(new BlockedFiletypeActionEvent(status, fileId, timestamp));

            return status;
        }
    } 
}