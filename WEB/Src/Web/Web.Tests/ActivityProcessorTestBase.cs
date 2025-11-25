namespace Microsoft.ApplicationInsights.Web.Tests
{
    using System;
    using System.Diagnostics;
    using System.Web;
    using OpenTelemetry;
    using OpenTelemetry.Trace;

    /// <summary>
    /// Base class for ActivityProcessor tests that sets up TracerProvider and ActivitySource.
    /// </summary>
    public abstract class ActivityProcessorTestBase : IDisposable
    {
        private const string TestActivitySourceName = "Microsoft.ApplicationInsights.Web.Tests";
        private static readonly ActivitySource TestActivitySource = new ActivitySource(TestActivitySourceName);
        
        private TracerProvider tracerProvider;

        protected ActivityProcessorTestBase()
        {
            HttpContext.Current = null;
        }

        /// <summary>
        /// Creates a TracerProvider with the given processor.
        /// </summary>
        protected void SetupTracerProvider(BaseProcessor<Activity> processor)
        {
            // Dispose existing provider if any
            this.tracerProvider?.Dispose();
            
            this.tracerProvider = Sdk.CreateTracerProviderBuilder()
                .AddSource(TestActivitySourceName)
                .AddProcessor(processor)
                .Build();
        }

        /// <summary>
        /// Creates and starts an activity using the TestActivitySource.
        /// The activity will be automatically stopped when disposed.
        /// </summary>
        protected Activity StartTestActivity(string name = "TestActivity")
        {
            return TestActivitySource.StartActivity(name);
        }

        public virtual void Dispose()
        {
            // Cleanup: Stop any running activities
            while (Activity.Current != null)
            {
                Activity.Current.Stop();
            }

            // Dispose TracerProvider to flush and release resources
            this.tracerProvider?.Dispose();
            this.tracerProvider = null;

            // Clear HttpContext to prevent test pollution
            HttpContext.Current = null;
        }
    }
}
