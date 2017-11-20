using System;
using System.Threading;

using Microsoft.ApplicationInsights.Metrics.Extensibility;
using Microsoft.ApplicationInsights.Metrics;
using System.ComponentModel;

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
        /// Groups extension methods that return pre-defined metric configurations.
        /// </summary>
        public static MetricConfigurations Common = new MetricConfigurations();
    }
}
