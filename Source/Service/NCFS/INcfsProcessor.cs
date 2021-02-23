using Glasswall.Core.Engine.Messaging;
using System;
using System.Threading.Tasks;

namespace Service.NCFS
{
    public interface INcfsProcessor
    {
        Task<NcfsOutcome> GetUnmanagedActionAsync(DateTime timestamp, string base64File, FileType fileType);
        Task<NcfsOutcome> GetBlockedActionAsync(DateTime timestamp, string base64File, FileType fileType);
    }
}
