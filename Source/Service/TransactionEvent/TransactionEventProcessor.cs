using Glasswall.Core.Engine.Common.FileProcessing;
using Glasswall.Core.Engine.Messaging;
using Service.StoreMessages.Enums;
using Service.StoreMessages.Events;
using Service.Messaging;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Service.ErrorReport;
using System.Text;
using Microsoft.Extensions.Logging;
using System.IO;
using Service.Prometheus;
using Prometheus;
using Service.Engine;
using Service.Configuration;
using Service.Storage;
using Service.NCFS;

namespace Service.TransactionEvent
{
    public class TransactionEventProcessor : ITransactionEventProcessor
    {
        private readonly IGlasswallEngineService _glasswallEngineService;
        private readonly IOutcomeSender _outcomeSender;
        private readonly ITransactionEventSender _transactionEventSender;
        private readonly IArchiveRequestSender _archiveRequestSender;
        private readonly IFileManager _fileManager;
        private readonly IErrorReportGenerator _errorReportGenerator;
        private readonly INcfsProcessor _ncfsProcessor;
        private readonly IFileProcessorConfig _config;
        private readonly ILogger<TransactionEventProcessor> _logger;

        private readonly TimeSpan _processingTimeoutDuration;

        private readonly List<FileType> _archiveTypes = new List<FileType>() { FileType.Zip, FileType.Rar, FileType.Tar, FileType.SevenZip, FileType.Gzip };

        public TransactionEventProcessor(IGlasswallEngineService glasswallEngineService, IOutcomeSender outcomeSender, ITransactionEventSender transactionEventSender, IArchiveRequestSender archiveRequestSender,
            IFileManager fileManager, IErrorReportGenerator errorReportGenerator, INcfsProcessor ncfsProcessor, IFileProcessorConfig config, ILogger<TransactionEventProcessor> logger)
        {
            _glasswallEngineService = glasswallEngineService ?? throw new ArgumentNullException(nameof(glasswallEngineService));
            _outcomeSender = outcomeSender ?? throw new ArgumentNullException(nameof(outcomeSender));
            _transactionEventSender = transactionEventSender ?? throw new ArgumentNullException(nameof(transactionEventSender));
            _archiveRequestSender = archiveRequestSender ?? throw new ArgumentNullException(nameof(archiveRequestSender));
            _fileManager = fileManager ?? throw new ArgumentNullException(nameof(fileManager));
            _errorReportGenerator = errorReportGenerator ?? throw new ArgumentNullException(nameof(errorReportGenerator));
            _ncfsProcessor = ncfsProcessor ?? throw new ArgumentNullException(nameof(ncfsProcessor));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _processingTimeoutDuration = _config.ProcessingTimeoutDuration;
        }

        public void Process()
        {
            using (MetricsCounters.ProcTime.NewTimer())
            {

                var task = Task.Run(() =>
                 {
                     return ProcessTransaction();
                 });

                try
                {
                    bool isCompletedSuccessfully = task.Wait(_processingTimeoutDuration);

                    if (!isCompletedSuccessfully)
                    {
                        MetricsCounters.ProcCnt.WithLabels(Labels.Timeout).Inc();
                        _logger.LogError($"File Id: {_config.FileId} Processing exceeded {_processingTimeoutDuration}s");
                        ClearRebuiltStore(_config.OutputPath);
                        CreateErrorReport();
                        _outcomeSender.Send(FileOutcome.Failed, _config.FileId, _config.ReplyTo);
                    }
                }
                catch (Exception e)
                {
                    MetricsCounters.ProcCnt.WithLabels(Labels.Exception).Inc();
                    _logger.LogError($"File Id: {_config.FileId} Processing threw exception {e.Message}");
                    ClearRebuiltStore(_config.OutputPath);
                    CreateErrorReport();
                    _outcomeSender.Send(FileOutcome.Failed, _config.FileId, _config.ReplyTo);
                }
            }
        }

        private async Task ProcessTransaction()
        {
            var timestamp = DateTime.UtcNow;
            _transactionEventSender.Send(new NewDocumentEvent(_config.PolicyId.ToString(), RequestMode.Response, _config.FileId, timestamp));

            _logger.LogInformation($"File Id: {_config.FileId} Using Glasswall Version: {_glasswallEngineService.GetGlasswallVersion()}");

            if (!_fileManager.FileExists(_config.InputPath))
            {
                throw new FileNotFoundException($"File Id: {_config.FileId} does not exist at {_config.InputPath}");
            }

            var file = _fileManager.ReadFile(_config.InputPath);

            var fileType = _glasswallEngineService.GetFileType(file, _config.FileId);
            _transactionEventSender.Send(new FileTypeDetectionEvent(fileType.FileTypeName, _config.FileId, timestamp));

            string status;
            if (fileType.FileType == FileType.Unknown)
            {
                var base64File = Convert.ToBase64String(file);
                status = await _ncfsProcessor.GetUnmanagedActionAsync(timestamp, base64File, fileType.FileType);
                _transactionEventSender.Send(new UnmanagedFileTypeActionEvent(status, _config.FileId, timestamp));
            }
            else if (_archiveTypes.Contains(fileType.FileType))
            {
                _archiveRequestSender.Send(_config.FileId, fileType.FileTypeName, _config.InputPath, _config.OutputPath, _config.ReplyTo);
                MetricsCounters.ProcCnt.WithLabels(Labels.ArchiveFound).Inc();
                return;
            }
            else
            {
                status = await ProcessFile(file, fileType.FileType, timestamp);
            }

            if (status == FileOutcome.Failed)
            {
                CreateErrorReport();
            }

            _outcomeSender.Send(status, _config.FileId, _config.ReplyTo);

            MetricsCounters.ProcCnt.WithLabels(status).Inc();
            return;
        }

        private void CreateErrorReport()
        {
            if (_config.GenerateReport)
            {
                var report = _errorReportGenerator.CreateReport(_config.FileId);
                _fileManager.WriteFile(_config.OutputPath, Encoding.UTF8.GetBytes(report));
            }
        }

        private async Task<string> ProcessFile(byte[] file, FileType filetype, DateTime timestamp)
        {
            string status;

            var report = _glasswallEngineService.AnalyseFile(file, filetype.ToString(), _config.FileId);
            _transactionEventSender.Send(new AnalysisCompletedEvent(report, _config.FileId, timestamp));

            _transactionEventSender.Send(new RebuildStartingEvent(_config.FileId, timestamp));

            var rebuiltFile = _glasswallEngineService.RebuildFile(file, filetype.ToString(), _config.FileId, _config.ContentManagementFlags);

            if (rebuiltFile == null)
            {
                var base64File = Convert.ToBase64String(file);
                status = await _ncfsProcessor.GetBlockedActionAsync(timestamp, base64File, filetype);
                _transactionEventSender.Send(new BlockedFiletypeActionEvent(status, _config.FileId, timestamp));
            }
            else
            {
                _fileManager.WriteFile(_config.OutputPath, rebuiltFile);
                status = FileOutcome.Replace;
            }

            _transactionEventSender.Send(new RebuildCompletedEvent(status, _config.FileId, timestamp));

            return status;
        }

        private void ClearRebuiltStore(string path)
        {
            if (_fileManager.FileExists(path))
            {
                _fileManager.DeleteFile(path);
            }
        }
    } 
}