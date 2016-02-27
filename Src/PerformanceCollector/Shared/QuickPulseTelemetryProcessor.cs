namespace Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.QuickPulse
{
    using System;
    using System.Globalization;
    using System.Threading;

    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.Implementation.QuickPulse;

    /// <summary>
    /// Extracts QuickPulse data from the telemetry stream.
    /// </summary>
    /// <remarks>Unlike other telemetry initializers, this class does not modify telemetry items.</remarks>
    internal class QuickPulseTelemetryProcessor : IQuickPulseTelemetryProcessor
    {
        private IQuickPulseDataAccumulatorManager dataAccumulatorManager = null;

        private Uri serviceEndpoint = null;

        private TelemetryConfiguration config = null;

        private bool isCollecting = false;

        private bool isInitialized = false;
        
        public QuickPulseTelemetryProcessor(ITelemetryProcessor next)
        {
            if (next == null)
            {
                throw new ArgumentNullException(nameof(next));
            }

            this.Next = next;
        }

        private ITelemetryProcessor Next { get; }

        public void Initialize(Uri serviceEndpoint, TelemetryConfiguration configuration)
        {
            if (this.isInitialized)
            {
                return;
            }

            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            this.serviceEndpoint = serviceEndpoint;
            this.config = configuration;

            this.isInitialized = true;
        }

        public void StartCollection(IQuickPulseDataAccumulatorManager accumulatorManager)
        {
            if (!this.isInitialized)
            {
                throw new InvalidOperationException("Can't start collection without initializing first.");    
            }

            if (this.isCollecting)
            {
                throw new InvalidOperationException("Can't start collection while it is already running.");
            }

            this.dataAccumulatorManager = accumulatorManager;
            
            this.isCollecting = true;
        }

        public void StopCollection()
        {
            this.dataAccumulatorManager = null;
            this.isCollecting = false;
        }

        /// <summary>
        /// Intercepts telemetry items and updates QuickPulse data when needed.
        /// </summary>
        /// <param name="telemetry">Telemetry item being tracked by AI.</param>
        /// <remarks>This method is performance critical since every AI telemetry item goes through it.</remarks>
        public void Process(ITelemetry telemetry)
        {
            bool letItemThrough = true;

            try
            {
                // filter out QPS requests from dependencies even when we're not collecting (for Pings)
                var dependencyCall = telemetry as DependencyTelemetry;
                if (this.serviceEndpoint != null && dependencyCall != null && !string.IsNullOrWhiteSpace(dependencyCall.Name))
                {
                    if (dependencyCall.Name.IndexOf(this.serviceEndpoint.Host, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        // this is an HTTP request to QuickPulse service, we don't want to let it through
                        letItemThrough = false;

                        return;
                    }
                }

                if (!this.isCollecting || this.dataAccumulatorManager == null)
                {
                    return;
                }

                // only process items that are going to the instrumentation key that our module is initialized with
                if (telemetry.Context != null && string.Equals(telemetry.Context.InstrumentationKey, this.config.InstrumentationKey))
                {
                    var request = telemetry as RequestTelemetry;
                    var exception = telemetry as ExceptionTelemetry;

                    if (request != null)
                    {
                        bool success = IsRequestSuccessful(request);

                        long requestCountAndDurationInTicks = QuickPulseDataAccumulator.EncodeCountAndDuration(1, request.Duration.Ticks);

                        Interlocked.Add(
                            ref this.dataAccumulatorManager.CurrentDataAccumulator.AIRequestCountAndDurationInTicks,
                            requestCountAndDurationInTicks);

                        if (success)
                        {
                            Interlocked.Increment(ref this.dataAccumulatorManager.CurrentDataAccumulator.AIRequestSuccessCount);
                        }
                        else
                        {
                            Interlocked.Increment(ref this.dataAccumulatorManager.CurrentDataAccumulator.AIRequestFailureCount);
                        }
                    }
                    else if (dependencyCall != null)
                    {
                        long dependencyCallCountAndDurationInTicks = QuickPulseDataAccumulator.EncodeCountAndDuration(
                            1,
                            dependencyCall.Duration.Ticks);

                        Interlocked.Add(
                            ref this.dataAccumulatorManager.CurrentDataAccumulator.AIDependencyCallCountAndDurationInTicks,
                            dependencyCallCountAndDurationInTicks);

                        if (dependencyCall.Success == true)
                        {
                            Interlocked.Increment(ref this.dataAccumulatorManager.CurrentDataAccumulator.AIDependencyCallSuccessCount);
                        }
                        else if (dependencyCall.Success == false)
                        {
                            Interlocked.Increment(ref this.dataAccumulatorManager.CurrentDataAccumulator.AIDependencyCallFailureCount);
                        }
                    }
                    else if (exception != null)
                    {
                        Interlocked.Increment(ref this.dataAccumulatorManager.CurrentDataAccumulator.AIExceptionCount);
                    }
                }
            }
            finally
            {
                if (letItemThrough)
                {
                    this.Next.Process(telemetry);
                }
            }
        }

        private static bool IsRequestSuccessful(RequestTelemetry request)
        {
            string responseCode = request.ResponseCode;
            bool? success = request.Success;

            if (string.IsNullOrWhiteSpace(responseCode))
            {
                responseCode = "200";
                success = true;
            }

            if (success == null)
            {
                int responseCodeInt;
                if (int.TryParse(responseCode, NumberStyles.Any, CultureInfo.InvariantCulture, out responseCodeInt))
                {
                    success = (responseCodeInt < 400) || (responseCodeInt == 401);
                }
                else
                {
                    success = true;
                }
            }

            return success.Value;
        }
    }
}