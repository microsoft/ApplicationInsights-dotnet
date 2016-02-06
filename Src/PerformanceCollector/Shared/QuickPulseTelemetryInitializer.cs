namespace Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.QuickPulse
{
    using System.Threading;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.Implementation.QuickPulse;

    /// <summary>
    /// Extracts QuickPulse data from the telemetry stream.
    /// </summary>
    /// <remarks>Unlike other telemetry initializers, this class does not modify telemetry items.</remarks>
    internal class QuickPulseTelemetryInitializer : IQuickPulseTelemetryInitializer
    {
        private IQuickPulseDataHub dataHub = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="QuickPulseTelemetryInitializer"/> class.
        /// </summary>
        public QuickPulseTelemetryInitializer()
        {
            this.dataHub = QuickPulseDataHub.Instance;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="QuickPulseTelemetryInitializer"/> class. Internal constructor for unit tests only.
        /// </summary>
        /// <param name="dataHub">Data sink for QuickPulse data.</param>
        internal QuickPulseTelemetryInitializer(IQuickPulseDataHub dataHub) : this()
        {
            this.dataHub = dataHub;
        }

        /// <summary>
        /// Intercepts telemetry items and updates QuickPulse data when needed.
        /// </summary>
        /// <param name="telemetry">Telemetry item being tracked by AI.</param>
        /// <remarks>This method is performance critical since every AI telemetry item goes through it.</remarks>
        public void Initialize(ITelemetry telemetry)
        {
            // we don't care about the actual instrumentation key to which this item is going to go
            // (telemetry.Context.InstrumentationKey), for now all QuickPulse data is being sent to 
            // the iKey passed to the module through configuration at initialization time 
            // (most likely TelemetryConfiguration.Active.InstrumentationKey)
            if (telemetry is RequestTelemetry)
            {
                var request = (RequestTelemetry)telemetry;

                Interlocked.Increment(ref this.dataHub.CurrentDataSampleReference.AIRequestCount);
                Interlocked.Add(ref this.dataHub.CurrentDataSampleReference.AIRequestDurationTicks, request.Duration.Ticks);

                if (request.Success == true)
                {
                    Interlocked.Increment(ref this.dataHub.CurrentDataSampleReference.AIRequestSuccessCount);
                }
                else if (request.Success == false)
                {
                    Interlocked.Increment(ref this.dataHub.CurrentDataSampleReference.AIRequestFailureCount);
                }
            }
            else if (telemetry is DependencyTelemetry)
            {
                var dependencyCall = (DependencyTelemetry)telemetry;

                Interlocked.Increment(ref this.dataHub.CurrentDataSampleReference.AIDependencyCallCount);
                Interlocked.Add(ref this.dataHub.CurrentDataSampleReference.AIDependencyCallDurationTicks, dependencyCall.Duration.Ticks);

                if (dependencyCall.Success == true)
                {
                    Interlocked.Increment(ref this.dataHub.CurrentDataSampleReference.AIDependencyCallSuccessCount);
                }
                else if (dependencyCall.Success == false)
                {
                    Interlocked.Increment(ref this.dataHub.CurrentDataSampleReference.AIDependencyCallFailureCount);
                }
            }
        }
    }
}