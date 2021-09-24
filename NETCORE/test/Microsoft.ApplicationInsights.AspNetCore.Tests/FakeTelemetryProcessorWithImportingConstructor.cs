namespace Microsoft.ApplicationInsights.AspNetCore.Tests
{
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Hosting;

    internal class FakeTelemetryProcessorWithImportingConstructor : ITelemetryProcessor
    {
        private readonly ITelemetryProcessor nextProcessor;

#if NETCOREAPP
        public IHostEnvironment HostingEnvironment { get; }

        /// <summary>
        /// Constructs an instance of the telemetry processor.
        /// This constructor is designed to be called from a DI framework.
        /// </summary>
        /// <param name="next">The next procesor in the chain.</param>
        /// <param name="hostingEnvironment">The hosting environment. This parameter will be injected by the DI framework.</param>
        public FakeTelemetryProcessorWithImportingConstructor(ITelemetryProcessor next, IHostEnvironment hostingEnvironment)
        {
            this.nextProcessor = next;
            this.HostingEnvironment = hostingEnvironment;
        }
#else
        public Microsoft.AspNetCore.Hosting.IHostingEnvironment HostingEnvironment { get; }

        /// <summary>
        /// Constructs an instance of the telemetry processor.
        /// This constructor is designed to be called from a DI framework.
        /// </summary>
        /// <param name="next">The next procesor in the chain.</param>
        /// <param name="hostingEnvironment">The hosting environment. This parameter will be injected by the DI framework.</param>
        public FakeTelemetryProcessorWithImportingConstructor(ITelemetryProcessor next, Microsoft.AspNetCore.Hosting.IHostingEnvironment hostingEnvironment)
        {
            this.nextProcessor = next;
            this.HostingEnvironment = hostingEnvironment;
        }
#endif

        public void Process(ITelemetry item)
        {
            nextProcessor.Process(item);
        }
    }
}
