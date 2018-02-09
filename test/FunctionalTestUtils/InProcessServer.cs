namespace FunctionalTestUtils
{
    using System;
    using System.IO;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.DependencyInjection;

    // a variant of aspnet/Hosting/test/Microsoft.AspNetCore.Hosting.Tests/HostingEngineTests.cs
    public class InProcessServer : IDisposable
    {
        private static Random random = new Random();

        public static Func<IWebHostBuilder, IWebHostBuilder> UseApplicationInsights =
            builder => builder.UseApplicationInsights();

        private readonly Func<IWebHostBuilder, IWebHostBuilder> configureHost;
        private IWebHost hostingEngine;
        private string url;

        private readonly BackTelemetryChannel backChannel;

        public BackTelemetryChannel BackChannel
        {
            get
            {
                return this.backChannel;
            }
        }

        public InProcessServer(string assemblyName, Func<IWebHostBuilder, IWebHostBuilder> configureHost = null)
        {
            this.configureHost = configureHost;
            // localhost instead of machine name, as its not possible to get machine name when running non windows.
            var machineName = "localhost";
            this.url = "http://" + machineName + ":" + random.Next(5000, 14000).ToString();
            this.backChannel = this.Start(assemblyName);
        }

        public string BaseHost
        {
            get
            {
                return this.url;
            }
        }

        public IServiceProvider ApplicationServices { get; private set; }

        private BackTelemetryChannel Start(string assemblyName)
        {
            var builder = new WebHostBuilder()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseUrls(this.BaseHost)
                .UseKestrel()
                .UseStartup(assemblyName)
                .UseEnvironment("Production");
            if (configureHost != null)
            {
                builder = configureHost(builder);
            }

            this.hostingEngine = builder.Build();

            this.hostingEngine.Start();

            this.ApplicationServices = this.hostingEngine.Services;
            return (BackTelemetryChannel)this.hostingEngine.Services.GetService<ITelemetryChannel>();
        }

        public void Dispose()
        {
            if (this.hostingEngine != null)
            {
                this.hostingEngine.Dispose();
            }
        }
    }
}
