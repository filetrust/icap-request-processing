using Glasswall.Core.Engine.Messaging;
using System.Threading.Tasks;

namespace Service.NCFS
{
    public interface INcfsClient
    {
        Task<NcfsOutcome> GetOutcome(string base64Body, FileType fileType);
    }
}
