namespace FunctionalTestUtils
{
    using System;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.AspNet.Hosting;
    using Microsoft.AspNet.Server.WebListener;
    using Microsoft.Framework.Configuration;
    using Microsoft.Framework.DependencyInjection;
    using Microsoft.Framework.Logging;
    using Microsoft.AspNet.Hosting.Internal;
    using Microsoft.Framework.Runtime;
    using Microsoft.AspNet.Hosting.Server;
    using Microsoft.AspNet.FeatureModel;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    using Microsoft.Framework.Runtime.Infrastructure;

    // a variant of aspnet/Hosting/test/Microsoft.AspNet.Hosting.Tests/HostingEngineTests.cs
    public class InProcessServer : IDisposable, IServerFactory
    {
        private readonly IList<StartInstance> _startInstances = new List<StartInstance>();
        private static Random random = new Random();
        
        private IFeatureCollection _featuresSupportedByThisHost = new FeatureCollection();
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
            var config = new ConfigurationSection();
            config.Add(customConfig);

            var services = new ServiceCollection();
            services.AddTransient<IApplicationEnvironment, ApplicationEnvironment>();
            var serviceProvider = services.BuildServiceProvider();

            var engine = CreateBuilder(config)
                .UseServer("Microsoft.AspNet.Server.WebListener")
                .UseStartup(assemblyName)
                .UseEnvironment("Production")
                .Build();
            this.hostingEngine = engine.Start();
            
            return (BackTelemetryChannel)engine.ApplicationServices.GetService<ITelemetryChannel>();
        }

        public void Dispose()
        {
            if (this.hostingEngine != null)
            {
                this.hostingEngine.Dispose();
            }
        }

        public IServerInformation Initialize(IConfiguration configuration)
        {
            return null;
        }

        public IDisposable Start(IServerInformation serverInformation, Func<IFeatureCollection, Task> application)
        {
            var startInstance = new StartInstance(application);
            _startInstances.Add(startInstance);
            application(_featuresSupportedByThisHost);
            return startInstance;
        }

        private WebHostBuilder CreateBuilder(IConfiguration config)
        {
            return new WebHostBuilder(CallContextServiceLocator.Locator.ServiceProvider, config);
        }

        public class StartInstance : IDisposable
        {
            private readonly Func<IFeatureCollection, Task> _application;

            public StartInstance(Func<IFeatureCollection, Task> application)
            {
                _application = application;
            }

            public int DisposeCalls { get; set; }

            public void Dispose()
            {
                DisposeCalls += 1;
            }
        }
    }
}
