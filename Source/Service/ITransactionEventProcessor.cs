using System.Threading.Tasks;

namespace Service
{
    public interface ITransactionEventProcessor
    {
        void Process();
    }
}