using System;
using System.Threading.Tasks;
using Flurl;
using Flurl.Http;
using Service.Configuration;
using Glasswall.Core.Engine.Messaging;

namespace Service.NCFS
{
    public class NcfsClient : INcfsClient
    {
        private IFileProcessorConfig _config;

        public NcfsClient(IFileProcessorConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public async Task<NcfsOutcome> GetOutcome(string base64Body, FileType fileType)
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
    }
}
