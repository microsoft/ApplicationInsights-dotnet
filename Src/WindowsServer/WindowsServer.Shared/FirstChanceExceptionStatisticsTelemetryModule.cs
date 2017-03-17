namespace Microsoft.ApplicationInsights.WindowsServer
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Runtime.ExceptionServices;
    using System.Threading;
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
        internal const int CacheSize = 100;

        internal const double TargetMovingAverage = 5000;
        internal const double CurrentWeight = .7;
        internal const double NewWeight = .3;
        internal const long TicksMovingAverage = 100000000; // 10 seconds
        internal long MovingAverageTimeout;

        // cheap dimension capping
        internal long DimCapTimeout;

        private const int LOCKED = 1;
        private const int UNLOCKED = 0;

        /// <summary>
        /// A key into an <see cref="Exception"/> object's <see cref="Exception.Data"/> dictionary
        /// used to indicate that the exception is being tracked.
        /// </summary>
        private static readonly object ExceptionIsTracked = new object();

        /// <summary>
        /// This object prevents double entry into the exception callback.
        /// </summary>
        [ThreadStatic]
        private static int executionSyncObject;

        private readonly Action<EventHandler<FirstChanceExceptionEventArgs>> registerAction;
        private readonly Action<EventHandler<FirstChanceExceptionEventArgs>> unregisterAction;
        private readonly object lockObject = new object();
        private readonly object movingAverageLockObject = new object();

        private TelemetryClient telemetryClient;
        private MetricManager metricManager;

        private bool isInitialized = false;

        private long newThreshold = 0;
        private long newProcessed = 0;
        private double currentMovingAverage = 0;

        private long cacheLifetime = 1200000000; // Two minutes in ticks

        private HashCache<string> operationValues = new HashCache<string>();
        private HashCache<string> methodValues = new HashCache<string>();
        private HashCache<string> typeValues = new HashCache<string>();

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
            this.DimCapTimeout = DateTime.UtcNow.Ticks + this.cacheLifetime;
            this.MovingAverageTimeout = DateTime.UtcNow.Ticks - 1; // Setting the timeout to be expired

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
                        this.telemetryClient = new TelemetryClient(configuration);
                        this.telemetryClient.Context.GetInternalContext().SdkVersion = SdkVersionUtils.GetSdkVersion("exstat:");

                        this.metricManager = new MetricManager(this.telemetryClient);

                        this.registerAction(this.CalculateStatistics);

                        this.isInitialized = true;
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
            bool wasTracked = IsTracked(exception);

            if (!wasTracked)
            {
                var innerException = exception.InnerException;
                if (innerException != null)
                {
                    wasTracked = IsTracked(innerException);
                }
            }

            if (!wasTracked && exception is AggregateException)
            {
                foreach (var innerException in ((AggregateException)exception).InnerExceptions)
                {
                    if (innerException != null)
                    {
                        wasTracked = IsTracked(innerException);

                        if (wasTracked == true)
                        {
                            break;
                        }
                    }
                }
            }

            // some exceptions like MemoryOverflow, ThreadAbort or ExecutionEngine are pre-instantiated 
            // so the .Data is now writable. Also it may be null in certain cases
            if (exception.Data != null && !exception.Data.IsReadOnly)
            {
                // mark exception as tracked
                exception.Data[ExceptionIsTracked] = null; // The value is unimportant. It's just a sentinel.
            }

            return wasTracked;
        }

        private static bool IsTracked(Exception exception)
        {
            if (exception.Data != null)
            {
                return exception.Data.Contains(ExceptionIsTracked);
            }

            return false;
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
                this.typeValues.Dispose();
                this.methodValues.Dispose();
                this.operationValues.Dispose();
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

                if (this.MovingAverageTimeout < DateTime.UtcNow.Ticks)
                {
                    lock (this.movingAverageLockObject)
                    {
                        if (this.MovingAverageTimeout < DateTime.UtcNow.Ticks)
                        {
                            if (this.MovingAverageTimeout + TicksMovingAverage < DateTime.UtcNow.Ticks)
                            {
                                this.currentMovingAverage = 0;
                            }
                            else
                            {
                                this.currentMovingAverage = (this.currentMovingAverage * CurrentWeight) +
                                (((double)this.newProcessed) * NewWeight);
                            }

                            this.newThreshold = (long)((TargetMovingAverage - (this.currentMovingAverage * CurrentWeight)) / NewWeight);

                            this.newProcessed = 0;

                            this.MovingAverageTimeout = DateTime.UtcNow.Ticks + TicksMovingAverage;
                        }
                    }
                }

                var exception = firstChanceExceptionArgs?.Exception;

                if (exception == null)
                {
                    WindowsServerEventSource.Log.FirstChanceExceptionCallbackExeptionIsNull();
                    return;
                }

                WindowsServerEventSource.Log.FirstChanceExceptionCallbackCalled();

                var type = exception.GetType().FullName;

                if (this.newProcessed < this.newThreshold)
                {
                    Interlocked.Increment(ref this.newProcessed);

                    // obtaining the operation name. At this stage we have no intention to send this telemetry item
                    ExceptionTelemetry fakeTelemetry = new ExceptionTelemetry(exception);
                    this.telemetryClient.Initialize(fakeTelemetry);

                    var operation = fakeTelemetry.Context.Operation.Name;

                    // obtaining failing method name by walking 1 frame up the stack
                    var frame = new StackFrame(1);
                    var failingMethod = frame.GetMethod();
                    var method = (failingMethod.DeclaringType?.FullName ?? "Global") + "." + failingMethod.Name;

                    var offset = frame.GetILOffset();
                    if (offset != StackFrame.OFFSET_UNKNOWN)
                    {
                        method += ": " + offset.ToString(CultureInfo.InvariantCulture);
                    }

                    bool wasTracked = this.WasExceptionTracked(exception);

                    this.TrackStatistics(type, operation, method, wasTracked);
                }
                else
                {
                    this.TrackStatistics(type, null, null, false);
                }
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

            if (this.DimCapTimeout < DateTime.UtcNow.Ticks)
            {
                this.ResetDimCapCaches(this.typeValues, this.methodValues, this.operationValues);
            }

            dimensions.Add("type", this.GetDimCappedString(type, this.typeValues));

            if (string.IsNullOrEmpty(method) == false)
            {
                dimensions.Add("method", this.GetDimCappedString(method, this.methodValues));
            }

            if (string.IsNullOrEmpty(operation) == false)
            {
                if (SdkInternalOperationsMonitor.IsEntered())
                {
                    dimensions.Add("operation", "AI (Internal)");
                }
                else if (!string.IsNullOrEmpty(operation))
                {
                    dimensions.Add("operation", this.GetDimCappedString(operation, this.operationValues));
                }
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

        private string GetDimCappedString(string dimensionValue, HashCache<string> hashCache)
        {
            hashCache.RwLock.EnterReadLock();

            if (hashCache.ValueCache.Contains(dimensionValue) == true)
            {
                hashCache.RwLock.ExitReadLock();

                return dimensionValue;
            }

            if (hashCache.ValueCache.Count > CacheSize)
            {
                hashCache.RwLock.ExitReadLock();

                return "OtherValue";
            }

            hashCache.RwLock.ExitReadLock();
            hashCache.RwLock.EnterWriteLock();

            hashCache.ValueCache.Add(dimensionValue);

            hashCache.RwLock.ExitWriteLock();

            return dimensionValue;
        }

        private void ResetDimCapCaches(HashCache<string> cache1, HashCache<string> cache2, HashCache<string> cache3)
        {
            cache1.RwLock.EnterWriteLock();

            if (this.DimCapTimeout < DateTime.UtcNow.Ticks)
            {
                cache2.RwLock.EnterWriteLock();

                if (this.DimCapTimeout < DateTime.UtcNow.Ticks)
                {
                    cache3.RwLock.EnterWriteLock();

                    if (this.DimCapTimeout < DateTime.UtcNow.Ticks)
                    {
                        cache1.ValueCache.Clear();
                        cache2.ValueCache.Clear();
                        cache3.ValueCache.Clear();

                        this.DimCapTimeout = DateTime.UtcNow.Ticks + this.cacheLifetime;
                    }

                    cache3.RwLock.ExitWriteLock();
                }

                cache2.RwLock.ExitWriteLock();
            }

            cache1.RwLock.ExitWriteLock();
        }

        internal class HashCache<T> : IDisposable
        {
            internal ReaderWriterLockSlim RwLock;
            internal HashSet<T> ValueCache = new HashSet<T>();

            private bool disposedValue = false; // To detect redundant calls

            internal HashCache()
            {
                this.RwLock = new ReaderWriterLockSlim();
                this.ValueCache = new HashSet<T>();
            }

            void IDisposable.Dispose()
            {
                this.Dispose(true);
            }

            // This code added to correctly implement the disposable pattern.
            internal void Dispose()
            {
                // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
                this.Dispose(true);
            }

            protected virtual void Dispose(bool disposing)
            {
                if (!this.disposedValue)
                {
                    if (disposing)
                    {
                        ////this.RwLock.Dispose();
                    }

                    this.disposedValue = true;
                }
            }
        }
    }
}
