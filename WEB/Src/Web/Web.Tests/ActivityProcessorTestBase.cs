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
        protected static readonly ActivitySource TestActivitySource = new ActivitySource("Microsoft.ApplicationInsights.Web.Tests");
        protected TracerProvider TracerProvider { get; set; }

        protected ActivityProcessorTestBase()
        {
            HttpContext.Current = null;
        }

        /// <summary>
        /// Creates a TracerProvider with the given processor.
        /// </summary>
        protected void SetupTracerProvider(BaseProcessor<Activity> processor)
        {
            TracerProvider = Sdk.CreateTracerProviderBuilder()
                .AddSource(TestActivitySource.Name)
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
            // Cleanup: Stop any running activities and clear HttpContext
            while (Activity.Current != null)
            {
                Activity.Current.Stop();
            }

            HttpContext.Current = null;
            TracerProvider?.Dispose();
        }
    }
}
