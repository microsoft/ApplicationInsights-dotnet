namespace Microsoft.ApplicationInsights.Extensibility.Implementation
{
    /// <summary>
    /// Encapsulates telemetry location information.
    /// </summary>
    public sealed class LocationContext
    {
        private string ip;

        internal LocationContext()
        {
        }

        /// <summary>
        /// Gets or sets the location IP.
        /// </summary>
        public string Ip
        {
            get { return string.IsNullOrEmpty(this.ip) ? null : this.ip; }
            set { this.ip = value; }
        }
    }
}