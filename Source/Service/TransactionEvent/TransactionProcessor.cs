using Service.StoreMessages.Enums;
using Service.StoreMessages.Events;
using Service.Messaging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Service.ErrorReport;
using System.Text;
using System.IO;
using Glasswall.Core.Engine.Messaging;
using Service.Prometheus;
using Service.Configuration;
using Service.Storage;

namespace Service.TransactionEvent
{
    public class TransactionProcessor : ITransactionProcessor
    {
        private readonly IFileProcessor _fileProcessor;
        private readonly IOutcomeSender _outcomeSender;
        private readonly ITransactionEventSender _transactionEventSender;
        private readonly IArchiveRequestSender _archiveRequestSender;
        private readonly IFileManager _fileManager;
        private readonly IErrorReportGenerator _errorReportGenerator;
        private readonly IFileProcessorConfig _config;

        private readonly List<FileType> _archiveTypes = new List<FileType> { FileType.Zip, FileType.Rar, FileType.Tar, FileType.SevenZip, FileType.Gzip };


        public TransactionProcessor(IFileProcessor fileProcessor, IOutcomeSender outcomeSender, ITransactionEventSender transactionEventSender, IArchiveRequestSender archiveRequestSender,
            IFileManager fileManager, IErrorReportGenerator errorReportGenerator, IFileProcessorConfig config)
        {
            _fileProcessor = fileProcessor ?? throw new ArgumentNullException(nameof(fileProcessor));
            _outcomeSender = outcomeSender ?? throw new ArgumentNullException(nameof(outcomeSender));
            _transactionEventSender = transactionEventSender ?? throw new ArgumentNullException(nameof(transactionEventSender));
            _archiveRequestSender = archiveRequestSender ?? throw new ArgumentNullException(nameof(archiveRequestSender));
            _fileManager = fileManager ?? throw new ArgumentNullException(nameof(fileManager));
            _errorReportGenerator = errorReportGenerator ?? throw new ArgumentNullException(nameof(errorReportGenerator));
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public async Task Process()
        {
            var timestamp = DateTime.UtcNow;
            _transactionEventSender.Send(new NewDocumentEvent(_config.PolicyId.ToString(), RequestMode.Response, _config.FileId, timestamp));

            if (!_fileManager.FileExists(_config.InputPath))
            {
                throw new FileNotFoundException($"File Id: {_config.FileId} does not exist at {_config.InputPath}");
            }

            var file = _fileManager.ReadFile(_config.InputPath);

            var outcome = await _fileProcessor.HandleFileTypeDetection(file, _config.FileId, timestamp);

            if (_archiveTypes.Contains(outcome.FileType))
            {
                _archiveRequestSender.Send(_config.FileId, outcome.FileType.ToString(), _config.InputPath, _config.OutputPath, _config.ReplyTo);
                MetricsCounters.ProcCnt.WithLabels(Labels.ArchiveFound).Inc();
                return;
            }

            _fileProcessor.HandleAnalysis(file, _config.FileId, outcome.FileType, timestamp);
            await _fileProcessor.HandleRebuild(file, _config.FileId, outcome, _config.OutputPath, _config.ContentManagementFlags, timestamp);

            //var outcome = await _fileProcessor.Process(file, timestamp);



            if (outcome.Status == FileOutcome.Failed)
            {
                CreateErrorReport();
            }

            _outcomeSender.Send(outcome.Status, _config.FileId, _config.ReplyTo, outcome.OptionalHeaders);

            MetricsCounters.ProcCnt.WithLabels(outcome.Status).Inc();
        }

        private void CreateErrorReport()
        {
            if (!_config.GenerateReport) return;
            var report = _errorReportGenerator.CreateReport(_config.FileId);
            _fileManager.WriteFile(_config.OutputPath, Encoding.UTF8.GetBytes(report));
        }
    } 
}