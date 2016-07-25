namespace Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.QuickPulse
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Threading;

    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing;
    using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.Implementation.QuickPulse;
    using Microsoft.ManagementServices.RealTimeDataProcessing.QuickPulseService;

    /// <summary>
    /// Extracts QuickPulse data from the telemetry stream.
    /// </summary>
    public class QuickPulseTelemetryProcessor : ITelemetryProcessor, ITelemetryModule, IQuickPulseTelemetryProcessor
    {
        private const int MaxTelemetryQuota = 30;

        private const int InitialTelemetryQuota = 3;

        private IQuickPulseDataAccumulatorManager dataAccumulatorManager = null;

        private Uri serviceEndpoint = QuickPulseDefaults.ServiceEndpoint;

        private TelemetryConfiguration config = null;

        private bool isCollecting = false;

        private QuickPulseQuotaTracker requestQuotaTracker = null;

        private QuickPulseQuotaTracker dependencyQuotaTracker = null;

        private QuickPulseQuotaTracker exceptionQuotaTracker = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="QuickPulseTelemetryProcessor"/> class.
        /// </summary>
        /// <param name="next">The next TelemetryProcessor in the chain.</param>
        /// <exception cref="ArgumentNullException">Thrown if next is null.</exception>
        public QuickPulseTelemetryProcessor(ITelemetryProcessor next)
            : this(next, new Clock())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="QuickPulseTelemetryProcessor"/> class. Internal constructor for unit tests only.
        /// </summary>
        /// <param name="next">The next TelemetryProcessor in the chain.</param>
        /// <param name="timeProvider">Time provider.</param>
        /// <param name="maxTelemetryQuota">Max telemetry quota.</param>
        /// <param name="initialTelemetryQuota">Initial telemetry quota.</param>
        /// <exception cref="ArgumentNullException">Thrown if next is null.</exception>
        internal QuickPulseTelemetryProcessor(
            ITelemetryProcessor next,
            Clock timeProvider,
            int? maxTelemetryQuota = null,
            int? initialTelemetryQuota = null)
        {
            if (next == null)
            {
                throw new ArgumentNullException(nameof(next));
            }

            this.Register();

            this.Next = next;

            this.requestQuotaTracker = new QuickPulseQuotaTracker(
                timeProvider,
                maxTelemetryQuota ?? MaxTelemetryQuota,
                initialTelemetryQuota ?? InitialTelemetryQuota);

            this.dependencyQuotaTracker = new QuickPulseQuotaTracker(
                timeProvider,
                maxTelemetryQuota ?? MaxTelemetryQuota,
                initialTelemetryQuota ?? InitialTelemetryQuota);

            this.exceptionQuotaTracker = new QuickPulseQuotaTracker(
                timeProvider,
                maxTelemetryQuota ?? MaxTelemetryQuota,
                initialTelemetryQuota ?? InitialTelemetryQuota);
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

        void IQuickPulseTelemetryProcessor.StartCollection(
            IQuickPulseDataAccumulatorManager accumulatorManager,
            Uri serviceEndpoint,
            TelemetryConfiguration configuration)
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
            bool letTelemetryThrough = true;

            try
            {
                // filter out QPS requests from dependencies even when we're not collecting (for Pings)
                var dependency = telemetry as DependencyTelemetry;
                if (this.serviceEndpoint != null && dependency != null && !string.IsNullOrWhiteSpace(dependency.Name))
                {
                    if (dependency.Name.IndexOf(this.serviceEndpoint.Host, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        // this is an HTTP request to QuickPulse service, we don't want to let it through
                        letTelemetryThrough = false;

                        return;
                    }
                }

                if (!this.isCollecting || this.dataAccumulatorManager == null)
                {
                    return;
                }

                this.ProcessTelemetry(telemetry);
            }
            catch (Exception e)
            {
                // whatever happened up there - we don't want to interrupt the chain of processors
                QuickPulseEventSource.Log.UnknownErrorEvent(e.ToInvariantString());
            }
            finally
            {
                if (letTelemetryThrough)
                {
                    this.Next.Process(telemetry);
                }
            }
        }

        private static ITelemetryDocument ConvertTelemetryToTelemetryDocument(
            RequestTelemetry telemetryAsRequest,
            DependencyTelemetry telemetryAsDependency,
            ExceptionTelemetry telemetryAsException)
        {
            const string TelemetryDocumentContractVersion = "1.0";

            if (telemetryAsRequest != null)
            {
                return new RequestTelemetryDocument()
                           {
                               Version = TelemetryDocumentContractVersion,
                               Timestamp = telemetryAsRequest.Timestamp,
                               Id = telemetryAsRequest.Id,
                               Name = telemetryAsRequest.Name,
                               StartTime = telemetryAsRequest.StartTime,
                               Success = IsRequestSuccessful(telemetryAsRequest),
                               Duration = telemetryAsRequest.Duration,
                               Sequence = telemetryAsRequest.Sequence,
                               ResponseCode = telemetryAsRequest.ResponseCode,
                               Url = telemetryAsRequest.Url,
                               HttpMethod = telemetryAsRequest.HttpMethod
                           };
            }
            else if (telemetryAsDependency != null)
            {
                return new DependencyTelemetryDocument()
                           {
                               Version = TelemetryDocumentContractVersion,
                               Timestamp = telemetryAsDependency.Timestamp,
                               Id = telemetryAsDependency.Id,
                               Name = telemetryAsDependency.Name,
                               StartTime = telemetryAsDependency.StartTime,
                               Success = telemetryAsDependency.Success,
                               Duration = telemetryAsDependency.Duration,
                               Sequence = telemetryAsDependency.Sequence,
                               ResultCode = telemetryAsDependency.ResultCode,
                               CommandName = telemetryAsDependency.CommandName,
                               DependencyTypeName = telemetryAsDependency.DependencyTypeName,
                               DependencyKind = telemetryAsDependency.DependencyKind
                           };
            }
            else if (telemetryAsException != null)
            {
                // //!!! cut length for Exception.ToString()
                return new ExceptionTelemetryDocument()
                           {
                               Version = TelemetryDocumentContractVersion,
                               Message = telemetryAsException.Message,
                               SeverityLevel =
                                   telemetryAsException.SeverityLevel != null
                                       ? telemetryAsException.SeverityLevel.Value.ToString()
                                       : null,
                               HandledAt = telemetryAsException.HandledAt.ToString(),
                               Exception =
                                   telemetryAsException.Exception != null
                                       ? telemetryAsException.Exception.ToString()
                                       : null
                           };
            }

            // this should never happen
            return null;
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

        private void ProcessTelemetry(ITelemetry telemetry)
        {
            // only process items that are going to the instrumentation key that our module is initialized with
            if (this.config != null && !string.IsNullOrWhiteSpace(this.config.InstrumentationKey) && telemetry.Context != null
                && string.Equals(telemetry.Context.InstrumentationKey, this.config.InstrumentationKey, StringComparison.OrdinalIgnoreCase))
            {
                var telemetryAsRequest = telemetry as RequestTelemetry;
                var telemetryAsDependency = telemetry as DependencyTelemetry;
                var telemetryAsException = telemetry as ExceptionTelemetry;

                this.UpdateAggregates(telemetryAsRequest, telemetryAsDependency, telemetryAsException);

                this.CollectTelemetryDocuments(telemetryAsRequest, telemetryAsDependency, telemetryAsException);
            }
        }

        private void CollectTelemetryDocuments(
            RequestTelemetry telemetryAsRequest,
            DependencyTelemetry telemetryAsDependency,
            ExceptionTelemetry telemetryAsException)
        {
            if (telemetryAsRequest != null && !IsRequestSuccessful(telemetryAsRequest))
            {
                if (this.requestQuotaTracker.ApplyQuota())
                {
                    var telemetryDocument = ConvertTelemetryToTelemetryDocument(telemetryAsRequest, telemetryAsDependency, telemetryAsException);
                    if (telemetryDocument != null)
                    {
                        this.dataAccumulatorManager.CurrentDataAccumulator.TelemetryDocuments.Push(telemetryDocument);
                    }
                }
            }
            else if (telemetryAsDependency != null && telemetryAsDependency.Success == false)
            {
                if (this.dependencyQuotaTracker.ApplyQuota())
                {
                    var telemetryDocument = ConvertTelemetryToTelemetryDocument(telemetryAsRequest, telemetryAsDependency, telemetryAsException);
                    if (telemetryDocument != null)
                    {
                        this.dataAccumulatorManager.CurrentDataAccumulator.TelemetryDocuments.Push(telemetryDocument);
                    }
                }
            }
            else if (telemetryAsException != null)
            {
                if (this.exceptionQuotaTracker.ApplyQuota())
                {
                    var telemetryDocument = ConvertTelemetryToTelemetryDocument(telemetryAsRequest, telemetryAsDependency, telemetryAsException);
                    if (telemetryDocument != null)
                    {
                        this.dataAccumulatorManager.CurrentDataAccumulator.TelemetryDocuments.Push(telemetryDocument);
                    }
                }
            }
        }

        private void UpdateAggregates(
            RequestTelemetry telemetryAsRequest,
            DependencyTelemetry telemetryAsDependency,
            ExceptionTelemetry telemetryAsException)
        {
            if (telemetryAsRequest != null)
            {
                bool success = IsRequestSuccessful(telemetryAsRequest);

                long requestCountAndDurationInTicks = QuickPulseDataAccumulator.EncodeCountAndDuration(1, telemetryAsRequest.Duration.Ticks);

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
            else if (telemetryAsDependency != null)
            {
                long dependencyCallCountAndDurationInTicks = QuickPulseDataAccumulator.EncodeCountAndDuration(1, telemetryAsDependency.Duration.Ticks);

                Interlocked.Add(
                    ref this.dataAccumulatorManager.CurrentDataAccumulator.AIDependencyCallCountAndDurationInTicks,
                    dependencyCallCountAndDurationInTicks);

                if (telemetryAsDependency.Success == true)
                {
                    Interlocked.Increment(ref this.dataAccumulatorManager.CurrentDataAccumulator.AIDependencyCallSuccessCount);
                }
                else if (telemetryAsDependency.Success == false)
                {
                    Interlocked.Increment(ref this.dataAccumulatorManager.CurrentDataAccumulator.AIDependencyCallFailureCount);
                }
            }
            else if (telemetryAsException != null)
            {
                Interlocked.Increment(ref this.dataAccumulatorManager.CurrentDataAccumulator.AIExceptionCount);
            }
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