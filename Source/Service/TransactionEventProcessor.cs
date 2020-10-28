using Glasswall.Core.Engine.Common.FileProcessing;
using Glasswall.Core.Engine.Messaging;
using Service.StoreMessages.Enums;
using Service.StoreMessages.Events;
using System;
using System.IO;

namespace Service
{
    public class TransactionEventProcessor : ITransactionEventProcessor
    {
        private readonly IGlasswallFileProcessor _fileProcessor;
        private readonly IGlasswallVersionService _glasswallVersionService;
        private readonly IMessageSender _messageSender;
        private readonly IFileProcessorConfig _config;

        public TransactionEventProcessor(IGlasswallFileProcessor fileProcessor, IGlasswallVersionService versionService, IMessageSender messageSender, IFileProcessorConfig config)
        {
            _fileProcessor = fileProcessor ?? throw new ArgumentNullException(nameof(fileProcessor));
            _glasswallVersionService = versionService ?? throw new ArgumentNullException(nameof(versionService));
            _messageSender = messageSender ?? throw new ArgumentNullException(nameof(messageSender));
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public void Process()
        {
            var timestamp = DateTime.UtcNow;
            //TODO: Replace policy id with id from config map
            _messageSender.SendTransactionEvent(new NewDocumentEvent(Guid.NewGuid().ToString(), RequestMode.Response, _config.FileId, timestamp));

            Console.WriteLine($"Using Glasswall Version: {_glasswallVersionService.GetVersion()}");

            var file = File.ReadAllBytes(_config.InputPath);

            var fileType = _fileProcessor.GetFileType(file);
            _messageSender.SendTransactionEvent(new FileTypeDetectionEvent(fileType.FileTypeName, _config.FileId, timestamp));

            string status;
            if (fileType.FileType == FileType.Unknown)
            {
                status = FileOutcome.Unmodified;

                _messageSender.SendTransactionEvent(new UnmanagedFileTypeActionEvent(status, _config.FileId, timestamp));
            }
            else
            {
                var report = _fileProcessor.AnalyseFile(fileType.FileTypeName, file);
                _messageSender.SendTransactionEvent(new AnalysisCompletedEvent(report, _config.FileId, timestamp));

                _messageSender.SendTransactionEvent(new RebuildStartingEvent(_config.FileId, timestamp));
                
                status = _fileProcessor.RebuildFile(file, fileType.FileTypeName);

                if (status != FileOutcome.Replace)
                    _messageSender.SendTransactionEvent(new BlockedFiletypeActionEvent(status, _config.FileId, timestamp));

                _messageSender.SendTransactionEvent(new RebuildCompletedEvent(status, _config.FileId, timestamp));
            }

            _messageSender.SendMessageAdaptationOutcome(status, _config.FileId, _config.ReplyTo);
        }
    } 
}
