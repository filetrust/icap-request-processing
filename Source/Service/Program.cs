using Microsoft.Extensions.DependencyInjection;
using Prometheus;

namespace Service
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var services = new ServiceCollection();

            var startup = new Startup();
            startup.ConfigureServices(services);
            var serviceProvider = services.BuildServiceProvider();

            // Get Service and call method
            using (var scope = serviceProvider.CreateScope())
            {
                var configuration = scope.ServiceProvider.GetService<IFileProcessorConfig>();

                var pusher = new MetricPusher(new MetricPusherOptions
                {
                    Endpoint = configuration.MetricsEndpoint,
                    Job = "icap-request-processing",
                    IntervalMilliseconds = 5,
                    Instance = configuration.FileId
                });

                pusher.Start();

                var service = scope.ServiceProvider.GetService<ITransactionEventProcessor>();
                service.Process();

                pusher.Stop();
            }
        }
    }
}