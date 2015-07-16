namespace Microsoft.ApplicationInsights.DataContracts
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading;
    
    using Channel;
    using Extensibility.Implementation.External;
    
    /// <summary>
    /// The class that represents information about performance counters.
    /// </summary>
    [DebuggerDisplay(@"CategoryName={CategoryName}; CounterName={CounterName}; InstanceName={InstanceName}; Value={Value}; Timestamp={Timestamp}")]
    public sealed class PerformanceCounterTelemetry : ITelemetry, ISupportProperties
    {
        internal const string TelemetryName = "PerformanceCounter";
        internal readonly string BaseType = typeof(PerformanceCounterData).Name;
        internal readonly PerformanceCounterData Data;

        private TelemetryContext context;

        /// <summary>
        /// Initializes a new instance of the <see cref="PerformanceCounterTelemetry"/> class.
        /// </summary>
        public PerformanceCounterTelemetry()
        {
            this.Data = new PerformanceCounterData();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PerformanceCounterTelemetry"/> class.
        /// </summary>
        /// <param name="categoryName">Category name.</param>
        /// <param name="counterName">Performance counter name.</param>
        /// <param name="instanceName">Instance name.</param>
        /// <param name="value">Performance counter value.</param>
        public PerformanceCounterTelemetry(string categoryName, string counterName, string instanceName, double value) : this()
        {
            this.CategoryName = categoryName;
            this.CounterName = counterName;
            this.InstanceName = instanceName;
            this.Value = value;
        }

        /// <summary>
        /// Gets or sets date and time when telemetry was recorded.
        /// </summary>
        public DateTimeOffset Timestamp { get; set; }

        /// <summary>
        /// Gets or sets the value that defines absolute order of the telemetry item.
        /// </summary>
        public string Sequence { get; set; }

        /// <summary>
        /// Gets the context associated with the current telemetry item.
        /// </summary>
        public TelemetryContext Context
        {
            get
            {
                return LazyInitializer.EnsureInitialized(ref this.context);
            }
        }

        /// <summary>
        /// Gets or sets the counter value.
        /// </summary>
        public double Value
        {
            get
            {
                return this.Data.value;
            }

            set
            {
                this.Data.value = value;
            }
        }

        /// <summary>
        /// Gets or sets the category name.
        /// </summary>
        public string CategoryName
        {
            get
            {
                return this.Data.categoryName;
            }

            set
            {
                this.Data.categoryName = value;
            }
        }

        /// <summary>
        /// Gets or sets the counter name.
        /// </summary>
        public string CounterName
        {
            get
            {
                return this.Data.counterName;
            }

            set
            {
                this.Data.counterName = value;
            }
        }

        /// <summary>
        /// Gets or sets the instance name.
        /// </summary>
        public string InstanceName
        {
            get
            {
                return this.Data.instanceName;
            }

            set
            {
                this.Data.instanceName = value;
            }
        }

        /// <summary>
        /// Gets a dictionary of application-defined property names and values providing additional information about this exception.
        /// </summary>
        public IDictionary<string, string> Properties
        {
            get { return this.Data.properties; }
        }

        /// <summary>
        /// Sanitizes the properties based on constraints.
        /// </summary>
        void ITelemetry.Sanitize()
        {
        }
    }
}
