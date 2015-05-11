namespace FunctionalTestUtils
{
    using System;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.AspNet.Hosting;
    using Microsoft.AspNet.Server.WebListener;
    using Microsoft.Framework.ConfigurationModel;
    using Microsoft.Framework.DependencyInjection;
    using Microsoft.Framework.Logging;

    public class InProcessServer : IDisposable
    {
        private static Random random = new Random();

        private IDisposable hostingEngine;
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
            var customConfig = new MemoryConfigurationSource();
            customConfig.Set("server.urls", this.BaseHost);
            var config = new Configuration();
            config.Add(customConfig);
            
            var context = new HostingContext
            {
                Configuration = config,
                ServerFactory = new ServerFactory(new LoggerFactory()),
                ApplicationName = assemblyName
            };

            this.hostingEngine = new HostingEngine().Start(context);

            return (BackTelemetryChannel)context.ApplicationServices.GetService<ITelemetryChannel>();
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
