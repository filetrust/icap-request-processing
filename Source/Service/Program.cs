using Glasswall.CloudSdk.AWS.Rebuild;
using Microsoft.Extensions.DependencyInjection;

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
            var service = serviceProvider.GetService<ITransactionEventProcessor>();
            service.Process();
        }
    }
}
