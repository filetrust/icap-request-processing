using System;
using System.Diagnostics.CodeAnalysis;
using Glasswall.Core.Engine;
using Glasswall.Core.Engine.Common;
using Glasswall.Core.Engine.Common.FileProcessing;
using Glasswall.Core.Engine.Common.GlasswallEngineLibrary;
using Glasswall.Core.Engine.Common.PolicyConfig;
using Glasswall.Core.Engine.FileProcessing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Service;
using Service.Messaging;

namespace Glasswall.CloudSdk.AWS.Rebuild
{
    [ExcludeFromCodeCoverage]
    public class Startup
    {
        IFileProcessorConfig Config { get; }

        public Startup()
        {
            Config = new FileProcessorConfig(); 

            var builder = new ConfigurationBuilder()
                .AddJsonFile("config/appsettings.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            builder.Bind(Config);
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IGlasswallVersionService, GlasswallVersionService>();
            services.AddSingleton<IFileTypeDetector, FileTypeDetector>();
            services.AddSingleton<IFileProtector, FileProtector>();
            services.AddSingleton<IFileAnalyser, FileAnalyser>();
            services.AddSingleton<IAdaptor<ContentManagementFlags, string>, GlasswallConfigurationAdaptor>();
            services.AddSingleton<IGlasswallFileProcessor, GlasswallFileProcessor>();
            services.AddSingleton<ITransactionEventProcessor, TransactionEventProcessor>();
            services.AddSingleton<IOutcomeSender, OutcomeSender>();
            services.AddSingleton<ITransactionEventSender, TransactionEventSender>();
            services.AddSingleton(Config);

            var p = (int)Environment.OSVersion.Platform;

            if ((p == 4) || (p == 6) || (p == 128))
            {
                services.AddSingleton<IGlasswallFileOperations, LinuxEngineOperations>();
            }
            else
            {
                services.AddSingleton<IGlasswallFileOperations, WindowsEngineOperations>();
            }
        }
    }
}