namespace FunctionalTestUtils
{
    using Microsoft.AspNet.Hosting;
    using Microsoft.Framework.ConfigurationModel;
    using Microsoft.Framework.DependencyInjection;
    using Microsoft.Framework.DependencyInjection.Fallback;
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

            var serviceCollection = HostingServices.Create(null);
            var services = serviceCollection.BuildServiceProvider();

            var context = new HostingContext()
            {
                Services = services,
                Configuration = config,
                ServerName = "Microsoft.AspNet.Server.WebListener",
                ApplicationName = assemblyName,
                EnvironmentName = "Production"
            };

            var hostingEngine = services.GetService<IHostingEngine>();
            if (hostingEngine == null)
            {
                throw new Exception("TODO: IHostingEngine service not available exception");
            }

            this.hostingEngine = hostingEngine.Start(context);
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
