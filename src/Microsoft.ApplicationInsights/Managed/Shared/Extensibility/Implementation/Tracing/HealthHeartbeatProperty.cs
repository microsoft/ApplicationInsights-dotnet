namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// Additional information added to the HealthHeartbeat sent by the Application Insights SDK
    /// </summary>
    public sealed class HealthHeartbeatProperty
    {
        /// <summary>
        /// Create a new property to add to, or update within, the payload for Health Heartbeats sent, with default values
        /// </summary>
        public HealthHeartbeatProperty() : this(string.Empty, null, true)
        {
        }

        /// <summary>
        /// Create a new property to add to, or update within, the payload for Health Heartbeats sent
        /// </summary>
        public HealthHeartbeatProperty(string name, object value, bool isHealthyValue)
        {
            this.Name = name;
            this.Value = value;
            this.IsHealthy = isHealthyValue;
        }

        /// <summary>
        /// Gets or sets the identifying name of the property to add or update in the health heartbeat payload
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the value of the property being added or updated in the health heartbeat payload
        /// </summary>
        public object Value { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether or not this property has a healthy or unhealthy value
        /// </summary>
        public bool IsHealthy { get; set; }
    }
}
