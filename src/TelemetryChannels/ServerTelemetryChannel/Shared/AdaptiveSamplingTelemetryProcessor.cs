namespace Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel
{
    using System;

    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel.Implementation;

    public class AdaptiveSamplingTelemetryProcessor : ITelemetryProcessor, IDisposable
    {
        private ITelemetryProcessor next;

        private SamplingTelemetryProcessor samplingProcessor;

        private SamplingPercentageEstimatorTelemetryProcessor estimatorProcessor;

        private SamplingPercentageEstimatorSettings estimatorSettings;

        private double initialSamplingPercentage;

        private AdaptiveSamplingPercentageEvaluatedCallback evaluationCallback;

        /// <summary>
        /// Initializes a new instance of the <see cref="SamplingTelemetryProcessor"/> class.
        /// <param name="next">Next TelemetryProcessor in call chain.</param>
        /// </summary>
        public AdaptiveSamplingTelemetryProcessor(ITelemetryProcessor next)
            : this(new SamplingPercentageEstimatorSettings(), null, next)
        {
        }

        public AdaptiveSamplingTelemetryProcessor(
            SamplingPercentageEstimatorSettings settings,
            AdaptiveSamplingPercentageEvaluatedCallback callback,
            ITelemetryProcessor next)
        {
            this.estimatorSettings = settings;
            this.evaluationCallback = callback;

            // make estimatortelemetry processor  work after sampling was done
            this.estimatorProcessor = new SamplingPercentageEstimatorTelemetryProcessor(settings, this.SamplingPercentageChanged, next);
            this.samplingProcessor = new SamplingTelemetryProcessor(this.estimatorProcessor);

            this.initialSamplingPercentage = 100.0;
        }

        public double InitialSamplingPercentage
        {
            get
            {
                return this.initialSamplingPercentage;
            }
            set
            {
                // note: 'initial' percentage will affect sampling even 
                // if it was running for a while
                this.initialSamplingPercentage = value;
                this.samplingProcessor.SamplingPercentage = this.initialSamplingPercentage;
            }
        }

        public double MaxTelemetryItemsPerSecond
        {
            get
            {
                return this.estimatorSettings.MaxTelemetryItemsPerSecond;
            }
            set
            {
                this.estimatorSettings.MaxTelemetryItemsPerSecond = value;
            }
        }

        public double MinSamplingPercentage
        {
            get
            {
                return this.estimatorSettings.MinSamplingPercentage;
            }
            set
            {
                this.estimatorSettings.MinSamplingPercentage = value;
            }
        }

        public double MaxSamplingPercentage
        {
            get
            {
                return this.estimatorSettings.MaxSamplingPercentage;
            }
            set
            {
                this.estimatorSettings.MaxSamplingPercentage = value;
            }
        }

        public int EvaluationIntervalSeconds
        {
            get
            {
                return this.estimatorSettings.EvaluationIntervalSeconds;
            }
            set
            {
                this.estimatorSettings.EvaluationIntervalSeconds = value;
            }
        }

        public int SamplingPercentageDecreaseTimeoutSeconds
        {
            get
            {
                return this.estimatorSettings.SamplingPercentageDecreaseTimeoutSeconds;
            }
            set
            {
                this.estimatorSettings.SamplingPercentageDecreaseTimeoutSeconds = value;
            }
        }

        public int SamplingPercentageIncreaseTimeoutSeconds
        {
            get
            {
                return this.estimatorSettings.SamplingPercentageIncreaseTimeoutSeconds;
            }
            set
            {
                this.estimatorSettings.SamplingPercentageIncreaseTimeoutSeconds = value;
            }
        }

        public double MovingAverageRatio
        {
            get
            {
                return this.estimatorSettings.MovingAverageRatio;
            }
            set
            {
                this.estimatorSettings.MovingAverageRatio = value;
            }
        }

        public void Process(ITelemetry item)
        {
            this.samplingProcessor.Process(item);
        }

        public void Dispose()
        {
            if (this.estimatorProcessor != null)
            {
                this.estimatorProcessor.Dispose();
                this.estimatorProcessor = null;
            }
        }

        private void SamplingPercentageChanged(
            double afterSamplingTelemetryItemRatePerSecond,
            double currentSamplingPercentage,
            double newSamplingPercentage,
            bool isSamplingPercentageChanged,
            SamplingPercentageEstimatorSettings settings)
        {
            if (isSamplingPercentageChanged)
            {
                this.samplingProcessor.SamplingPercentage = newSamplingPercentage;
            }

            if (this.evaluationCallback != null)
            {
                this.evaluationCallback(afterSamplingTelemetryItemRatePerSecond,
                    currentSamplingPercentage,
                    newSamplingPercentage,
                    isSamplingPercentageChanged,
                    settings);
            }
        }
    }
}
