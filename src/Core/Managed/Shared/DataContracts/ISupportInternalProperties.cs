namespace Microsoft.ApplicationInsights.DataContracts
{
    internal interface ISupportInternalProperties
    {
        /// <summary>
        /// Gets or sets a value indicating whether the telemetry was sent.
        /// </summary>
        bool Sent { get; set; }
    }
}
