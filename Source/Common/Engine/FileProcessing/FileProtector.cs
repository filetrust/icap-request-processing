using System;
using System.Linq;
using Glasswall.Core.Engine.Common;
using Glasswall.Core.Engine.Common.FileProcessing;
using Glasswall.Core.Engine.Common.GlasswallEngineLibrary;
using Glasswall.Core.Engine.Common.PolicyConfig;

namespace Glasswall.Core.Engine.FileProcessing
{
    public class FileProtector : IFileProtector
    {
        private readonly IGlasswallFileOperations _glasswallFileOperations;
        private readonly IAdaptor<ContentManagementFlags, string> _glasswallConfigurationAdaptor;

        public FileProtector(IGlasswallFileOperations glasswallFileOperations,
            IAdaptor<ContentManagementFlags, string> glasswallConfigurationAdaptor)
        {
            _glasswallFileOperations = glasswallFileOperations ?? throw new ArgumentNullException(nameof(glasswallFileOperations));
            _glasswallConfigurationAdaptor = glasswallConfigurationAdaptor ?? throw new ArgumentNullException(nameof(glasswallConfigurationAdaptor));
        }

        public IFileProtectResponse GetProtectedFile(ContentManagementFlags contentManagementFlags, string fileType, byte[] fileBytes)
        {
            var response = new FileProtectResponse { ProtectedFile = Enumerable.Empty<byte>().ToArray() };

            var glasswallConfiguration = _glasswallConfigurationAdaptor.Adapt(contentManagementFlags);
            var configurationOutcome = _glasswallFileOperations.SetConfiguration(glasswallConfiguration);
            if (configurationOutcome != EngineOutcome.Success)
            {
                response.Outcome = configurationOutcome;
                return response;
            }

            var engineOutcome = _glasswallFileOperations.ProtectFile(fileBytes, fileType, out var protectedFile);
            response.Outcome = engineOutcome;
            response.ProtectedFile = protectedFile;

            if (engineOutcome != EngineOutcome.Success)
            {
                response.ErrorMessage = _glasswallFileOperations.GetEngineError();
            }

            return response;
        }
    }
}
