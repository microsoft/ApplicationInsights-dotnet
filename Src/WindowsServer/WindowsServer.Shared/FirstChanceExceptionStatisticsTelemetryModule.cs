namespace Microsoft.ApplicationInsights.WindowsServer
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
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
        private const int UNLOCKED = 0;

        /// <summary>
        /// This object prevents double entry into the exception callback.
        /// </summary>
        [ThreadStatic]
        private static int executionSyncObject;

        private readonly Action<EventHandler<FirstChanceExceptionEventArgs>> registerAction;
        private readonly Action<EventHandler<FirstChanceExceptionEventArgs>> unregisterAction;
        private readonly object lockObject = new object();

        private ConditionalWeakTable<Exception, object> exceptionIsTracked = new ConditionalWeakTable<Exception, object>();

        private TelemetryClient telemetryClient;
        private MetricManager metricManager;

        private bool isInitialized = false;

        // cheap dimentions capping
        private ConcurrentDictionary<string, int> operationValues = new ConcurrentDictionary<string, int>();
        private ConcurrentDictionary<string, int> methodValues = new ConcurrentDictionary<string, int>();
        private ConcurrentDictionary<string, int> typeValues = new ConcurrentDictionary<string, int>();

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
                        this.telemetryClient.Context.GetInternalContext().SdkVersion = SdkVersionUtils.GetSdkVersion("exstat:");

                        this.metricManager = new MetricManager(this.telemetryClient);

                        this.registerAction(this.CalculateStatistics);
                    }
                }
            }
        }

        /// <summary>
        /// Disposing TaskSchedulerOnUnobservedTaskException instance. This class doesn't have the finalize method as we expect it 
        /// live for a duration of the process and be disposed by AI infrastructure.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        internal bool WasExceptionTracked(Exception exception)
        {
            object tmp;
            bool wasTracked = this.exceptionIsTracked.TryGetValue(exception, out tmp);

            if (!wasTracked)
            {
                var innerException = exception.InnerException;
                if (innerException != null)
                {
                    wasTracked = this.exceptionIsTracked.TryGetValue(innerException, out tmp);
                }
            }

            if (!wasTracked && exception is AggregateException)
            {
                foreach (var innerException in ((AggregateException)exception).InnerExceptions)
                {
                    if (innerException != null)
                    {
                        wasTracked = this.exceptionIsTracked.TryGetValue(innerException, out tmp);
                    }
                }
            }

            // mark exception as tracked
            this.exceptionIsTracked.GetOrCreateValue(exception);
            return wasTracked;
        }

        /// <summary>
        /// IDisposable implementation.
        /// </summary>
        /// <param name="disposing">The method has been called directly or indirectly by a user's code.</param>
        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.unregisterAction(this.CalculateStatistics);
                this.metricManager.Dispose();
            }
        }

        private void CalculateStatistics(object sender, FirstChanceExceptionEventArgs firstChanceExceptionArgs)
        {
            // this is thread local variable. No need to lock
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
                var method = (failingMethod.DeclaringType?.FullName ?? "Global") + "." + failingMethod.Name;

                bool wasTracked = this.WasExceptionTracked(exception);

                this.TrackStatistics(type, operation, method, wasTracked);
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
                executionSyncObject = UNLOCKED;
            }
        }

        private void TrackStatistics(string type, string operation, string method, bool wasTracked)
        {
            var dimensions = new Dictionary<string, string>();
            dimensions.Add("type", this.GetDimCappedString(type, this.typeValues));
            dimensions.Add("method", this.GetDimCappedString(method, this.methodValues));

            if (!string.IsNullOrEmpty(operation))
            {
                dimensions.Add("operation", this.GetDimCappedString(operation, this.operationValues));
            }

            var metric = this.metricManager.CreateMetric("Exceptions Thrown", dimensions);

            if (wasTracked)
            {
                metric.Track(0);
            }
            else
            {
                metric.Track(1);
            }
        }

        private string GetDimCappedString(string dimensionValue, ConcurrentDictionary<string, int> capValues)
        {
            int temp;
            if (capValues.TryGetValue(dimensionValue, out temp))
            {
                return dimensionValue;
            }
            else if (capValues.Count > 100)
            {
                return "OtherValue";
            }
            else
            {
                capValues.TryAdd(dimensionValue, 0);
                return dimensionValue;
            }
        }
    }
}
