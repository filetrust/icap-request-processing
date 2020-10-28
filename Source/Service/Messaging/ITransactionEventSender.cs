using Service.StoreMessages.Events;

namespace Service.Messaging
{
    public interface ITransactionEventSender
    {
        void Send(Event transactionEvent);
    }
}
