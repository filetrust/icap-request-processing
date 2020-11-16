using RabbitMQ.Client;
using System;
using System.Collections.Generic;

namespace Service.Messaging
{
    public class ArchiveRequestSender : IArchiveRequestSender, IDisposable
    {
        private const string HostName = "localhost";
        private const string Exchange = "adaptation-exchange";
        private const string RoutingKey = "archive-adaptation-request";

        private bool disposedValue;

        private readonly IModel _channel;
        private readonly IConnection _connection;

        public ArchiveRequestSender()
        {
            var connectionFactory = new ConnectionFactory() { HostName = HostName };
            _connection = connectionFactory.CreateConnection();
            _channel = _connection.CreateModel();

            Console.WriteLine($"Connection established to {HostName}");
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _channel?.Dispose();
                    _connection?.Dispose();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public void Send(string fileId, string sourceLocation, string rebuiltLocation, string replyTo)
        {
            var headers = new Dictionary<string, object>()
                {
                    { "archive-file-id", fileId },
                    { "source-file-location", sourceLocation },
                    { "rebuilt-file-location", rebuiltLocation },
                    { "outcome-reply-to", replyTo }
                };

            var replyProps = _channel.CreateBasicProperties();
            replyProps.Headers = headers;
            _channel.BasicPublish(Exchange, RoutingKey, basicProperties: replyProps);

            Console.WriteLine($"Sent Archive Request, FileId: {fileId}, SourceFileLocation: {sourceLocation}, " +
                $"RebuiltFileLocation: {rebuiltLocation}, ReplyTo: {replyTo}");
        }
    }
}
