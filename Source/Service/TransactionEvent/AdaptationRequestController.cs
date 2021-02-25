using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Glasswall.Core.Engine.Messaging;
using Microsoft.Extensions.Logging;

namespace Service.TransactionEvent
{
    public class AdaptationRequestController : IAdaptationRequestController
    {
        private readonly IFileProcessor _fileProcessor;
        private readonly ILogger<AdaptationRequestController> _logger;

        private readonly List<FileType> _archiveTypes = new List<FileType> { FileType.Zip, FileType.Rar, FileType.Tar, FileType.SevenZip, FileType.Gzip };

        public AdaptationRequestController(IFileProcessor fileProcessor, ILogger<AdaptationRequestController> logger)
        {
            _fileProcessor = fileProcessor ?? throw new ArgumentNullException(nameof(fileProcessor));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task ProcessRequest(AdaptationContext context)
        {
            string outcome;

            var file = _fileProcessor.HandleNewFileRead(context.FileId, context.PolicyId, context.InputPath, context.TimeStamp);

            var fileType = _fileProcessor.HandleFileTypeDetection(file, context.FileId, context.TimeStamp);

            if (fileType == FileType.Unknown)
            {
                _logger.LogInformation($"File Id:{context.FileId} Unknown File Type found, calling Unmanaged Event.");
                outcome = await context.OnUnmanagedEvent(file, context.FileId, fileType, context.OptionalHeaders, context.TimeStamp);
            }
            else if (_archiveTypes.Contains(fileType))
            {
                _logger.LogInformation($"File Id:{context.FileId} Archive File Type {fileType} found, calling Archive Event.");
                context.OnArchiveEvent(context.FileId, fileType.ToString(), context.InputPath, context.OutputPath, context.ReplyTo);
                return;
            }
            else
            {
                _fileProcessor.HandleAnalysis(file, context.FileId, fileType, context.TimeStamp);

                outcome = _fileProcessor.HandleRebuild(file, context.FileId, fileType, context.OutputPath, context.ContentManagementFlags, context.TimeStamp);

                if (outcome == FileOutcome.Failed)
                {
                    _logger.LogInformation($"File Id:{context.FileId} Rebuild failed, calling Blocked Event");
                    outcome = await context.OnBlockedEvent(file, context.FileId, fileType, context.OptionalHeaders, context.TimeStamp);
                }
            }

            if (outcome == FileOutcome.Failed)
            {
                _logger.LogInformation($"File Id:{context.FileId} Outcome failed, calling Failed Event");
                context.OnFailedEvent(context.FileId, context.OutputPath, context.GenerateErrorReport);
            }

            _logger.LogInformation($"File Id:{context.FileId} Processing File finished, calling Success Event");
            context.OnSuccessEvent(outcome, context.FileId, context.ReplyTo, context.OptionalHeaders);
        }
    }
}
