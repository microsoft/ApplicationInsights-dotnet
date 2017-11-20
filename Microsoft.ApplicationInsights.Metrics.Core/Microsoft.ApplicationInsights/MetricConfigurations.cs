using System;

namespace Microsoft.ApplicationInsights
{
    /// <summary>
    /// Static container for the most commonly used metric configurations.
    /// </summary>
    public sealed class MetricConfigurations
    {
        private MetricConfigurations()
        {
        }
        
        /// <summary>
        /// Groups extension methods that return pre-defined metric configurations and related constants.
        /// </summary>
        public static readonly MetricConfigurations Common = new MetricConfigurations();
    }
}
