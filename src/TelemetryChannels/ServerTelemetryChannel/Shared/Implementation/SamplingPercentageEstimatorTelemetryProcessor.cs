namespace Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel.Implementation
{
    using System;
    using System.Threading;

    using Microsoft.ApplicationInsights.Channel;

    public delegate void AdaptiveSamplingPercentageEvaluatedCallback(
        double afterSamplingTelemetryItemRatePerSecond,
        double currentSamplingPercentage,
        double newSamplingPercentage,
        bool isSamplingPercentageChanged,
        SamplingPercentageEstimatorSettings settings);

    internal class SamplingPercentageEstimatorTelemetryProcessor : ITelemetryProcessor, IDisposable
    {
        private ITelemetryProcessor next;

        private SamplingPercentageEstimatorSettings settings;

        private ExponentialMovingAverageCounter itemCount;

        private Timer evaluationTimer;

        private int evaluationIntervalMs;

        private int currenSamplingRate;

        private DateTimeOffset samplingPercentageLastChangeDateTime;

        private AdaptiveSamplingPercentageEvaluatedCallback evaluationCallback;

        /// <summary>
        /// Initializes a new instance of the <see cref="SamplingTelemetryProcessor"/> class.
        /// <param name="next">Next TelemetryProcessor in call chain.</param>
        /// </summary>
        public SamplingPercentageEstimatorTelemetryProcessor(ITelemetryProcessor next)
            : this(new SamplingPercentageEstimatorSettings(), null, next)
        {
        }

        public SamplingPercentageEstimatorTelemetryProcessor(SamplingPercentageEstimatorSettings settings, AdaptiveSamplingPercentageEvaluatedCallback callback, ITelemetryProcessor next)
        {
            if (settings == null)
            {
                throw new ArgumentNullException("settings");
            }

            if (next == null)
            {
                throw new ArgumentNullException("next");
            }

            this.evaluationCallback = callback;
            this.settings = settings;
            this.next = next;

            // set sampling rate to minimum initially
            this.currenSamplingRate = settings.EffectiveMinSamplingRate;

            this.itemCount = new ExponentialMovingAverageCounter(settings.EffectiveMovingAverageRatio);

            this.samplingPercentageLastChangeDateTime = DateTimeOffset.UtcNow;

            // set evaluation interval to default value if it is negative or zero
            this.evaluationIntervalMs = this.settings.EffectiveEvaluationIntervalSeconds * 1000;

            // set up timer to run math to estimate sampling percentage
            this.evaluationTimer = new Timer(
                EstimateSamplingPercentage, 
                null,
                this.evaluationIntervalMs,
                this.evaluationIntervalMs);
        }

        public void Process(ITelemetry item)
        {
            // increment post-samplin telemetry item counter
            this.itemCount.Increment();

            // continue processing telemetry item with the next telemetry processor
            this.next.Process(item);
        }

        public void Dispose()
        {
            if (this.evaluationTimer != null)
            {
                this.evaluationTimer.Dispose();
                this.evaluationTimer = null;
            }
        }

        private void EstimateSamplingPercentage(object state)
        {
            // get observed after-sampling eps
            double observedEps = this.itemCount.StartNewInterval() * 1000 / this.evaluationIntervalMs;

            // we see events post sampling, so get pre-sampling eps
            double beforeSamplingEps = observedEps * this.currenSamplingRate;

            // caclulate suggested sampling rate
            int suggestedSamplingRate = (int)Math.Ceiling(beforeSamplingEps / this.settings.EffectiveMaxTelemetryItemsPerSecond);

            // adjust suggested rate so that it fits between min and max configured
            if (suggestedSamplingRate > this.settings.EffectiveMaxSamplingRate)
            {
                suggestedSamplingRate = this.settings.EffectiveMaxSamplingRate;
            }

            if (suggestedSamplingRate < this.settings.EffectiveMinSamplingRate)
            {
                suggestedSamplingRate = this.settings.EffectiveMinSamplingRate;
            }

            // see if evaluation interval was changed and apply change
            int newEvaluationIntervalMs = this.settings.EffectiveEvaluationIntervalSeconds * 1000;

            if (this.evaluationIntervalMs != newEvaluationIntervalMs)
            {
                this.evaluationIntervalMs = newEvaluationIntervalMs;
                this.evaluationTimer.Change(this.evaluationIntervalMs, this.evaluationIntervalMs);
            }

            // check to see if sampling rate needs changes
            bool samplingPercentageChangeNeeded = suggestedSamplingRate != this.currenSamplingRate;

            if (samplingPercentageChangeNeeded)
            {
                // check to see if enough time passed since last sampling % change
                if ((DateTimeOffset.UtcNow - this.samplingPercentageLastChangeDateTime).TotalSeconds <
                    (suggestedSamplingRate > this.currenSamplingRate
                        ? this.settings.EffectivePercentageDecreaseTimeoutSeconds
                        : this.settings.EffectivePercentageIncreaseTimeoutSeconds))
                {
                    samplingPercentageChangeNeeded = false;
                }
            }

            // call evaluation callback if provided
            if (this.evaluationCallback != null)
            {
                // we do not want to crash timer thread knocking out the process
                // in case customer-provided callback failed
                try
                {
                    this.evaluationCallback(
                        observedEps,
                        100.0 / this.currenSamplingRate,
                        100.0 / suggestedSamplingRate,
                        samplingPercentageChangeNeeded,
                        this.settings);
                }
                catch
                {
                    // TODO: Report exception somehow
                }
            }

            if (samplingPercentageChangeNeeded)
            { 
                // apply sampling perfcentage change
                this.samplingPercentageLastChangeDateTime = DateTimeOffset.UtcNow;
                this.currenSamplingRate = suggestedSamplingRate;

                // since we're observing event count post sampling and we're about
                // to change sampling rate, reset counter
                this.itemCount = new ExponentialMovingAverageCounter(this.settings.EffectiveMovingAverageRatio);
            }
        }
    }
}
