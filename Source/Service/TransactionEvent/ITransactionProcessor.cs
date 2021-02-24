using System.Threading.Tasks;

namespace Service.TransactionEvent
{
    public interface ITransactionProcessor
    {
        Task Process();
    }
}