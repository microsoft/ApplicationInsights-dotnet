namespace Microsoft.ApplicationInsights.Extensibility.Implementation
{    
    using System.Collections.Generic;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.External;

    /// <summary>
    /// Encapsulates telemetry location information.
    /// </summary>
    public sealed class LocationContext
    {
        private readonly IDictionary<string, string> tags;

        internal LocationContext(IDictionary<string, string> tags)
        {
            this.tags = tags;
        }

        /// <summary>
        /// Gets or sets the location IP.
        /// </summary>
        public string Ip
        {
            get 
            { 
                return this.tags.GetTagValueOrNull(ContextTagKeys.Keys.LocationIp); 
            }

            set 
            {
                if (value != null)
                {
                    this.tags.SetStringValueOrRemove(ContextTagKeys.Keys.LocationIp, value);
                }
            }
        }
    }
}