namespace FunctionalTestUtils
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Hosting.Server;
    using Microsoft.AspNetCore.Http.Features;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Configuration.Memory;
    using Microsoft.Extensions.PlatformAbstractions;

    // a variant of aspnet/Hosting/test/Microsoft.AspNet.Hosting.Tests/HostingEngineTests.cs
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

        private BackTelemetryChannel Start(string assemblyName)
        {
            var customConfig = new MemoryConfigurationProvider();
            customConfig.Set("server.urls", this.BaseHost);
            var configBuilder = new ConfigurationBuilder();
            configBuilder.Add(customConfig);
            var config = configBuilder.Build();

            this.hostingEngine = CreateBuilder(config)
                .UseServer("Microsoft.AspNetCore.Server.WebListener")
                .UseStartup(assemblyName)
                .UseEnvironment("Production")
                .Build();
            this.hostingEngine.Start();
            
            
            return (BackTelemetryChannel)this.hostingEngine.Services.GetService<ITelemetryChannel>();
        }

        public void Dispose()
        {
            if (this.hostingEngine != null)
            {
                this.hostingEngine.Dispose();
            }
        }
        
        private WebHostBuilder CreateBuilder(IConfiguration config)
        {
            var hostBuilder = new WebHostBuilder();
            hostBuilder.UseConfiguration(config);
            return hostBuilder;
        }
    }
}
