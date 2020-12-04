namespace Service.Messaging
{
    public interface IArchiveRequestSender
    {
        void Send(string fileId, string fileType, string sourceLocation, string rebuiltLocation, string replyTo);
    }
}
