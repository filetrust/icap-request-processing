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
using Service.Messaging;

namespace Service
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
            services.AddTransient<IGlasswallVersionService, GlasswallVersionService>();
            services.AddTransient<IFileTypeDetector, FileTypeDetector>();
            services.AddTransient<IFileProtector, FileProtector>();
            services.AddTransient<IFileAnalyser, FileAnalyser>();
            services.AddTransient<IAdaptor<ContentManagementFlags, string>, GlasswallConfigurationAdaptor>();
            services.AddTransient<IGlasswallFileProcessor, GlasswallFileProcessor>();
            services.AddTransient<ITransactionEventProcessor, TransactionEventProcessor>();
            services.AddScoped<IOutcomeSender, OutcomeSender>();
            services.AddScoped<ITransactionEventSender, TransactionEventSender>();
            services.AddTransient<IFileManager, LocalFileManager>();
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