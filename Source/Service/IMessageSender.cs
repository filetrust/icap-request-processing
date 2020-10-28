using Service.StoreMessages.Events;

namespace Service
{
    public interface IMessageSender
    {
        void SendMessageAdaptationOutcome(string status, string fileId, string replyTo);
        void SendTransactionEvent(Event transactionEvent);
    }
}
