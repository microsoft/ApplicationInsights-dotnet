namespace FunctionalTestUtils
{
    using System;
    using Microsoft.AspNet.Hosting;
    using Microsoft.AspNet.Server.WebListener;
    using Microsoft.Framework.ConfigurationModel;
    using Microsoft.Framework.Logging;

    public class InProcessServer : IDisposable
    {
        private IDisposable hostingEngine;
        private string url = "http://localhost:" + (new Random(239).Next(5000, 8000)).ToString();
        
        public InProcessServer(string assemblyName)
        {
            this.Start(assemblyName);
        }

        public string BaseHost
        {
            get
            {
                return this.url;
            }
        }

        private void Start(string assemblyName)
        {
            var customConfig = new NameValueConfigurationSource();
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
