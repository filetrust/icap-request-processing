using Glasswall.Core.Engine.Messaging;
using Microsoft.Extensions.Logging;
using Service.Configuration;
using Service.ErrorReport;
using Service.Messaging;
using Service.Storage;
using Service.StoreMessages.Enums;
using Service.StoreMessages.Events;
using System;
using System.Collections.Generic;
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

        private readonly Dictionary<NcfsDecision, string> _decisionMappings = new Dictionary<NcfsDecision, string>() 
        {
            { NcfsDecision.Block, FileOutcome.Failed },
            { NcfsDecision.Relay, FileOutcome.Unmodified },
            { NcfsDecision.Replace, FileOutcome.Replace }
        };

        public NcfsProcessor(INcfsClient ncfsClient, ITransactionEventSender transactionEventSender, IFileManager fileManager, IFileProcessorConfig config, ILogger<NcfsProcessor> logger)
        {
            _ncfsClient = ncfsClient ?? throw new ArgumentNullException(nameof(ncfsClient));
            _transactionEventSender = transactionEventSender ?? throw new ArgumentNullException(nameof(transactionEventSender));
            _fileManager = fileManager ?? throw new ArgumentNullException(nameof(fileManager));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<NcfsOutcome> GetUnmanagedActionAsync(DateTime timestamp, string base64File, FileType fileType)
        {
            if (_config.UnprocessableFileTypeAction == NcfsOption.Refer)
            {
                _transactionEventSender.Send(new NcfsStartedEvent(_config.FileId, timestamp));

                var ncfsOutcome = await CallNcfsApi(base64File, fileType);

                _transactionEventSender.Send(new NcfsCompletedEvent(ncfsOutcome.NcfsDecision.ToString(), _config.FileId, timestamp));

                ncfsOutcome.FileOutcome = _decisionMappings[ncfsOutcome.NcfsDecision];

                return ncfsOutcome;
            }
            else
            {
                return new NcfsOutcome
                {
                    FileOutcome = _config.UnprocessableFileTypeAction == NcfsOption.Block
                    ? FileOutcome.Failed
                    : FileOutcome.Unmodified
                };
            }
        }

        public async Task<NcfsOutcome> GetBlockedActionAsync(DateTime timestamp, string base64File, FileType fileType)
        {
            if (_config.GlasswallBlockedFilesAction == NcfsOption.Refer)
            {
                _transactionEventSender.Send(new NcfsStartedEvent(_config.FileId, timestamp));

                var ncfsOutcome = await CallNcfsApi(base64File, fileType);

                _transactionEventSender.Send(new NcfsCompletedEvent(ncfsOutcome.NcfsDecision.ToString(), _config.FileId, timestamp));

                ncfsOutcome.FileOutcome = _decisionMappings[ncfsOutcome.NcfsDecision];

                return ncfsOutcome;
            }
            else
            {
                return new NcfsOutcome
                {
                    FileOutcome = _config.GlasswallBlockedFilesAction == NcfsOption.Block
                    ? FileOutcome.Failed
                    : FileOutcome.Unmodified
                };
            }
        }

        private async Task<NcfsOutcome> CallNcfsApi(string base64File, FileType fileType)
        {
            _logger.LogInformation($"File Id: {_config.FileId} Calling NCFS Api.");

            var response = await _ncfsClient.GetOutcome(base64File, fileType);

            _logger.LogInformation($"File Id: {_config.FileId} Received outcome {response.NcfsDecision} from NCFS Api.");

            if (response.NcfsDecision == NcfsDecision.Replace)
            {
                _logger.LogInformation($"File Id: {_config.FileId} Received base64 replacement from NCFS Api.");

                _fileManager.WriteFile(_config.OutputPath, Encoding.UTF8.GetBytes(response.Base64Replacement));
            }

            return response;
        }
    }
}
