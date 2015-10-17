namespace Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel.Implementation
{
    using System;

    public class SamplingPercentageEstimatorSettings
    {
        private static SamplingPercentageEstimatorSettings Default = new SamplingPercentageEstimatorSettings();

        public SamplingPercentageEstimatorSettings()
        {
            // set defalt values
            this.MaxTelemetryItemsPerSecond = 5.0;
            this.MinSamplingPercentage = 0.1;
            this.MaxSamplingPercentage = 100.0;
            this.EvaluationIntervalSeconds = 15;
            this.SamplingPercentageDecreaseTimeoutSeconds = 2 * 60;
            this.SamplingPercentageIncreaseTimeoutSeconds = 15 * 60;
            this.MovingAverageRatio = 0.25;
        }
        
        public double MaxTelemetryItemsPerSecond { get; set; }

        public double MinSamplingPercentage { get; set; }

        public double MaxSamplingPercentage { get; set; }

        public int EvaluationIntervalSeconds { get; set; }

        public int SamplingPercentageDecreaseTimeoutSeconds { get; set; }

        public int SamplingPercentageIncreaseTimeoutSeconds { get; set; }

        public double MovingAverageRatio { get; set; }

        internal double EffectiveMaxTelemetryItemsPerSecond
        {
            get
            {
                return this.MaxTelemetryItemsPerSecond <= 0 ? 1E-12 : this.MaxTelemetryItemsPerSecond;
            }
        }

        internal int EffectiveMinSamplingRate
        {
            get
            {
                double effectiveMaxSamplingPercentage = this.MaxSamplingPercentage > 100 ? 100 : this.MaxSamplingPercentage <= 0 ? 1E-12 : this.MaxSamplingPercentage;

                return (int)Math.Floor(100 / effectiveMaxSamplingPercentage);
            }
        }

        internal int EffectiveMaxSamplingRate
        {
            get
            {
                double effectiveMinSamplingPercentage = this.MinSamplingPercentage > 100 ? 100 : this.MinSamplingPercentage <= 0 ? 1E-12 : this.MinSamplingPercentage;

                return (int)Math.Ceiling(100 / effectiveMinSamplingPercentage);
            }
        }

        internal int EffectiveEvaluationIntervalSeconds
        {
            get
            {
                return this.EvaluationIntervalSeconds <= 0 
                    ? Default.EvaluationIntervalSeconds 
                    : this.EvaluationIntervalSeconds;
            }
        }

        internal int EffectivePercentageDecreaseTimeoutSeconds
        {
            get
            {
                return this.SamplingPercentageDecreaseTimeoutSeconds <= 0 
                    ? Default.SamplingPercentageDecreaseTimeoutSeconds 
                    : this.SamplingPercentageDecreaseTimeoutSeconds;
            }
        }

        internal int EffectivePercentageIncreaseTimeoutSeconds
        {
            get
            {
                return this.SamplingPercentageIncreaseTimeoutSeconds <= 0 
                    ? Default.EffectivePercentageIncreaseTimeoutSeconds 
                    : this.SamplingPercentageIncreaseTimeoutSeconds;
            }
        }

        internal double EffectiveMovingAverageRatio
        {
            get
            {
                return this.MovingAverageRatio < 0 
                    ? Default.MovingAverageRatio 
                    : this.MovingAverageRatio;
            }
        }
    }
}
