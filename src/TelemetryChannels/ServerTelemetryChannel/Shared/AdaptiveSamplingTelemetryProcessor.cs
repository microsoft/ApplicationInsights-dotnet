namespace Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel
{
    using System;

    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel.Implementation;

    /// <summary>
    /// Telemetry processor for sampling telemetry at a dynamic rate before sending to Application Insights.
    /// </summary>
    public class AdaptiveSamplingTelemetryProcessor : ITelemetryProcessor, IDisposable
    {
        /// <summary>
        /// Fixed-rate sampling telemetry processor.
        /// </summary>
        private SamplingTelemetryProcessor samplingProcessor;

        /// <summary>
        /// Sampling percentage estimator telemetry processor.
        /// </summary>
        private SamplingPercentageEstimatorTelemetryProcessor estimatorProcessor;

        /// <summary>
        /// Sampling percentage estimator settings.
        /// </summary>
        private SamplingPercentageEstimatorSettings estimatorSettings;

        /// <summary>
        /// Initial sampling percentage applied after start of the process.
        /// </summary>
        private double initialSamplingPercentage;

        /// <summary>
        /// Callback invoked every time sampling percentage is evaluated.
        /// </summary>
        private AdaptiveSamplingPercentageEvaluatedCallback evaluationCallback;

        /// <summary>
        /// Initializes a new instance of the <see cref="AdaptiveSamplingTelemetryProcessor"/> class.
        /// <param name="next">Next TelemetryProcessor in call chain.</param>
        /// </summary>
        public AdaptiveSamplingTelemetryProcessor(ITelemetryProcessor next)
            : this(new SamplingPercentageEstimatorSettings(), null, next)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AdaptiveSamplingTelemetryProcessor"/> class.
        /// <param name="settings">Sampling percentage estimator settings.</param>
        /// <param name="callback">Callback invoked every time sampling percentage is evaluated.</param>
        /// <param name="next">Next TelemetryProcessor in call chain.</param>
        /// </summary>
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

        /// <summary>
        /// Gets or sets initial sampling percentage applied at the start
        /// of the process to dynamically vary the percentage.
        /// </summary>
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

        /// <summary>
        /// Gets or sets maximum rate of telemetry items per second
        /// dynamic sampling will try to adhere to.
        /// </summary>
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

        /// <summary>
        /// Gets or sets minimum sampling percentage that can be set 
        /// by the dynamic sampling percentage algorithm.
        /// </summary>
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

        /// <summary>
        /// Gets or sets maximum sampling percentage that can be set 
        /// by the dynamic sampling percentage algorithm.
        /// </summary>
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

        /// <summary>
        /// Gets or sets duration of the sampling percentage evaluation 
        /// interval in seconds.
        /// </summary>
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

        /// <summary>
        /// Gets or sets a value indicating how long to not to decrease
        /// sampling percentage after last change to prevent excessive fluctuation.
        /// </summary>
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

        /// <summary>
        /// Gets or sets a value indicating how long to not to increase
        /// sampling percentage after last change to prevent excessive fluctuation.
        /// </summary>
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

        /// <summary>
        /// Gets or sets exponential moving average ratio (factor) applied
        /// during calculation of rate of telemetry items produced by the application.
        /// </summary>
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

        /// <summary>
        /// Processes telemetry item.
        /// </summary>
        /// <param name="item">Telemetry item to process.</param>
        public void Process(ITelemetry item)
        {
            this.samplingProcessor.Process(item);
        }

        /// <summary>
        /// Disposes the object.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes the object.
        /// </summary>
        /// <param name="disposing">True if disposing.</param>
        protected virtual void Dispose(bool disposing)
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
                this.evaluationCallback(
                    afterSamplingTelemetryItemRatePerSecond,
                    currentSamplingPercentage,
                    newSamplingPercentage,
                    isSamplingPercentageChanged,
                    settings);
            }
        }
    }
}
