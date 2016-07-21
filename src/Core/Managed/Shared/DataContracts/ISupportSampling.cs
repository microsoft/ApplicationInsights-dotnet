namespace Microsoft.ApplicationInsights.DataContracts
{
    /// <summary>
    /// Represent objects that support data sampling.
    /// </summary>
    public interface ISupportSampling
    {
        /// <summary>
        /// Gets or sets data sampling percentage (between 0 and 100).
        /// </summary>
        double SamplingPercentage { get; set; }

        /// <summary>
        /// Gets or sets if sampling should be skipped.
        /// </summary>
        bool SamplingSkipped { get; set; }
    }
}