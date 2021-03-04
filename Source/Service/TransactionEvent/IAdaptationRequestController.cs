using System.Threading.Tasks;

namespace Service.TransactionEvent
{
    public interface IAdaptationRequestController
    {
        Task ProcessRequest(AdaptationContext context);
    }
}