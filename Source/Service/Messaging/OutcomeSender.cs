using RabbitMQ.Client;
using System;
using System.Collections.Generic;

namespace Service.Messaging
{
    public class OutcomeSender : IOutcomeSender, IDisposable
    {
        private const string HostName = "rabbitmq-service";

        private bool disposedValue;

        private readonly IModel _channel;
        private readonly IConnection _connection;

        public OutcomeSender()
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

        public void Send(string status, string fileId, string replyTo)
        {
            var headers = new Dictionary<string, object>()
                {
                    { "file-id", fileId },
                    { "file-outcome", status },
                };

            var replyProps = _channel.CreateBasicProperties();
            replyProps.Headers = headers;

            Console.Write($"ReplyTo: {replyTo}, FileId: {fileId}");

            _channel.BasicPublish("", replyTo, basicProperties: replyProps);
            Console.WriteLine($"Sent Message, FileId: {fileId}, Outcome: {status}");
        }
    }
}
