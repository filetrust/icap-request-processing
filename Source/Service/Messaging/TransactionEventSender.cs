using RabbitMQ.Client;
using Service.StoreMessages.Events;
using System;
using System.Text;

namespace Service.Messaging
{
    public class TransactionEventSender : ITransactionEventSender, IDisposable
    {
        private const string HostName = "rabbitmq-service";
        private const string Exchange = "adaptation-exchange";
        private const string RoutingKey = "transaction-event";

        private bool disposedValue;

        private readonly IModel _channel;
        private readonly IConnection _connection;

        public TransactionEventSender()
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

        public void Send(Event transactionEvent)
        {
            _channel.ExchangeDeclare(Exchange, "direct", true);

            var message = Encoding.UTF8.GetBytes(transactionEvent.ToJson());

            _channel.BasicPublish(Exchange, RoutingKey, null, message);
            Console.WriteLine($"Sent Transaction Event, FileId: {transactionEvent.FileId}, EventId: {transactionEvent.EventId}");
        }
    }
}
