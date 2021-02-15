using System;
using System.Threading.Tasks;
using Flurl;
using Flurl.Http;
using Service.Configuration;
using Glasswall.Core.Engine.Messaging;
using Microsoft.Extensions.Logging;

namespace Service.NCFS
{
    public class NcfsClient : INcfsClient
    {
        private IFileProcessorConfig _config;
        private ILogger<NcfsClient> _logger;

        public NcfsClient(IFileProcessorConfig config, ILogger<NcfsClient> logger)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<NcfsOutcome> GetOutcome(string base64Body, FileType fileType)
        {
            try
            {
                FlurlHttp.ConfigureClient(_config.NcfsRoutingUrl, cli => cli.Settings.HttpClientFactory = new UntrustedCertClientFactory());

                var response = await $"{_config.NcfsRoutingUrl}/api/Decide"
                    .PostJsonAsync(new
                    {
                        Base64Body = base64Body,
                        DetectedFiletype = fileType
                    });

                var responseJson = await response.GetJsonAsync();

                return new NcfsOutcome
                {
                    NcfsDecision = response.Headers.FirstOrDefault("ncfs-decision"),
                    Base64Replacement = responseJson?.base64Replacement ?? string.Empty
                };
            }
            catch (FlurlHttpException ex)
            {
                var error = await ex.GetResponseStringAsync();
                _logger.LogError($"File Id: {_config.FileId} Error returned from NCFS Api: {error}");

                return new NcfsOutcome
                {
                    NcfsDecision = FileOutcome.Failed
                };
            }
            catch (Exception e)
            {
                _logger.LogError($"File Id: {_config.FileId} Error calling NCFS Api: {e.Message}");

                return new NcfsOutcome
                {
                    NcfsDecision = FileOutcome.Failed
                };
            }
        }
    }
}
