using Glasswall.Core.Engine.Messaging;
using Microsoft.Extensions.Logging;
using Service.Configuration;
using Service.ErrorReport;
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
        private readonly IErrorReportGenerator _errorReportGenerator;
        private readonly IFileProcessorConfig _config;
        private readonly ILogger<NcfsProcessor> _logger;

        public NcfsProcessor(INcfsClient ncfsClient, ITransactionEventSender transactionEventSender, IFileManager fileManager, IErrorReportGenerator errorReportGenerator, IFileProcessorConfig config, ILogger<NcfsProcessor> logger)
        {
            _ncfsClient = ncfsClient ?? throw new ArgumentNullException(nameof(ncfsClient));
            _transactionEventSender = transactionEventSender ?? throw new ArgumentNullException(nameof(transactionEventSender));
            _fileManager = fileManager ?? throw new ArgumentNullException(nameof(fileManager));
            _errorReportGenerator = errorReportGenerator ?? throw new ArgumentNullException(nameof(errorReportGenerator));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<string> GetUnmanagedActionAsync(DateTime timestamp, string base64File, FileType fileType)
        {
            if (_config.UnprocessableFileTypeAction == NcfsOption.Refer)
            {
                _transactionEventSender.Send(new NcfsStartedEvent(_config.FileId, timestamp));

                var decision = await CallNcfsApi(base64File, fileType);

                _transactionEventSender.Send(new NcfsCompletedEvent(decision.ToString(), _config.FileId, timestamp));

                return decision == NcfsDecision.Relay ? FileOutcome.Unmodified : FileOutcome.Replace;
            }
            else if (_config.UnprocessableFileTypeAction == NcfsOption.Block)
            {
                _logger.LogInformation($"File Id: {_config.FileId} Policy has Block setting, generating error report.");
                GenerateErrorReport();
                return FileOutcome.Replace;
            }
            else
            {
                return FileOutcome.Unmodified;
            }
        }

        public async Task<string> GetBlockedActionAsync(DateTime timestamp, string base64File, FileType fileType)
        {
            if (_config.GlasswallBlockedFilesAction == NcfsOption.Refer)
            {
                _transactionEventSender.Send(new NcfsStartedEvent(_config.FileId, timestamp));

                var decision = await CallNcfsApi(base64File, fileType);

                _transactionEventSender.Send(new NcfsCompletedEvent(decision.ToString(), _config.FileId, timestamp));

                return decision == NcfsDecision.Relay ? FileOutcome.Unmodified : FileOutcome.Replace;
            }
            else if (_config.GlasswallBlockedFilesAction == NcfsOption.Block)
            {
                _logger.LogInformation($"File Id: {_config.FileId} Policy has Block setting, generating error report.");
                GenerateErrorReport();
                return FileOutcome.Replace;
            }
            else
            {
                return FileOutcome.Unmodified;
            }
        }

        private async Task<NcfsDecision> CallNcfsApi(string base64File, FileType fileType)
        {
            _logger.LogInformation($"File Id: {_config.FileId} Calling NCFS Api.");

            var response = await _ncfsClient.GetOutcome(base64File, fileType);

            _logger.LogInformation($"File Id: {_config.FileId} Received outcome {response.NcfsDecision} from NCFS Api.");

            if (response.NcfsDecision == NcfsDecision.Replace)
            {
                _logger.LogInformation($"File Id: {_config.FileId} Received base64 replacement from NCFS Api.");

                _fileManager.WriteFile(_config.OutputPath, Encoding.UTF8.GetBytes(response.Base64Replacement));
            }
            else if (response.NcfsDecision == NcfsDecision.Block)
            {
                _logger.LogInformation($"File Id: {_config.FileId} Received Block response, generating error report.");
                GenerateErrorReport();
            }

            return response.NcfsDecision;
        }

        private void GenerateErrorReport()
        {
            if (_config.GenerateReport)
            {
                var report = _errorReportGenerator.CreateReport(_config.FileId);
                _fileManager.WriteFile(_config.OutputPath, Encoding.UTF8.GetBytes(report));
            }
        }
    }
}
