using Service.StoreMessages.Enums;
using Service.StoreMessages.Events;
using Service.Messaging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Service.ErrorReport;
using System.Text;
using System.IO;
using Service.Prometheus;
using Service.Configuration;
using Service.Storage;

namespace Service.TransactionEvent
{
    public class AdaptationRequestProcessor : IAdaptationRequestProcessor
    {
        private readonly IFileProcessor _fileProcessor;
        private readonly IOutcomeSender _outcomeSender;
        private readonly IArchiveRequestSender _archiveRequestSender;
        private readonly IFileManager _fileManager;
        private readonly IErrorReportGenerator _errorReportGenerator;
        private readonly IAdaptationRequestController _adaptationRequestController;
        private readonly IFileProcessorConfig _config;

        public AdaptationRequestProcessor(IFileProcessor fileProcessor, IOutcomeSender outcomeSender, ITransactionEventSender transactionEventSender, IArchiveRequestSender archiveRequestSender,
            IFileManager fileManager, IErrorReportGenerator errorReportGenerator, IAdaptationRequestController adaptationRequestController, IFileProcessorConfig config)
        {
            _fileProcessor = fileProcessor ?? throw new ArgumentNullException(nameof(fileProcessor));
            _outcomeSender = outcomeSender ?? throw new ArgumentNullException(nameof(outcomeSender));
            _archiveRequestSender = archiveRequestSender ?? throw new ArgumentNullException(nameof(archiveRequestSender));
            _fileManager = fileManager ?? throw new ArgumentNullException(nameof(fileManager));
            _errorReportGenerator = errorReportGenerator ?? throw new ArgumentNullException(nameof(errorReportGenerator));
            _adaptationRequestController = adaptationRequestController ?? throw new ArgumentNullException(nameof(adaptationRequestController));
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public async Task Process()
        {
            var context = new AdaptationContext
            {
                FileId = _config.FileId,
                ContentManagementFlags = _config.ContentManagementFlags,
                TimeStamp = DateTime.UtcNow,
                OptionalHeaders = new Dictionary<string, string>(),
                OutputPath = _config.OutputPath,
                InputPath = _config.InputPath,
                GenerateErrorReport = _config.GenerateReport,
                ReplyTo = _config.ReplyTo,
                OnSuccessEvent = (status, fileId, replyTo, optionalHeaders) =>
                {
                    _outcomeSender.Send(status, fileId, replyTo, optionalHeaders);
                    MetricsCounters.ProcCnt.WithLabels(status).Inc();
                },
                OnArchiveEvent = (fileId, outcome, inputPath, outputPath, replyTo) =>
                {
                    _archiveRequestSender.Send(fileId, outcome, inputPath, outputPath, replyTo);
                    MetricsCounters.ProcCnt.WithLabels(Labels.ArchiveFound).Inc();
                },
                OnBlockedEvent = (file, fileId, fileType, optionalHeaders, timestamp) => _fileProcessor.HandleBlockedFile(file, fileId, fileType, optionalHeaders, timestamp),
                OnUnmanagedEvent = (file, fileId, fileType, optionalHeaders, timestamp) => _fileProcessor.HandleUnmanagedFile(file, fileId, fileType, optionalHeaders, timestamp),
                OnFailedEvent = (fileId, outputPath, generateReport) =>
                {
                    if (!_config.GenerateReport) return;
                    var report = _errorReportGenerator.CreateReport(_config.FileId);
                    _fileManager.WriteFile(_config.OutputPath, Encoding.UTF8.GetBytes(report));
                }
            };

            await _adaptationRequestController.ProcessRequest(context);
        }
    } 
}