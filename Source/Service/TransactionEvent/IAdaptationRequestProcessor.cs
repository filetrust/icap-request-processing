using System.Threading.Tasks;

namespace Service.TransactionEvent
{
    public interface IAdaptationRequestProcessor
    {
        Task Process();
    }
}