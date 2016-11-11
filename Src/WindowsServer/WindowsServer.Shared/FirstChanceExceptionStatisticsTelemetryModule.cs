namespace Microsoft.ApplicationInsights.WindowsServer
{
    using System;
    using System.Diagnostics;
    using System.Runtime.ExceptionServices;
    using Extensibility.Implementation.Tracing;
    using Implementation;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.ApplicationInsights.Web.Implementation;

    /// <summary>
    /// The module subscribed to AppDomain.CurrentDomain.FirstChanceException to send exceptions statistics to ApplicationInsights.
    /// </summary>
    public sealed class FirstChanceExceptionStatisticsTelemetryModule : ITelemetryModule, IDisposable
    {
        private const int LOCKED = 1;

        /// <summary>
        /// This object prevents double entry into the exception callback.
        /// </summary>
        [ThreadStatic]
        private static int executionSyncObject;

        private readonly Action<EventHandler<FirstChanceExceptionEventArgs>> registerAction;
        private readonly Action<EventHandler<FirstChanceExceptionEventArgs>> unregisterAction;
        private readonly object lockObject = new object();

        private TelemetryClient telemetryClient;

        private bool isInitialized = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="FirstChanceExceptionStatisticsTelemetryModule" /> class.
        /// </summary>
        public FirstChanceExceptionStatisticsTelemetryModule() : this(
            action => AppDomain.CurrentDomain.FirstChanceException += action,
            action => AppDomain.CurrentDomain.FirstChanceException -= action)
        {
        }

        internal FirstChanceExceptionStatisticsTelemetryModule(
            Action<EventHandler<FirstChanceExceptionEventArgs>> registerAction,
            Action<EventHandler<FirstChanceExceptionEventArgs>> unregisterAction)
        {
            this.registerAction = registerAction;
            this.unregisterAction = unregisterAction;
        }

        /// <summary>
        /// Initializes the telemetry module.
        /// </summary>
        /// <param name="configuration">Telemetry Configuration used for creating TelemetryClient for sending exception statistics to Application Insights.</param>
        public void Initialize(TelemetryConfiguration configuration)
        {
            // Core SDK creates 1 instance of a module but calls Initialize multiple times
            if (!this.isInitialized)
            {
                lock (this.lockObject)
                {
                    if (!this.isInitialized)
                    {
                        this.isInitialized = true;

                        this.telemetryClient = new TelemetryClient(configuration);
                        this.telemetryClient.Context.GetInternalContext().SdkVersion = SdkVersionUtils.GetSdkVersion("stat:");

                        this.registerAction(this.CalculateStatistics);
                    }
                }
            }
        }

        /// <summary>
        /// Disposing TaskSchedulerOnUnobservedTaskException instance.
        /// </summary>
        public void Dispose()
        {
            this.unregisterAction(this.CalculateStatistics);
        }

        private void CalculateStatistics(object sender, FirstChanceExceptionEventArgs firstChanceExceptionArgs)
        {
            if (executionSyncObject == LOCKED)
            {
                return;
            }

            try
            {
                executionSyncObject = LOCKED;

                var exception = firstChanceExceptionArgs?.Exception;

                if (exception == null)
                {
                    WindowsServerEventSource.Log.FirstChanceExceptionCallbackExeptionIsNull();
                    return;
                }

                WindowsServerEventSource.Log.FirstChanceExceptionCallbackCalled();

                var type = exception.GetType().FullName;

                // obtaining the operation name. At this stage we have no intention to send this telemetry item
                ExceptionTelemetry fakeTelemetry = new ExceptionTelemetry(exception);
                this.telemetryClient.Initialize(fakeTelemetry);

                var operation = fakeTelemetry.Context.Operation.Name;

                // obtaining failing method name by walking 1 frame up the stack
                var failingMethod = new StackFrame(1).GetMethod();
                var method = failingMethod.DeclaringType.FullName + "." + failingMethod.Name;

                this.TrackStatistcis(type, operation, method);
            }
            catch (Exception exc)
            {
                try
                {
                    WindowsServerEventSource.Log.FirstChanceExceptionCallbackException(exc.ToInvariantString());
                }
                catch (Exception)
                {
                    // this is absolutely critical to not throw out of this method
                    // Otherwise it will affect the customer application behavior significantly
                }
            }
            finally
            {
                executionSyncObject = LOCKED;
            }
        }

        private void TrackStatistcis(string type, string operation, string method)
        {
            var metricTelemetry = new MetricTelemetry();

            metricTelemetry.Value = 1;

            metricTelemetry.Properties["type"] = type;
            metricTelemetry.Properties["operation"] = operation;
            metricTelemetry.Properties["method"] = method;

            this.telemetryClient.TrackMetric(metricTelemetry);
        }
    }
}
