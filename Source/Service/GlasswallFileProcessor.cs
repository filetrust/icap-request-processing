using Glasswall.Core.Engine.Common.FileProcessing;
using Glasswall.Core.Engine.Common.PolicyConfig;
using Glasswall.Core.Engine.Messaging;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.IO;

namespace Service
{
    public class GlasswallFileProcessor : IGlasswallFileProcessor
    {
        private readonly IGlasswallVersionService _glasswallVersionService;
        private readonly IFileTypeDetector _fileTypeDetector;
        private readonly IFileProtector _fileProtector;
        private readonly IFileProcessorConfig _config;

        public GlasswallFileProcessor(IGlasswallVersionService glasswallVersionService, IFileTypeDetector fileTypeDetector, IFileProtector fileProtector, IFileProcessorConfig config)
        {
            _glasswallVersionService = glasswallVersionService ?? throw new ArgumentNullException(nameof(glasswallVersionService));
            _fileTypeDetector = fileTypeDetector ?? throw new ArgumentNullException(nameof(fileTypeDetector));
            _fileProtector = fileProtector ?? throw new ArgumentNullException(nameof(fileProtector));
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public void ProcessFile()
        {
            var status = RebuildFile();
            SendMessage(status);
        }

        private string RebuildFile()
        {
            byte[] protectedFile = null;

            Console.WriteLine($"Using Glasswall Version: {_glasswallVersionService.GetVersion()}");

            var file = File.ReadAllBytes(_config.InputPath);

            var fileType = _fileTypeDetector.DetermineFileType(file);

            Console.WriteLine($"Filetype Detected for {_config.FileId}: {fileType.FileTypeName}");

            string status;
            if (fileType.FileType == FileType.Unknown)
            {
                status = FileOutcome.Unmodified;
            }
            else
            {
                var protectedFileResponse = _fileProtector.GetProtectedFile(GetDefaultContentManagement(), fileType.FileTypeName, file);

                if (!string.IsNullOrWhiteSpace(protectedFileResponse.ErrorMessage))
                {
                    if (protectedFileResponse.IsDisallowed)
                    {
                        status = FileOutcome.Unmodified;
                    }
                    else
                    {
                        status = FileOutcome.Failed;
                    }
                }
                else
                {
                    protectedFile = protectedFileResponse.ProtectedFile;
                    status = FileOutcome.Replace;
                }
            }

            Directory.CreateDirectory("/output");

            File.WriteAllBytes(_config.OutputPath, protectedFile ?? file);

            Console.WriteLine($"Status of {status} for {_config.FileId}");

            return status;
        }

        private void SendMessage(string status)
        {
            var factory = new ConnectionFactory() { HostName = "rabbitmq-service" };
            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                var headers = new Dictionary<string, object>()
                {
                    { "file-id", _config.FileId },
                    { "file-outcome", status },
                };

                var replyProps = channel.CreateBasicProperties();
                replyProps.Headers = headers;

                Console.Write($"ReplyTo: {_config.ReplyTo}, FileId: {_config.FileId}");

                channel.BasicPublish("", _config.ReplyTo, basicProperties: replyProps);
                Console.WriteLine($"Sent Message, FileId: {_config.FileId}, Outcome: {status}");
            };
        }

        private ContentManagementFlags GetDefaultContentManagement()
        {
            return new ContentManagementFlags
            {
                ExcelContentManagement = new ExcelContentManagement
                {
                    DynamicDataExchange = ContentManagementFlagAction.Sanitise,
                    EmbeddedFiles = ContentManagementFlagAction.Sanitise,
                    EmbeddedImages = ContentManagementFlagAction.Sanitise,
                    ExternalHyperlinks = ContentManagementFlagAction.Sanitise,
                    InternalHyperlinks = ContentManagementFlagAction.Sanitise,
                    Macros = ContentManagementFlagAction.Sanitise,
                    Metadata = ContentManagementFlagAction.Sanitise,
                    ReviewComments = ContentManagementFlagAction.Sanitise
                },
                PdfContentManagement = new PdfContentManagement
                {
                    Acroform = ContentManagementFlagAction.Sanitise,
                    ActionsAll = ContentManagementFlagAction.Sanitise,
                    EmbeddedFiles = ContentManagementFlagAction.Sanitise,
                    EmbeddedImages = ContentManagementFlagAction.Sanitise,
                    ExternalHyperlinks = ContentManagementFlagAction.Sanitise,
                    InternalHyperlinks = ContentManagementFlagAction.Sanitise,
                    Javascript = ContentManagementFlagAction.Sanitise,
                    Metadata = ContentManagementFlagAction.Sanitise,
                    Watermark = "Glasswall Processed"
                },
                PowerPointContentManagement = new PowerPointContentManagement
                {
                    EmbeddedFiles = ContentManagementFlagAction.Sanitise,
                    EmbeddedImages = ContentManagementFlagAction.Sanitise,
                    ExternalHyperlinks = ContentManagementFlagAction.Sanitise,
                    InternalHyperlinks = ContentManagementFlagAction.Sanitise,
                    Macros = ContentManagementFlagAction.Sanitise,
                    Metadata = ContentManagementFlagAction.Sanitise,
                    ReviewComments = ContentManagementFlagAction.Sanitise
                },
                WordContentManagement = new WordContentManagement
                {
                    DynamicDataExchange = ContentManagementFlagAction.Sanitise,
                    EmbeddedFiles = ContentManagementFlagAction.Sanitise,
                    EmbeddedImages = ContentManagementFlagAction.Sanitise,
                    ExternalHyperlinks = ContentManagementFlagAction.Sanitise,
                    InternalHyperlinks = ContentManagementFlagAction.Sanitise,
                    Macros = ContentManagementFlagAction.Sanitise,
                    Metadata = ContentManagementFlagAction.Sanitise,
                    ReviewComments = ContentManagementFlagAction.Sanitise
                }
            };
        }
    }
}
