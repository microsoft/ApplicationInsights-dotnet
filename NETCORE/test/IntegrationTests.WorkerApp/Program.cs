using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace IntegrationTests.WorkerApp
{
    public static class Program
    {
        public static Task Main(string[] args)
        {
            return CreateHostBuilder(args).Build().RunAsync();
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                .ConfigureServices((context, services) =>
                {
                    services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();
                    services.AddHostedService<QueuedHostedService>();
                    services.AddHttpClient();
                });
        }
    }
}
