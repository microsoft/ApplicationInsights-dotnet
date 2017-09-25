namespace FunctionalTestUtils
{
    using System;
    using System.IO;
    using Microsoft.AspNetCore.Hosting;
    using AI;

    // a variant of aspnet/Hosting/test/Microsoft.AspNetCore.Hosting.Tests/HostingEngineTests.cs
    public class InProcessServer : IDisposable
    {
        private const string httpListenerConnectionString = "http://localhost:4001/v2/track/";

        private static Random random = new Random();
        private static object noParallelism = new object();

        public static Func<IWebHostBuilder, IWebHostBuilder> UseApplicationInsights =
            builder => builder.UseApplicationInsights();

        private readonly Func<IWebHostBuilder, IWebHostBuilder> configureHost;
        private IWebHost hostingEngine;
        private string url;

        private TelemetryHttpListenerObservable listener;       
        

        public InProcessServer(string assemblyName, Func<IWebHostBuilder, IWebHostBuilder> configureHost = null)
        {
            this.configureHost = configureHost;

            var machineName = Environment.GetEnvironmentVariable("COMPUTERNAME");
            this.url = "http://" + machineName + ":" + random.Next(5000, 14000).ToString();           

            this.Start(assemblyName);
        }

        public TelemetryHttpListenerObservable Listener
        {
            get
            {
                return this.listener;
            }
        }

        public string BaseHost
        {
            get
            {
                return this.url;
            }
        }

        public T[] Execute<T>(Func<T[]> receiveItemsMethod)
        {
            lock (noParallelism)
            {
                try
                {
                    this.listener = new TelemetryHttpListenerObservable(httpListenerConnectionString);
                    this.listener.Start();

                    if (receiveItemsMethod != null)
                    {
                        return receiveItemsMethod();
                    }
                }
                finally
                {
                    if (this.listener != null)
                    {
                        this.listener.Stop();
                    }
                }
            }

            return null;
        }

        public IServiceProvider ApplicationServices { get; private set; }

        private void Start(string assemblyName)
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
