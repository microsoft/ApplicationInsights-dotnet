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
        private const string TelemetryDocumentContractVersion = "1.0";
        
        private const int MaxTelemetryQuota = 30;

        private const int InitialTelemetryQuota = 3;

        private const int MaxFieldLength = 32768;

        private IQuickPulseDataAccumulatorManager dataAccumulatorManager = null;

        private Uri serviceEndpoint = QuickPulseDefaults.ServiceEndpoint;

        private TelemetryConfiguration config = null;

        private bool isCollecting = false;

        private bool disableFullTelemetryItems = false;

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
            TelemetryConfiguration configuration,
            bool disableFullTelemetryItems)
        {
            if (this.isCollecting)
            {
                throw new InvalidOperationException("Can't start collection while it is already running.");
            }

            this.dataAccumulatorManager = accumulatorManager;
            this.serviceEndpoint = serviceEndpoint;
            this.config = configuration;
            this.isCollecting = true;
            this.disableFullTelemetryItems = disableFullTelemetryItems;
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

        private static ITelemetryDocument ConvertRequestToTelemetryDocument(RequestTelemetry requestTelemetry)
        {
            var name = requestTelemetry.Name;
            if (name != null && name.Length > MaxFieldLength)
            {
                name = name.Substring(0, MaxFieldLength);
            }

            return new RequestTelemetryDocument()
                       {
                           Version = TelemetryDocumentContractVersion,
                           Timestamp = requestTelemetry.Timestamp,
                           Id = requestTelemetry.Id,
                           Name = name,
                           StartTime = requestTelemetry.StartTime,
                           Success = IsRequestSuccessful(requestTelemetry),
                           Duration = requestTelemetry.Duration,
                           ResponseCode = requestTelemetry.ResponseCode,
                           Url = requestTelemetry.Url,
                           HttpMethod = requestTelemetry.HttpMethod
                       };
        }

        private static ITelemetryDocument ConvertDependencyToTelemetryDocument(DependencyTelemetry dependencyTelemetry)
        {
            var name = dependencyTelemetry.Name;
            if (name != null && name.Length > MaxFieldLength)
            {
               name = name.Substring(0, MaxFieldLength);
            }

            var commandName = dependencyTelemetry.CommandName;
            if (commandName != null && commandName.Length > MaxFieldLength)
            {
                commandName = commandName.Substring(0, MaxFieldLength);
            }

            return new DependencyTelemetryDocument()
                       {
                           Version = TelemetryDocumentContractVersion,
                           Timestamp = dependencyTelemetry.Timestamp,
                           Id = dependencyTelemetry.Id,
                           Name = name,
                           StartTime = dependencyTelemetry.StartTime,
                           Success = dependencyTelemetry.Success,
                           Duration = dependencyTelemetry.Duration,
                           ResultCode = dependencyTelemetry.ResultCode,
                           CommandName = commandName,
                           DependencyTypeName = dependencyTelemetry.DependencyTypeName,
                           DependencyKind = dependencyTelemetry.DependencyKind
                       };
        }

        private static ITelemetryDocument ConvertExceptionToTelemetryDocument(ExceptionTelemetry exceptionTelemetry)
        {
            string exception = exceptionTelemetry.Exception != null ? exceptionTelemetry.Exception.ToString() : null;
            if (exception != null && exception.Length > MaxFieldLength)
            {
                exception = exception.Substring(0, MaxFieldLength);
            }

            string message = exceptionTelemetry.Message;
            if (message != null && message.Length > MaxFieldLength)
            {
                message = message.Substring(0, MaxFieldLength);
            }

            return new ExceptionTelemetryDocument()
                       {
                           Version = TelemetryDocumentContractVersion,
                           Message = message,
                           SeverityLevel =
                               exceptionTelemetry.SeverityLevel != null
                                   ? exceptionTelemetry.SeverityLevel.Value.ToString()
                                   : null,
                           HandledAt = exceptionTelemetry.HandledAt.ToString(),
                           Exception = exception
                       };
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

                // update aggregates
                if (telemetryAsRequest != null)
                {
                    this.UpdateRequestAggregates(telemetryAsRequest);
                }
                else if (telemetryAsDependency != null)
                {
                    this.UpdateDependencyAggregates(telemetryAsDependency);
                }
                else if (telemetryAsException != null)
                {
                    this.UpdateExceptionAggregates();
                }

                // collect full telemetry items
                if (!this.disableFullTelemetryItems)
                {
                    if (telemetryAsRequest != null && !IsRequestSuccessful(telemetryAsRequest))
                    {
                        this.CollectRequest(telemetryAsRequest);
                    }
                    else if (telemetryAsDependency != null && telemetryAsDependency.Success == false)
                    {
                        this.CollectDependency(telemetryAsDependency);
                    }
                    else if (telemetryAsException != null)
                    {
                        this.CollectException(telemetryAsException);
                    }
                }
            }
        }
        
        private void CollectRequest(RequestTelemetry requestTelemetry)
        {
            if (this.requestQuotaTracker.ApplyQuota())
            {
                ITelemetryDocument telemetryDocument = ConvertRequestToTelemetryDocument(requestTelemetry);

                this.dataAccumulatorManager.CurrentDataAccumulator.TelemetryDocuments.Push(telemetryDocument);
            }
        }
        
        private void CollectDependency(DependencyTelemetry dependencyTelemetry)
        {
            if (this.dependencyQuotaTracker.ApplyQuota())
            {
                ITelemetryDocument telemetryDocument = ConvertDependencyToTelemetryDocument(dependencyTelemetry);

                this.dataAccumulatorManager.CurrentDataAccumulator.TelemetryDocuments.Push(telemetryDocument);
            }
        }

        private void CollectException(ExceptionTelemetry exceptionTelemetry)
        {
            if (this.exceptionQuotaTracker.ApplyQuota())
            {
                ITelemetryDocument telemetryDocument = ConvertExceptionToTelemetryDocument(exceptionTelemetry);

                this.dataAccumulatorManager.CurrentDataAccumulator.TelemetryDocuments.Push(telemetryDocument);
            }
        }

        private void UpdateExceptionAggregates()
        {
            Interlocked.Increment(ref this.dataAccumulatorManager.CurrentDataAccumulator.AIExceptionCount);
        }

        private void UpdateDependencyAggregates(DependencyTelemetry dependencyTelemetry)
        {
            long dependencyCallCountAndDurationInTicks = QuickPulseDataAccumulator.EncodeCountAndDuration(1, dependencyTelemetry.Duration.Ticks);

            Interlocked.Add(
                ref this.dataAccumulatorManager.CurrentDataAccumulator.AIDependencyCallCountAndDurationInTicks,
                dependencyCallCountAndDurationInTicks);

            if (dependencyTelemetry.Success == true)
            {
                Interlocked.Increment(ref this.dataAccumulatorManager.CurrentDataAccumulator.AIDependencyCallSuccessCount);
            }
            else if (dependencyTelemetry.Success == false)
            {
                Interlocked.Increment(ref this.dataAccumulatorManager.CurrentDataAccumulator.AIDependencyCallFailureCount);
            }
        }

        private void UpdateRequestAggregates(RequestTelemetry requestTelemetry)
        {
            bool success = IsRequestSuccessful(requestTelemetry);

            long requestCountAndDurationInTicks = QuickPulseDataAccumulator.EncodeCountAndDuration(1, requestTelemetry.Duration.Ticks);

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