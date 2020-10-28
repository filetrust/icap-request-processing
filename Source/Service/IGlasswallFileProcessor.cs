using Glasswall.Core.Engine.Messaging;

namespace Service
{
    public interface IGlasswallFileProcessor
    {
        FileTypeDetectionResponse GetFileType(byte[] file);
        string AnalyseFile(string fileType, byte[] file);
        string RebuildFile(byte[] file, string fileType);
    }
}
