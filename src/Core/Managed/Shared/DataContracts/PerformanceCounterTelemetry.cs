namespace Microsoft.ApplicationInsights.DataContracts
{
    using System;
    using System.Collections.Generic;

    using Channel;

    /// <summary>
    /// The class that represents information about performance counters.
    /// </summary>
    [Obsolete("Use MetricTelemetry instead.")]
    public sealed class PerformanceCounterTelemetry : ITelemetry, ISupportProperties
    {
        internal readonly MetricTelemetry Data;

        private string categoryName = string.Empty;
        private string counterName = string.Empty;

        /// <summary>
        /// Initializes a new instance of the <see cref="PerformanceCounterTelemetry"/> class.
        /// </summary>
        public PerformanceCounterTelemetry()
        {
            this.Data = new MetricTelemetry();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PerformanceCounterTelemetry"/> class.
        /// </summary>
        /// <param name="categoryName">Category name.</param>
        /// <param name="counterName">Performance counter name.</param>
        /// <param name="instanceName">Instance name.</param>
        /// <param name="value">Performance counter value.</param>
        public PerformanceCounterTelemetry(string categoryName, string counterName, string instanceName, double value) 
            : this()
        {
            this.CategoryName = categoryName;
            this.CounterName = counterName;
            this.InstanceName = instanceName;
            this.Value = value;
        }

        /// <summary>
        /// Gets or sets date and time when telemetry was recorded.
        /// </summary>
        public DateTimeOffset Timestamp
        {
            get
            {
                return this.Data.Timestamp;
            }

            set
            {
                this.Data.Timestamp = value;
            }
        }

        /// <summary>
        /// Gets or sets the value that defines absolute order of the telemetry item.
        /// </summary>
        public string Sequence
        {
            get
            {
                return this.Data.Sequence;
            }

            set
            {
                this.Data.Sequence = value;
            }
        }

        /// <summary>
        /// Gets the context associated with the current telemetry item.
        /// </summary>
        public TelemetryContext Context
        {
            get
            {
                return this.Data.Context;
            }
        }

        /// <summary>
        /// Gets or sets the counter value.
        /// </summary>
        public double Value
        {
            get
            {
                return this.Data.Value;
            }

            set
            {
                this.Data.Value = value;
            }
        }

        /// <summary>
        /// Gets or sets the category name.
        /// </summary>
        public string CategoryName
        {
            get
            {
                return this.categoryName;
            }

            set
            {
                this.categoryName = value;
                this.UpdateName();
            }
        }

        /// <summary>
        /// Gets or sets the counter name.
        /// </summary>
        public string CounterName
        {
            get
            {
                return this.counterName;
            }

            set
            {
                this.counterName = value;
                this.UpdateName();
            }
        }

        /// <summary>
        /// Gets or sets the instance name.
        /// </summary>
        public string InstanceName
        {
            get
            {
                if (this.Properties.ContainsKey("CounterInstanceName"))
                {
                    return this.Properties["CounterInstanceName"];
                }

                return string.Empty;
            }

            set
            {
                this.Properties["CounterInstanceName"] = value;
                this.UpdateName();
            }
        }

        /// <summary>
        /// Gets a dictionary of application-defined property names and values providing additional information about this exception.
        /// </summary>
        public IDictionary<string, string> Properties
        {
            get { return this.Data.Properties; }
        }

        /// <summary>
        /// Sanitizes the properties based on constraints.
        /// </summary>
        void ITelemetry.Sanitize()
        {
            ((ITelemetry)this.Data).Sanitize();
        }

        private void UpdateName()
        {
            if (this.categoryName == "Processor")
            {
                this.Data.Name = "\\" + this.categoryName + "(_Total)\\" + this.counterName;
            }
            else if (this.categoryName == "Process")
            {
                this.Data.Name = "\\" + this.categoryName + "(??APP_WIN32_PROC??)\\" + this.counterName;
            }
            else if (this.categoryName == "ASP.NET Applications")
            {
                this.Data.Name = "\\" + this.categoryName + "(??APP_W3SVC_PROC??)\\" + this.counterName;
            }
            else if (this.categoryName == ".NET CLR Exceptions")
            {
                this.Data.Name = "\\" + this.categoryName + "(??APP_CLR_PROC??)\\" + this.counterName;
            }
            else
            {
                this.Data.Name = string.IsNullOrEmpty(this.InstanceName) ?
                    this.Data.Name = "\\" + this.categoryName + "\\" + this.counterName :
                    this.Data.Name = "\\" + this.categoryName + "(" + this.InstanceName + ")\\" + this.counterName;
            }
        }
    }
}
