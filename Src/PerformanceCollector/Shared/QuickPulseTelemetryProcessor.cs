namespace Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.QuickPulse
{
    using System;
    using System.Globalization;
    using System.Linq;
    using System.Threading;

    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing;
    using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.Implementation.QuickPulse;

    /// <summary>
    /// Extracts QuickPulse data from the telemetry stream.
    /// </summary>
    public class QuickPulseTelemetryProcessor : ITelemetryProcessor, ITelemetryModule, IQuickPulseTelemetryProcessor
    {
        private IQuickPulseDataAccumulatorManager dataAccumulatorManager = null;

        private Uri serviceEndpoint = QuickPulseDefaults.ServiceEndpoint;

        private TelemetryConfiguration config = null;

        private bool isCollecting = false;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="QuickPulseTelemetryProcessor"/> class.
        /// </summary>
        /// <param name="next">The next TelemetryProcessor in the chain.</param>
        /// <exception cref="ArgumentNullException">Thrown if next is null.</exception>
        public QuickPulseTelemetryProcessor(ITelemetryProcessor next)
        {
            if (next == null)
            {
                throw new ArgumentNullException(nameof(next));
            }

            this.Register();

            this.Next = next;
        }

        private ITelemetryProcessor Next { get; }

        /// <summary>
        /// Initialize method is called after all configuration properties have been loaded from the configuration.
        /// </summary>
        public void Initialize(TelemetryConfiguration configuration)
        {
            /*
            The configuration that is being passed into this method is the configuration that is the reason
            why this instance of telemetry processor was created. Regardless of which instrumentation key is passed in,
            this telemetry processor will only collect for whichever instrumentation key is specified by the module in StartCollection call.
            */

            this.Register();
        }
        
        void IQuickPulseTelemetryProcessor.StartCollection(IQuickPulseDataAccumulatorManager accumulatorManager, Uri serviceEndpoint, TelemetryConfiguration configuration)
        {
            if (this.isCollecting)
            {
                throw new InvalidOperationException("Can't start collection while it is already running.");
            }
            
            this.dataAccumulatorManager = accumulatorManager;
            this.serviceEndpoint = serviceEndpoint;
            this.config = configuration;
            this.isCollecting = true;
        }

        void IQuickPulseTelemetryProcessor.StopCollection()
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
                if (this.config != null && !string.IsNullOrWhiteSpace(this.config.InstrumentationKey) && telemetry.Context != null
                    && string.Equals(telemetry.Context.InstrumentationKey, this.config.InstrumentationKey, StringComparison.OrdinalIgnoreCase))
                {
                    var request = telemetry as RequestTelemetry;
                    var exception = telemetry as ExceptionTelemetry;

                    if (request != null)
                    {
                        bool success = IsRequestSuccessful(request);

                        long requestCountAndDurationInTicks = QuickPulseDataAccumulator.EncodeCountAndDuration(1, request.Duration.Ticks);

                        Interlocked.Add(ref this.dataAccumulatorManager.CurrentDataAccumulator.AIRequestCountAndDurationInTicks, requestCountAndDurationInTicks);

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
                        long dependencyCallCountAndDurationInTicks = QuickPulseDataAccumulator.EncodeCountAndDuration(1, dependencyCall.Duration.Ticks);

                        Interlocked.Add(ref this.dataAccumulatorManager.CurrentDataAccumulator.AIDependencyCallCountAndDurationInTicks, dependencyCallCountAndDurationInTicks);

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
            catch (Exception e)
            {
                // whatever happened up there - we don't want to interrupt the chain of processors
                QuickPulseEventSource.Log.UnknownErrorEvent(e.ToInvariantString());
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

        private void Register()
        {
            var module = TelemetryModules.Instance.Modules.OfType<QuickPulseTelemetryModule>().SingleOrDefault();
            if (module != null)
            {
                module.RegisterTelemetryProcessor(this);
            }
        }
    }
}