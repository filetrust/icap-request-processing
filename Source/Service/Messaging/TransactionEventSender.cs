using RabbitMQ.Client;
using Service.StoreMessages.Events;
using System;
using System.Text;

namespace Service.Messaging
{
    public class TransactionEventSender : ITransactionEventSender, IDisposable
    {
        private const string Exchange = "adaptation-exchange";
        private const string RoutingKey = "transaction-event";

        private bool disposedValue;

        private readonly IModel _channel;
        private readonly IConnection _connection;

        public TransactionEventSender(IFileProcessorConfig fileProcessorConfig)
        {
            if (fileProcessorConfig == null) throw new ArgumentNullException(nameof(fileProcessorConfig));
            var connectionFactory = new ConnectionFactory()
            {
                HostName = fileProcessorConfig.TransactionEventQueueHostname,
                Port = fileProcessorConfig.TransactionEventQueuePort,
                UserName = fileProcessorConfig.MessageBrokerUser,
                Password = fileProcessorConfig.MessageBrokerPassword
            };
            _connection = connectionFactory.CreateConnection();
            _channel = _connection.CreateModel();

            Console.WriteLine($"TransactionEventSender Connection established to {fileProcessorConfig.TransactionEventQueueHostname}");
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
