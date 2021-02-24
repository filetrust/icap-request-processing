using Glasswall.Core.Engine.Messaging;
using Service.StoreMessages.Events;
using Service.Messaging;
using System;
using System.Threading.Tasks;
using Glasswall.Core.Engine.Common.PolicyConfig;
using Microsoft.Extensions.Logging;
using Service.Engine;
using Service.Storage;
using Service.NCFS;

namespace Service.TransactionEvent
{
    public class FileProcessor : IFileProcessor
    {
        private readonly IGlasswallEngineService _glasswallEngineService;
        private readonly ITransactionEventSender _transactionEventSender;
        private readonly IFileManager _fileManager;
        private readonly INcfsProcessor _ncfsProcessor;
        private readonly ILogger<FileProcessor> _logger;

        public FileProcessor(IGlasswallEngineService glasswallEngineService,ITransactionEventSender transactionEventSender, 
            IFileManager fileManager, INcfsProcessor ncfsProcessor, ILogger<FileProcessor> logger)
        {
            _glasswallEngineService = glasswallEngineService ?? throw new ArgumentNullException(nameof(glasswallEngineService));
            _transactionEventSender = transactionEventSender ?? throw new ArgumentNullException(nameof(transactionEventSender));
            _fileManager = fileManager ?? throw new ArgumentNullException(nameof(fileManager));
            _ncfsProcessor = ncfsProcessor ?? throw new ArgumentNullException(nameof(ncfsProcessor));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<TransactionOutcome> HandleFileTypeDetection(byte[] file, string fileId, DateTime timestamp)
        {
            _logger.LogInformation($"File Id: {fileId} Using Glasswall Version: {_glasswallEngineService.GetGlasswallVersion()}");

            var fileType = _glasswallEngineService.GetFileType(file, fileId);
            _transactionEventSender.Send(new FileTypeDetectionEvent(fileType.FileTypeName, fileId, timestamp));

            var outcome = new TransactionOutcome
            {
                FileType = fileType.FileType
            };

            if (outcome.FileType == FileType.Unknown)
            {
                await HandleUnmanagedFile(file, fileId, outcome, timestamp);
            }

            return outcome;
        }

        public void HandleAnalysis(byte[] file, string fileId, FileType fileType, DateTime timestamp)
        {
            var report = _glasswallEngineService.AnalyseFile(file, fileType.ToString(), fileId);
            _transactionEventSender.Send(new AnalysisCompletedEvent(report, fileId, timestamp));
        }

        public async Task HandleRebuild(byte[] file, string fileId, TransactionOutcome outcome, string outputPath, ContentManagementFlags contentManagementFlags, DateTime timestamp)
        {
            _transactionEventSender.Send(new RebuildStartingEvent(fileId, timestamp));

            var rebuiltFile = _glasswallEngineService.RebuildFile(file, outcome.FileType.ToString(), fileId, contentManagementFlags);

            if (rebuiltFile == null || rebuiltFile.Length == 0)
            {
                await HandleBlockedFile(file, fileId, outcome, timestamp);
            }
            else
            {
                _fileManager.WriteFile(outputPath, rebuiltFile);
                outcome.Status = FileOutcome.Replace;
            }

            _transactionEventSender.Send(new RebuildCompletedEvent(outcome.Status, fileId, timestamp));
        }

        private async Task HandleUnmanagedFile(byte[] file, string fileId, TransactionOutcome outcome, DateTime timestamp)
        {
            var base64File = Convert.ToBase64String(file);
            var ncfsOutcome = await _ncfsProcessor.GetUnmanagedActionAsync(timestamp, base64File, outcome.FileType);

            outcome.Status = ncfsOutcome.FileOutcome;
            if (!string.IsNullOrEmpty(ncfsOutcome.ReplacementMimeType))
            {
                outcome.OptionalHeaders.Add("outcome-header-Content-Type", ncfsOutcome.ReplacementMimeType);
            }

            _transactionEventSender.Send(new UnmanagedFileTypeActionEvent(outcome.Status, fileId, timestamp));
        }

        private async Task HandleBlockedFile(byte[] file, string fileId, TransactionOutcome outcome, DateTime timestamp)
        {
            var base64File = Convert.ToBase64String(file);
            var ncfsOutcome = await _ncfsProcessor.GetBlockedActionAsync(timestamp, base64File, outcome.FileType);

            outcome.Status = ncfsOutcome.FileOutcome;
            if (!string.IsNullOrEmpty(ncfsOutcome.ReplacementMimeType))
            {
                outcome.OptionalHeaders.Add("outcome-header-Content-Type", ncfsOutcome.ReplacementMimeType);
            }

            _transactionEventSender.Send(new BlockedFiletypeActionEvent(outcome.Status, fileId, timestamp));
        }
    } 
}