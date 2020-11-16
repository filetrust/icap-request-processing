using Service.StoreMessages.Events;

namespace Service.Messaging
{
    public interface IArchiveRequestSender
    {
        void Send(string fileId, string sourceLocation, string rebuiltLocation, string replyTo);
    }
}
