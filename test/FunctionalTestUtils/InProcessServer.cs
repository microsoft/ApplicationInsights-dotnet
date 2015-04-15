namespace FunctionalTestUtils
{
    using Microsoft.AspNet.Hosting;
    using Microsoft.Framework.ConfigurationModel;
    using Microsoft.Framework.DependencyInjection;
    using System;

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

            var serviceCollection = new ServiceCollection();
            var services = serviceCollection.BuildServiceProvider();

            var engine = WebHost.CreateEngine(services, config)
                .UseEnvironment("Production")
                .UseServer("Microsoft.AspNet.Server.WebListener")
                .UseStartup(assemblyName);
            this.hostingEngine = engine.Start();
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
