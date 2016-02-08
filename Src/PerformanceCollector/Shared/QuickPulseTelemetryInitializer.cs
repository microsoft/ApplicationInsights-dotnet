namespace Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.QuickPulse
{
    using System;
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
        private IQuickPulseDataAccumulatorManager dataAccumulatorManager = null;

        private bool isCollecting = false;

        public void StartCollection(IQuickPulseDataAccumulatorManager accumulatorManager)
        {
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
        public void Initialize(ITelemetry telemetry)
        {
            if (!this.isCollecting || this.dataAccumulatorManager == null)
            {
                return;
            }

            // we don't care about the actual instrumentation key to which this item is going to go
            // (telemetry.Context.InstrumentationKey), for now all QuickPulse data is being sent to 
            // the iKey passed to the module through configuration at initialization time 
            // (most likely TelemetryConfiguration.Active.InstrumentationKey)
            var request = telemetry as RequestTelemetry;
            var dependencyCall = telemetry as DependencyTelemetry;

            if (request != null)
            {
                long requestCountAndDurationInTicks = QuickPulseDataAccumulator.EncodeCountAndDuration(1, request.Duration.Ticks);

                Interlocked.Add(
                    ref this.dataAccumulatorManager.CurrentDataAccumulatorReference.AIRequestCountAndDurationInTicks,
                    requestCountAndDurationInTicks);

                if (request.Success == true)
                {
                    Interlocked.Increment(ref this.dataAccumulatorManager.CurrentDataAccumulatorReference.AIRequestSuccessCount);
                }
                else if (request.Success == false)
                {
                    Interlocked.Increment(ref this.dataAccumulatorManager.CurrentDataAccumulatorReference.AIRequestFailureCount);
                }
            }
            else if (dependencyCall != null)
            {
                long dependencyCallCountAndDurationInTicks = QuickPulseDataAccumulator.EncodeCountAndDuration(1, dependencyCall.Duration.Ticks);
                Interlocked.Add(
                    ref this.dataAccumulatorManager.CurrentDataAccumulatorReference.AIDependencyCallCountAndDurationInTicks,
                    dependencyCallCountAndDurationInTicks);

                if (dependencyCall.Success == true)
                {
                    Interlocked.Increment(ref this.dataAccumulatorManager.CurrentDataAccumulatorReference.AIDependencyCallSuccessCount);
                }
                else if (dependencyCall.Success == false)
                {
                    Interlocked.Increment(ref this.dataAccumulatorManager.CurrentDataAccumulatorReference.AIDependencyCallFailureCount);
                }
            }
        }
    }
}