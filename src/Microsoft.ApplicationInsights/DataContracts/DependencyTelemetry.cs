namespace Microsoft.ApplicationInsights.DataContracts
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.External;

    /// <summary>
    /// The class that represents information about the collected dependency.
    /// <a href="https://go.microsoft.com/fwlink/?linkid=839889">Learn more.</a>
    /// </summary>
    public sealed class DependencyTelemetry : OperationTelemetry, ITelemetry, ISupportProperties, ISupportSampling, ISupportMetrics
    {
        internal new const string TelemetryName = "RemoteDependency";

        internal readonly string BaseType = typeof(RemoteDependencyData).Name;

        internal readonly RemoteDependencyData InternalData;
        private readonly TelemetryContext context;

        private double? samplingPercentage;

        private bool successFieldSet;

        /// <summary>
        /// Initializes a new instance of the <see cref="DependencyTelemetry"/> class.
        /// </summary>
        public DependencyTelemetry()
        {
            this.InternalData = new RemoteDependencyData();
            this.successFieldSet = true;
            this.context = new TelemetryContext(this.InternalData.properties);
            this.GenerateId();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DependencyTelemetry"/> class with the given <paramref name="dependencyName"/>, <paramref name="data"/>,
        /// <paramref name="startTime"/>, <paramref name="duration"/> and <paramref name="success"/> property values.
        /// </summary>
        [Obsolete("Use other constructors which allows to define dependency call with all the properties.")]
        public DependencyTelemetry(string dependencyName, string data, DateTimeOffset startTime, TimeSpan duration, bool success)
            : this()
        {
            this.Name = dependencyName;
            this.Data = data;
            this.Duration = duration;
            this.Success = success;
            this.Timestamp = startTime;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DependencyTelemetry"/> class with the given <paramref name="dependencyName"/>, <paramref name="target"/>,
        /// <paramref name="dependencyName"/>, <paramref name="data"/> property values.
        /// </summary>
        public DependencyTelemetry(string dependencyTypeName, string target, string dependencyName, string data)
            : this()
        {
            this.Type = dependencyTypeName;
            this.Target = target;
            this.Name = dependencyName;
            this.Data = data;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DependencyTelemetry"/> class with the given <paramref name="dependencyName"/>, <paramref name="target"/>,
        /// <paramref name="dependencyName"/>, <paramref name="data"/>, <paramref name="startTime"/>, <paramref name="duration"/>, <paramref name="resultCode"/>
        /// and <paramref name="success"/> and  property values.
        /// </summary>
        public DependencyTelemetry(string dependencyTypeName, string target, string dependencyName, string data, DateTimeOffset startTime, TimeSpan duration, string resultCode, bool success)
            : this()
        {
            this.Type = dependencyTypeName;
            this.Target = target;
            this.Name = dependencyName;
            this.Data = data;
            this.Timestamp = startTime;
            this.Duration = duration;
            this.ResultCode = resultCode;
            this.Success = success;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DependencyTelemetry"/> class by cloning an existing instance.
        /// </summary>
        /// <param name="source">Source instance of <see cref="DependencyTelemetry"/> to clone from.</param>
        private DependencyTelemetry(DependencyTelemetry source)
        {
            this.InternalData = source.InternalData.DeepClone();
            this.context = source.context.DeepClone(this.InternalData.properties);
            this.Sequence = source.Sequence;
            this.Timestamp = source.Timestamp;
            this.samplingPercentage = source.samplingPercentage;
            this.successFieldSet = source.successFieldSet;
        }

        /// <summary>
        /// Gets or sets date and time when telemetry was recorded.
        /// </summary>
        public override DateTimeOffset Timestamp { get; set; }

        /// <summary>
        /// Gets or sets the value that defines absolute order of the telemetry item.
        /// </summary>
        public override string Sequence { get; set; }

        /// <summary>
        /// Gets the context associated with the current telemetry item.
        /// </summary>
        public override TelemetryContext Context
        {
            get { return this.context; }
        }

        /// <summary>
        /// Gets or sets Dependency ID.
        /// </summary>
        public override string Id
        {
            get { return this.InternalData.id; }
            set { this.InternalData.id = value; }
        }

        /// <summary>
        /// Gets or sets the Result Code.
        /// </summary>
        public string ResultCode
        {
            get { return this.InternalData.resultCode; }
            set { this.InternalData.resultCode = value; }
        }

        /// <summary>
        /// Gets or sets resource name.
        /// </summary>
        public override string Name
        {
            get { return this.InternalData.name; }
            set { this.InternalData.name = value; }
        }

        /// <summary>
        /// Gets or sets text of SQL command or empty it not applicable.
        /// </summary>
        [Obsolete("Renamed to Data")]
        public string CommandName
        {
            get { return this.InternalData.data; }
            set { this.InternalData.data = value; }
        }

        /// <summary>
        /// Gets or sets data associated with the current dependency instance. Command name/statement for SQL dependency, URL for http dependency.
        /// </summary>
        public string Data
        {
            get { return this.InternalData.data; }
            set { this.InternalData.data = value; }
        }

        /// <summary>
        /// Gets or sets target of dependency call. SQL server name, url host, etc.
        /// </summary>
        public string Target
        {
            get { return this.InternalData.target; }
            set { this.InternalData.target = value; }
        }

        /// <summary>
        /// Gets or sets the dependency type name.
        /// </summary>
        [Obsolete("Renamed to Type")]
        public string DependencyTypeName
        {
            get { return this.Type;  }
            set { this.Type = value; }
        }

        /// <summary>
        /// Gets or sets the dependency type name.
        /// </summary>
        public string Type
        {
            get { return this.InternalData.type; }
            set { this.InternalData.type = value; }
        }

        /// <summary>
        /// Gets or sets the amount of time it took the application to handle the request.
        /// </summary>
        public override TimeSpan Duration
        {
            get { return Utils.ValidateDuration(this.InternalData.duration); }
            set { this.InternalData.duration = value.ToString(); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the dependency call was successful or not.
        /// </summary>
        public override bool? Success
        {
            get
            {
                if (this.successFieldSet)
                {
                    return this.InternalData.success;
                }

                return null;
            }

            set
            {
                if (value != null && value.HasValue)
                {
                    this.InternalData.success = value.Value;
                    this.successFieldSet = true;
                }
                else
                {
                    this.InternalData.success = true;
                    this.successFieldSet = false;
                }
            }
        }

        /// <summary>
        /// Gets a dictionary of application-defined property names and values providing additional information about this remote dependency.
        /// <a href="https://go.microsoft.com/fwlink/?linkid=525722#properties">Learn more</a>
        /// </summary>
        public override IDictionary<string, string> Properties
        {
            get { return this.InternalData.properties; }
        }

        /// <summary>
        /// Gets a dictionary of application-defined event metrics.
        /// <a href="https://go.microsoft.com/fwlink/?linkid=525722#properties">Learn more</a>
        /// </summary>
        public override IDictionary<string, double> Metrics
        {
            get { return this.InternalData.measurements; }
        }

        /// <summary>
        /// Gets or sets the dependency kind, like SQL, HTTP, Azure, etc.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use Type")]
        public string DependencyKind
        {
            get
            {
                return this.DependencyTypeName;
            }

            set
            {
                this.DependencyTypeName = value;
            }
        }

        /// <summary>
        /// Gets or sets data sampling percentage (between 0 and 100).
        /// Should be 100/n where n is an integer. <a href="https://go.microsoft.com/fwlink/?linkid=832969">Learn more</a>
        /// </summary>
        double? ISupportSampling.SamplingPercentage
        {
            get { return this.samplingPercentage; }
            set { this.samplingPercentage = value; }
        }

        /// <summary>
        /// Deeply clones a <see cref="DependencyTelemetry"/> object.
        /// </summary>
        /// <returns>A cloned instance.</returns>
        public override ITelemetry DeepClone()
        {
            return new DependencyTelemetry(this);
        }

        /// <summary>
        /// Sanitizes the properties based on constraints.
        /// </summary>
        void ITelemetry.Sanitize()
        {
            this.Name = this.Name.SanitizeName();
            this.Name = Utils.PopulateRequiredStringValue(this.Name, "name", typeof(DependencyTelemetry).FullName);
            this.Id.SanitizeName();
            this.ResultCode = this.ResultCode.SanitizeResultCode();
            this.Type = this.Type.SanitizeDependencyType();
            this.Data = this.Data.SanitizeData();
            this.Properties.SanitizeProperties();
        }
    }
}
