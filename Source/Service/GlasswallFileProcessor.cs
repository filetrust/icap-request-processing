using Glasswall.Core.Engine.Common.FileProcessing;
using Glasswall.Core.Engine.Common.PolicyConfig;
using Glasswall.Core.Engine.Messaging;
using Service.StoreMessages;
using System;
using System.IO;

namespace Service
{
    public class GlasswallFileProcessor : IGlasswallFileProcessor
    {
        private readonly IFileTypeDetector _fileTypeDetector;
        private readonly IFileProtector _fileProtector;
        private readonly IFileAnalyser _fileAnalyser;
        private readonly IFileProcessorConfig _config;

        public GlasswallFileProcessor(IFileTypeDetector fileTypeDetector, IFileProtector fileProtector, IFileAnalyser fileAnalyser, IFileProcessorConfig config)
        {
            _fileTypeDetector = fileTypeDetector ?? throw new ArgumentNullException(nameof(fileTypeDetector));
            _fileProtector = fileProtector ?? throw new ArgumentNullException(nameof(fileProtector));
            _fileAnalyser = fileAnalyser ?? throw new ArgumentNullException(nameof(fileAnalyser));
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public FileTypeDetectionResponse GetFileType(byte[] file)
        {
            var fileType = _fileTypeDetector.DetermineFileType(file);

            Console.WriteLine($"Filetype Detected for {_config.FileId}: {fileType.FileTypeName}");

            return fileType;
        }

        public string AnalyseFile(string fileType, byte[] file)
        {
            return _fileAnalyser.GetReport(GetDefaultContentManagement(), fileType, file);
        }

        public string RebuildFile(byte[] file, string fileType)
        {
            string status;
            byte[] protectedFile = null;

            var protectedFileResponse = _fileProtector.GetProtectedFile(GetDefaultContentManagement(), fileType, file);

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

            File.WriteAllBytes(_config.OutputPath, protectedFile ?? file);

            Console.WriteLine($"Status of {status} for {_config.FileId}");

            return status;
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
