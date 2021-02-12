using Glasswall.Core.Engine.Common.PolicyConfig;
using Glasswall.Core.Engine.Messaging;

namespace Service.Engine
{
    public interface IGlasswallEngineService
    {
        string GetGlasswallVersion();
        FileTypeDetectionResponse GetFileType(byte[] file, string fileId);
        string AnalyseFile(byte[] file, string fileType, string fileId);
        byte[] RebuildFile(byte[] file, string fileType, string fileId, ContentManagementFlags contentManagementFlags);
    }
}
