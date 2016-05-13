namespace FunctionalTestUtils
{
    using System;
    using System.IO;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Configuration;

    // a variant of aspnet/Hosting/test/Microsoft.AspNetCore.Hosting.Tests/HostingEngineTests.cs
    public class InProcessServer : IDisposable
    {
        private static Random random = new Random();
        
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
        
        public InProcessServer(string assemblyName)
        {
            this.url = "http://localhost:" + random.Next(5000, 14000).ToString();
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
            this.hostingEngine = CreateBuilder()
                 .UseContentRoot(Directory.GetCurrentDirectory())
                .UseUrls(this.BaseHost)
                .UseKestrel()
                .UseStartup(assemblyName)
                .UseEnvironment("Production")
                .Build();

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
        
        private WebHostBuilder CreateBuilder()
        {
            var config = new ConfigurationBuilder()
                .Build();

            var hostBuilder = new WebHostBuilder();
            hostBuilder.UseConfiguration(config);
            return hostBuilder;
        }
    }
}
