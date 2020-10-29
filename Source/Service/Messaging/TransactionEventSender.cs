using RabbitMQ.Client;
using Service.StoreMessages.Events;
using System;
using System.Text;

namespace Service.Messaging
{
    public class TransactionEventSender : ITransactionEventSender
    {
        private const string HostName = "rabbitmq-service";
        private const string Exchange = "adaptation-exchange";
        private const string RoutingKey = "transaction-event";

        private readonly IModel _channel;

        public TransactionEventSender()
        {
            var connectionFactory = new ConnectionFactory() { HostName = HostName };
            var connection = connectionFactory.CreateConnection();
            _channel = connection.CreateModel();

            Console.WriteLine($"Connection established to {HostName}");
        }

        public void Send(Event transactionEvent)
        {
            _channel.ExchangeDeclare(Exchange, "direct", true);

            var message = Encoding.UTF8.GetBytes(transactionEvent.ToJson());

            _channel.BasicPublish(Exchange, RoutingKey, null, message);
            Console.WriteLine($"Sent Transaction Event, FileId: {transactionEvent.FileId}, EventId: {transactionEvent.EventId}");
        }
    }
}
