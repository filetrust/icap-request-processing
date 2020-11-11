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
        private readonly IFileManager _fileManager;
        private readonly IFileProcessorConfig _config;

        public TransactionEventProcessor(IGlasswallFileProcessor fileProcessor, IGlasswallVersionService versionService, IOutcomeSender outcomeSender, ITransactionEventSender transactionEventSender, IFileManager fileManager, IFileProcessorConfig config)
        {
            _fileProcessor = fileProcessor ?? throw new ArgumentNullException(nameof(fileProcessor));
            _glasswallVersionService = versionService ?? throw new ArgumentNullException(nameof(versionService));
            _outcomeSender = outcomeSender ?? throw new ArgumentNullException(nameof(outcomeSender));
            _transactionEventSender = transactionEventSender ?? throw new ArgumentNullException(nameof(transactionEventSender));
            _fileManager = fileManager ?? throw new ArgumentNullException(nameof(fileManager));
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public void Process()
        {
            var timestamp = DateTime.UtcNow;
            _transactionEventSender.Send(new NewDocumentEvent(_config.PolicyId.ToString(), RequestMode.Response, _config.FileId, timestamp));

            Console.WriteLine($"Using Glasswall Version: {_glasswallVersionService.GetVersion()}");

            var file = _fileManager.ReadFile(_config.InputPath);

            var fileType = _fileProcessor.GetFileType(file);
            _transactionEventSender.Send(new FileTypeDetectionEvent(fileType.FileTypeName, _config.FileId, timestamp));

            string status;
            if (fileType.FileType == FileType.Unknown)
            {
                status = GetUnmanagedAction();

                _transactionEventSender.Send(new UnmanagedFileTypeActionEvent(status, _config.FileId, timestamp));
            }
            else
            {
                var report = _fileProcessor.AnalyseFile(file, fileType.FileTypeName);
                _transactionEventSender.Send(new AnalysisCompletedEvent(report, _config.FileId, timestamp));

                _transactionEventSender.Send(new RebuildStartingEvent(_config.FileId, timestamp));
                
                var rebuiltFile = _fileProcessor.RebuildFile(file, fileType.FileTypeName);

                if (rebuiltFile == null)
                {
                    status = GetBlockedAction();
                    _transactionEventSender.Send(new BlockedFiletypeActionEvent(status, _config.FileId, timestamp));
                }
                else
                {
                    _fileManager.WriteFile(_config.OutputPath, rebuiltFile);
                    status = FileOutcome.Replace;
                }

                _transactionEventSender.Send(new RebuildCompletedEvent(status, _config.FileId, timestamp));
            }

            _outcomeSender.Send(status, _config.FileId, _config.ReplyTo);
        }

        private string GetUnmanagedAction()
        {
            // Will be extended to include Refer Action & Decision from NCFS Service
            return _config.UnprocessableFileTypeAction == NcfsOption.Block 
                ? FileOutcome.Replace 
                : FileOutcome.Unmodified;
        }

        private string GetBlockedAction()
        {
            // Will be extended to include Refer Action & Decision from NCFS Service
            return _config.GlasswallBlockedFilesAction == NcfsOption.Block
                ? FileOutcome.Replace
                : FileOutcome.Unmodified;
        }
    } 
}