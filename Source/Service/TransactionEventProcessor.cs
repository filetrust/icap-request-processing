using Glasswall.Core.Engine.Common.FileProcessing;
using Glasswall.Core.Engine.Messaging;
using Service.StoreMessages.Enums;
using Service.StoreMessages.Events;
using Service.Messaging;
using System;
using System.IO;

namespace Service
{
    public class TransactionEventProcessor : ITransactionEventProcessor
    {
        private readonly IGlasswallFileProcessor _fileProcessor;
        private readonly IGlasswallVersionService _glasswallVersionService;
        private readonly IOutcomeSender _outcomeSender;
        private readonly ITransactionEventSender _transactionEventSender;
        private readonly IFileProcessorConfig _config;

        public TransactionEventProcessor(IGlasswallFileProcessor fileProcessor, IGlasswallVersionService versionService, IOutcomeSender outcomeSender, ITransactionEventSender transactionEventSender, IFileProcessorConfig config)
        {
            _fileProcessor = fileProcessor ?? throw new ArgumentNullException(nameof(fileProcessor));
            _glasswallVersionService = versionService ?? throw new ArgumentNullException(nameof(versionService));
            _outcomeSender = outcomeSender ?? throw new ArgumentNullException(nameof(outcomeSender));
            _transactionEventSender = transactionEventSender ?? throw new ArgumentNullException(nameof(transactionEventSender));
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public void Process()
        {
            var timestamp = DateTime.UtcNow;
            _transactionEventSender.Send(new NewDocumentEvent(Guid.NewGuid().ToString(), RequestMode.Response, _config.FileId, timestamp));

            Console.WriteLine($"Using Glasswall Version: {_glasswallVersionService.GetVersion()}");

            var file = File.ReadAllBytes(_config.InputPath);

            var fileType = _fileProcessor.GetFileType(file);
            _transactionEventSender.Send(new FileTypeDetectionEvent(fileType.FileTypeName, _config.FileId, timestamp));

            string status;
            if (fileType.FileType == FileType.Unknown)
            {
                status = FileOutcome.Unmodified;

                _transactionEventSender.Send(new UnmanagedFileTypeActionEvent(status, _config.FileId, timestamp));
            }
            else
            {
                var report = _fileProcessor.AnalyseFile(fileType.FileTypeName, file);
                _transactionEventSender.Send(new AnalysisCompletedEvent(report, _config.FileId, timestamp));

                _transactionEventSender.Send(new RebuildStartingEvent(_config.FileId, timestamp));
                
                status = _fileProcessor.RebuildFile(file, fileType.FileTypeName);

                if (status != FileOutcome.Replace)
                    _transactionEventSender.Send(new BlockedFiletypeActionEvent(status, _config.FileId, timestamp));

                _transactionEventSender.Send(new RebuildCompletedEvent(status, _config.FileId, timestamp));
            }

            _outcomeSender.Send(status, _config.FileId, _config.ReplyTo);
        }
    } 
}
