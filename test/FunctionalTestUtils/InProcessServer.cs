namespace FunctionalTestUtils
{
    using Microsoft.AspNet.Hosting;
    using Microsoft.Framework.ConfigurationModel;
    using Microsoft.Framework.DependencyInjection;
    using Microsoft.Framework.DependencyInjection.Fallback;
    using System;

    public class InProcessServer
    {
        private string url = "http://localhost:" + (new Random(239).Next(5000, 8000)).ToString();
        private string assemblyName = "ConsoleTest";

        public InProcessServer(string currentAssemblyName)
        {
            this.assemblyName = currentAssemblyName;
        }

        public string BaseHost
        {
            get
            {
                return this.url;
            }
        }

        public IDisposable Start()
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
                ApplicationName = this.assemblyName,
                EnvironmentName = "Production"
            };

            var hostingEngine = services.GetService<IHostingEngine>();
            if (hostingEngine == null)
            {
                throw new Exception("TODO: IHostingEngine service not available exception");
            }

            return hostingEngine.Start(context);
        }
    }
}
