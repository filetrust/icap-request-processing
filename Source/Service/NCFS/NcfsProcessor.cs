﻿using Glasswall.Core.Engine.Messaging;
using Microsoft.Extensions.Logging;
using Service.Configuration;
using Service.Messaging;
using Service.Storage;
using Service.StoreMessages.Enums;
using Service.StoreMessages.Events;
using System;
using System.Text;
using System.Threading.Tasks;

namespace Service.NCFS
{
    public class NcfsProcessor : INcfsProcessor
    {
        private readonly INcfsClient _ncfsClient;
        private readonly ITransactionEventSender _transactionEventSender;
        private readonly IFileManager _fileManager;
        private readonly IFileProcessorConfig _config;
        private readonly ILogger<NcfsProcessor> _logger;

        public NcfsProcessor(INcfsClient ncfsClient, ITransactionEventSender transactionEventSender, IFileManager fileManager, IFileProcessorConfig config, ILogger<NcfsProcessor> logger)
        {
            _ncfsClient = ncfsClient ?? throw new ArgumentNullException(nameof(ncfsClient));
            _transactionEventSender = transactionEventSender ?? throw new ArgumentNullException(nameof(transactionEventSender));
            _fileManager = fileManager ?? throw new ArgumentNullException(nameof(fileManager));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<string> GetUnmanagedActionAsync(DateTime timestamp, string base64File, FileType fileType)
        {
            string outcome;
            if (_config.UnprocessableFileTypeAction == NcfsOption.Refer)
            {
                _transactionEventSender.Send(new NcfsStartedEvent(_config.FileId, timestamp));

                outcome = await CallNcfsApi(base64File, fileType);

                _transactionEventSender.Send(new NcfsCompletedEvent(outcome, _config.FileId, timestamp));
            }
            else
            {
                outcome = _config.UnprocessableFileTypeAction == NcfsOption.Block
                    ? FileOutcome.Failed
                    : FileOutcome.Unmodified;
            }

            return outcome;
        }

        public async Task<string> GetBlockedActionAsync(DateTime timestamp, string base64File, FileType fileType)
        {
            string outcome;
            if (_config.GlasswallBlockedFilesAction == NcfsOption.Refer)
            {
                _transactionEventSender.Send(new NcfsStartedEvent(_config.FileId, timestamp));

                outcome = await CallNcfsApi(base64File, fileType);

                _transactionEventSender.Send(new NcfsCompletedEvent(outcome, _config.FileId, timestamp));
            }
            else
            {
                outcome = _config.GlasswallBlockedFilesAction == NcfsOption.Block
                    ? FileOutcome.Failed
                    : FileOutcome.Unmodified;
            }

            return outcome;
        }

        private async Task<string> CallNcfsApi(string base64File, FileType fileType)
        {
            _logger.LogInformation($"File Id: {_config.FileId} Calling NCFS Api.");

            var response = await _ncfsClient.GetOutcome(base64File, fileType);

            _logger.LogInformation($"File Id: {_config.FileId} Received outcome {response.NcfsDecision} from NCFS Api.");

            if (!string.IsNullOrEmpty(response.Base64Replacement))
            {
                _logger.LogInformation($"File Id: {_config.FileId} Received base64 replacement from NCFS Api.");

                _fileManager.WriteFile(_config.OutputPath, Encoding.UTF8.GetBytes(response.Base64Replacement));
            }

            return response.NcfsDecision;
        }
    }
}
