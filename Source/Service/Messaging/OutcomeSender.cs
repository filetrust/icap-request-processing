using RabbitMQ.Client;
using System;
using System.Collections.Generic;

namespace Service.Messaging
{
    public class OutcomeSender : IOutcomeSender
    {
        private const string HostName = "rabbitmq-service";

        private IModel _channel;

        public OutcomeSender()
        {
            var connectionFactory = new ConnectionFactory() { HostName = HostName };
            var connection = connectionFactory.CreateConnection();
            _channel = connection.CreateModel();

            Console.WriteLine($"Connection established to {HostName}");
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
