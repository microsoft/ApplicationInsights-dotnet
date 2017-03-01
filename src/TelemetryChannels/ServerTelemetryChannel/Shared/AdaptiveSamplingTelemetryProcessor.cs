namespace Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel
{
    using System;

    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.WindowsServer.Channel.Implementation;
    using Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel.Implementation;

    /// <summary>
    /// Telemetry processor for sampling telemetry at a dynamic rate before sending to Application Insights.
    /// </summary>
    public class AdaptiveSamplingTelemetryProcessor : ITelemetryProcessor, ITelemetryModule, IDisposable
    {
        /// <summary>
        /// Fixed-rate sampling telemetry processor.
        /// </summary>
        private readonly SamplingTelemetryProcessor samplingProcessor;
        
        /// <summary>
        /// Sampling percentage estimator settings.
        /// </summary>
        private readonly SamplingPercentageEstimatorSettings estimatorSettings;

        /// <summary>
        /// Callback invoked every time sampling percentage is evaluated.
        /// </summary>
        private readonly AdaptiveSamplingPercentageEvaluatedCallback evaluationCallback;

        /// <summary>
        /// Sampling percentage estimator telemetry processor.
        /// </summary>
        private SamplingPercentageEstimatorTelemetryProcessor estimatorProcessor;

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
        }

        /// <summary>
        /// Gets or sets a semicolon separated list of telemetry types that should not be sampled. 
        /// Types listed are excluded even if they are set in IncludedTypes.
        /// </summary>
        public string ExcludedTypes
        {
            get { return this.samplingProcessor.ExcludedTypes; }

            set { this.samplingProcessor.ExcludedTypes = value; }
        }

        /// <summary>
        /// Gets or sets a semicolon separated list of telemetry types that should be sampled. 
        /// If left empty all types are included implicitly. 
        /// Types are not included if they are set in ExcludedTypes.
        /// </summary>
        public string IncludedTypes
        {
            get { return this.samplingProcessor.IncludedTypes; }

            set { this.samplingProcessor.IncludedTypes = value; }
        }

        /// <summary>
        /// Gets or sets initial sampling percentage applied at the start
        /// of the process to dynamically vary the percentage.
        /// </summary>
        public double InitialSamplingPercentage
        {
            get
            {
                return this.estimatorSettings.InitialSamplingPercentage;
            }

            set
            {
                // note: 'initial' percentage will affect sampling even 
                // if it was running for a while
                this.estimatorSettings.InitialSamplingPercentage = value;
                this.samplingProcessor.SamplingPercentage = value;
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
        /// Gets or sets duration of the sampling percentage evaluation interval.
        /// </summary>
        public TimeSpan EvaluationInterval
        {
            get
            {
                return this.estimatorSettings.EvaluationInterval;
            }

            set
            {
                this.estimatorSettings.EvaluationInterval = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating how long to not to decrease
        /// sampling percentage after last change to prevent excessive fluctuation.
        /// </summary>
        public TimeSpan SamplingPercentageDecreaseTimeout
        {
            get
            {
                return this.estimatorSettings.SamplingPercentageDecreaseTimeout;
            }

            set
            {
                this.estimatorSettings.SamplingPercentageDecreaseTimeout = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating how long to not to increase
        /// sampling percentage after last change to prevent excessive fluctuation.
        /// </summary>
        public TimeSpan SamplingPercentageIncreaseTimeout
        {
            get
            {
                return this.estimatorSettings.SamplingPercentageIncreaseTimeout;
            }

            set
            {
                this.estimatorSettings.SamplingPercentageIncreaseTimeout = value;
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
        /// Initializes this processor using the correct telemetry pipeline configuration.
        /// </summary>
        /// <param name="configuration"></param>
        public void Initialize(TelemetryConfiguration configuration)
        {
            this.samplingProcessor.Initialize(configuration);
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
            if (disposing)
            {
                IDisposable estimatorProc = this.estimatorProcessor;
                if (estimatorProc != null)
                {
                    estimatorProc.Dispose();
                    this.estimatorProcessor = null;
                }

                IDisposable samplingProc = this.samplingProcessor;
                if (samplingProc != null)
                {
                    samplingProc.Dispose();
                    // Cannot set samplingProcessor = null, since it is readonly, but multiple Dispose calls must be idempotent.
                }
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
                TelemetryChannelEventSource.Log.SamplingChanged(newSamplingPercentage);
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
