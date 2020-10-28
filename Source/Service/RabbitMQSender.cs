using RabbitMQ.Client;
using Service.StoreMessages.Events;
using System;
using System.Collections.Generic;
using System.Text;

namespace Service
{
    public class RabbitMQSender : IMessageSender
    {
        private const string HostName = "rabbitmq-service";

        public void SendMessageAdaptationOutcome(string status, string fileId, string replyTo)
        {
            var factory = new ConnectionFactory() { HostName = HostName };
            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                var headers = new Dictionary<string, object>()
                {
                    { "file-id", fileId },
                    { "file-outcome", status },
                };

                var replyProps = channel.CreateBasicProperties();
                replyProps.Headers = headers;

                Console.Write($"ReplyTo: {replyTo}, FileId: {fileId}");

                channel.BasicPublish("", replyTo, basicProperties: replyProps);
                Console.WriteLine($"Sent Message, FileId: {fileId}, Outcome: {status}");
            };
        }

        public void SendTransactionEvent(Event transactionEvent)
        {
            const string exchange = "adaptation-exchange";
            const string routingKey = "transaction-event";

            var factory = new ConnectionFactory() { HostName = HostName };
            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.ExchangeDeclare(exchange, "direct", true);

                var message = Encoding.UTF8.GetBytes(transactionEvent.ToJson());

                channel.BasicPublish(exchange, routingKey, null, message);
                Console.WriteLine($"Sent Transaction Event, FileId: {transactionEvent.FileId}, EventId: {transactionEvent.EventId}");
            };
        }
    }
}
