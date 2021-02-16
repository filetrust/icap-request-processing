using Glasswall.Core.Engine.Common.FileProcessing;
using Glasswall.Core.Engine.Common.PolicyConfig;
using Glasswall.Core.Engine.Messaging;
using Microsoft.Extensions.Logging;
using System;

namespace Service.Engine
{
    public class GlasswallEngineService : IGlasswallEngineService
    {
        private readonly IGlasswallVersionService _glasswallVersionService;
        private readonly IFileTypeDetector _fileTypeDetector;
        private readonly IFileProtector _fileProtector;
        private readonly IFileAnalyser _fileAnalyser;
        private readonly ILogger<GlasswallEngineService> _logger;

        public GlasswallEngineService(IGlasswallVersionService glasswallVersionService, IFileTypeDetector fileTypeDetector, IFileProtector fileProtector, IFileAnalyser fileAnalyser, ILogger<GlasswallEngineService> logger)
        {
            _glasswallVersionService = glasswallVersionService ?? throw new ArgumentNullException(nameof(glasswallVersionService));
            _fileTypeDetector = fileTypeDetector ?? throw new ArgumentNullException(nameof(fileTypeDetector));
            _fileProtector = fileProtector ?? throw new ArgumentNullException(nameof(fileProtector));
            _fileAnalyser = fileAnalyser ?? throw new ArgumentNullException(nameof(fileAnalyser));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public string GetGlasswallVersion()
        {
            return _glasswallVersionService.GetVersion();
        }

        public FileTypeDetectionResponse GetFileType(byte[] file, string fileId)
        {
            var fileType = _fileTypeDetector.DetermineFileType(file);

            _logger.LogInformation($"FileId: {fileId}, Filetype Detected: {fileType.FileTypeName}");

            return fileType;
        }

        public string AnalyseFile(byte[] file, string fileType, string fileId)
        {
            return _fileAnalyser.GetReport(GetDefaultContentManagement(), fileType, file);
        }

        public byte[] RebuildFile(byte[] file, string fileType, string fileId, ContentManagementFlags contentManagementFlags)
        {
            var protectedFileResponse = _fileProtector.GetProtectedFile(contentManagementFlags ?? GetDefaultContentManagement(), fileType, file);

            if (!string.IsNullOrWhiteSpace(protectedFileResponse.ErrorMessage) || protectedFileResponse.ProtectedFile == null)
            {
                if (protectedFileResponse.IsDisallowed)
                    _logger.LogInformation($"File {fileId} is disallowed by Content Management Policy");

                _logger.LogInformation($"File {fileId} could not be rebuilt: {protectedFileResponse.ErrorMessage}.");

                return new byte[0];
            }
            else
            {
                _logger.LogInformation($"FileId: {fileId}, successfully rebuilt.");

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
