using Glasswall.Core.Engine.Common.FileProcessing;
using Glasswall.Core.Engine.Common.PolicyConfig;
using Glasswall.Core.Engine.Messaging;
using System;

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

        public string AnalyseFile(byte[] file, string fileType)
        {
            return _fileAnalyser.GetReport(GetDefaultContentManagement(), fileType, file);
        }

        public byte[] RebuildFile(byte[] file, string fileType)
        {
            var protectedFileResponse = _fileProtector.GetProtectedFile(_config.ContentManagementFlags ?? GetDefaultContentManagement(), fileType, file);

            if (!string.IsNullOrWhiteSpace(protectedFileResponse.ErrorMessage) || protectedFileResponse.ProtectedFile == null)
            {
                if (protectedFileResponse.IsDisallowed)
                    Console.WriteLine($"File {_config.FileId} is disallowed by Content Management Policy");

                Console.WriteLine($"File {_config.FileId} could not be rebuilt.");

                return null;
            }
            else
            {
                Console.WriteLine($"File {_config.FileId} successfully rebuilt.");

                return protectedFileResponse.ProtectedFile;
            }
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
