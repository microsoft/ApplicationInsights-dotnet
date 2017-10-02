namespace FunctionalTestUtils
{
    using System;
    using System.IO;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.DependencyInjection;
    using Xunit.Abstractions;

    public class EndpointAddress
    {
        private const string httpListenerConnectionString = "http://localhost:{0}/v2/track/";
        private static Random random = new Random();

        public EndpointAddress()
        {
            this.ConnectionString = string.Format(httpListenerConnectionString, random.Next(2000, 5000).ToString());
        }

        public string ConnectionString { get; }
    }

    // a variant of aspnet/Hosting/test/Microsoft.AspNetCore.Hosting.Tests/HostingEngineTests.cs
    public class InProcessServer : IDisposable
    {
        private readonly string httpListenerConnectionString;
        private readonly ITestOutputHelper output;

        private static Random random = new Random();
        private static object noParallelism = new object();

        private IWebHost hostingEngine;
        private string url;

        private TelemetryHttpListenerObservable listener;       
        

        public InProcessServer(string assemblyName, ITestOutputHelper output)
        {
            this.output = output;

            var machineName = Environment.GetEnvironmentVariable("COMPUTERNAME");
            this.url = "http://" + machineName + ":" + random.Next(5000, 14000).ToString();

            output.WriteLine(string.Format("{0}: Launching application at: {1}", DateTime.Now.ToString("G"), this.url));

            this.httpListenerConnectionString = this.Start(assemblyName);

            output.WriteLine(string.Format("{0}: Starting listener at: {1}", DateTime.Now.ToString("G"), this.httpListenerConnectionString));

            this.listener = new TelemetryHttpListenerObservable(this.httpListenerConnectionString);
            this.listener.Start();
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

        public IServiceProvider ApplicationServices { get; private set; }

        private string Start(string assemblyName)
        {
            var builder = new WebHostBuilder()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseUrls(this.BaseHost)
                .UseKestrel()
                .UseStartup(assemblyName)
                .UseEnvironment("Production");

            this.hostingEngine = builder.Build();
            this.hostingEngine.Start();

            this.ApplicationServices = this.hostingEngine.Services;

            return ((EndpointAddress)this.hostingEngine.Services.GetService<EndpointAddress>()).ConnectionString;
        }

        public void Dispose()
        {
            if (this.hostingEngine != null)
            {
                this.hostingEngine.Dispose();
            }

            if (this.listener != null)
            {
                output.WriteLine(string.Format("{0}: Stopping listener at: {1}", DateTime.Now.ToString("G"), this.httpListenerConnectionString));
                this.listener.Stop();
            }
        }
    }
}
