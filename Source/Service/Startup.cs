﻿using System;
using System.Diagnostics.CodeAnalysis;
using Glasswall.Core.Engine;
using Glasswall.Core.Engine.Common;
using Glasswall.Core.Engine.Common.FileProcessing;
using Glasswall.Core.Engine.Common.GlasswallEngineLibrary;
using Glasswall.Core.Engine.Common.PolicyConfig;
using Glasswall.Core.Engine.FileProcessing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Service.Configuration;
using Service.Engine;
using Service.ErrorReport;
using Service.Messaging;
using Service.NCFS;
using Service.Storage;
using Service.TransactionEvent;

namespace Service
{
    [ExcludeFromCodeCoverage]
    public class Startup
    {
        FileProcessorConfig Config { get; }

        public Startup()
        {
            Config = new FileProcessorConfig(); 

            var builder = new ConfigurationBuilder()
                .AddJsonFile("config/appsettings.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            builder.Bind(Config);

            if (String.IsNullOrEmpty(Config.MessageBrokerUser))
            {
                Config.MessageBrokerUser = "guest";
            }

            if (String.IsNullOrEmpty(Config.MessageBrokerPassword))
            {
                Config.MessageBrokerPassword = "guest";
            }
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddLogging(configure => configure.AddConsole());
            services.AddTransient<IGlasswallVersionService, GlasswallVersionService>();
            services.AddTransient<IFileTypeDetector, FileTypeDetector>();
            services.AddTransient<IFileProtector, FileProtector>();
            services.AddTransient<IFileAnalyser, FileAnalyser>();
            services.AddTransient<IAdaptor<ContentManagementFlags, string>, GlasswallConfigurationAdaptor>();
            services.AddTransient<IGlasswallEngineService, GlasswallEngineService>();
            services.AddTransient<IProcessor, Processor>();
            services.AddTransient<IAdaptationRequestProcessor, AdaptationRequestProcessor>();
            services.AddTransient<IAdaptationRequestController, AdaptationRequestController>();
            services.AddTransient<IFileProcessor, FileProcessor>();
            services.AddScoped<IOutcomeSender, OutcomeSender>();
            services.AddScoped<ITransactionEventSender, TransactionEventSender>();
            services.AddScoped<IArchiveRequestSender, ArchiveRequestSender>();
            services.AddTransient<IFileManager, LocalFileManager>();
            services.AddTransient<IErrorReportGenerator, HtmlErrorReportGenerator>();
            services.AddTransient<INcfsProcessor, NcfsProcessor>();
            services.AddTransient<INcfsClient, NcfsClient>();
            services.AddSingleton<IFileProcessorConfig>(Config);

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