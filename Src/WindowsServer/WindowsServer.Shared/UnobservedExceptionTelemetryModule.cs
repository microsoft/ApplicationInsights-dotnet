namespace Microsoft.ApplicationInsights.WindowsServer
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.ApplicationInsights.Web.Implementation;
    using Microsoft.ApplicationInsights.WindowsServer.Implementation;

    /// <summary>
    /// The module subscribed to TaskScheduler.UnobservedTaskException to send exceptions to ApplicationInsights.
    /// </summary>
    public sealed class UnobservedExceptionTelemetryModule : ITelemetryModule, IDisposable
    {
        private readonly Action<EventHandler<UnobservedTaskExceptionEventArgs>> registerAction;
        private readonly Action<EventHandler<UnobservedTaskExceptionEventArgs>> unregisterAction;
        private readonly object lockObject = new object();

        private TelemetryClient telemetryClient;
        private bool isInitialized = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="UnobservedExceptionTelemetryModule" /> class.
        /// </summary>
        public UnobservedExceptionTelemetryModule() : this(
            action => TaskScheduler.UnobservedTaskException += action,
            action => TaskScheduler.UnobservedTaskException -= action)
        {
        }

        internal UnobservedExceptionTelemetryModule(
            Action<EventHandler<UnobservedTaskExceptionEventArgs>> registerAction,
            Action<EventHandler<UnobservedTaskExceptionEventArgs>> unregisterAction)
        {
            this.registerAction = registerAction;
            this.unregisterAction = unregisterAction;
        }

        /// <summary>
        /// Initializes the telemetry module.
        /// </summary>
        /// <param name="configuration">Telemetry Configuration used for creating TelemetryClient for sending exceptions to ApplicationInsights.</param>
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
                        this.telemetryClient.Context.GetInternalContext().SdkVersion = SdkVersionUtils.GetSdkVersion("unobs:");

                        this.registerAction(this.TaskSchedulerOnUnobservedTaskException);
                    }
                }
            }
        }

        /// <summary>
        /// Disposing TaskSchedulerOnUnobservedTaskException instance.
        /// </summary>
        public void Dispose()
        {
            this.unregisterAction(this.TaskSchedulerOnUnobservedTaskException);
        }

        private void TaskSchedulerOnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs unobservedTaskExceptionEventArgs)
        {
            WindowsServerEventSource.Log.TaskSchedulerOnUnobservedTaskException();

            var exp = new ExceptionTelemetry(unobservedTaskExceptionEventArgs.Exception)
            {
                SeverityLevel = SeverityLevel.Critical,
            };

            // TODO: what if TrackException will throw another UnobservedTaskException?
            // Either put a comment here why it will never ever happen or include a protection logic
            this.telemetryClient.TrackException(exp);
        }
    }
}
