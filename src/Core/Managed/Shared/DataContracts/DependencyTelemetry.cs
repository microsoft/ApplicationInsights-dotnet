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
    /// </summary>
    public sealed class DependencyTelemetry : OperationTelemetry, ITelemetry, ISupportProperties, ISupportSampling
    {
        internal const string TelemetryName = "RemoteDependency";

        internal readonly string BaseType = typeof(RemoteDependencyData).Name;

        internal readonly RemoteDependencyData Data;
        private readonly TelemetryContext context;

        private double? samplingPercentage;

        /// <summary>
        /// Initializes a new instance of the <see cref="DependencyTelemetry"/> class.
        /// </summary>
        public DependencyTelemetry()
        {
            this.Data = new RemoteDependencyData();
            this.context = new TelemetryContext(this.Data.properties);
            this.Id = Convert.ToBase64String(BitConverter.GetBytes(WeakConcurrentRandom.Instance.Next()));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DependencyTelemetry"/> class with the given <paramref name="dependencyName"/>, <paramref name="commandName"/>, 
        /// <paramref name="startTime"/>, <paramref name="duration"/> and <paramref name="success"/> property values.
        /// </summary>
        public DependencyTelemetry(string dependencyName, string commandName, DateTimeOffset startTime, TimeSpan duration, bool success)
            : this()
        {
            this.Name = dependencyName;
            this.CommandName = commandName;
            this.Duration = duration;
            this.Success = success;
            this.StartTime = startTime;
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
            get { return this.Data.id; }
            set { this.Data.id = value; }
        }

        /// <summary>
        /// Gets or sets the Result Code.
        /// </summary>
        public string ResultCode
        {
            get { return this.Data.resultCode; }
            set { this.Data.resultCode = value; }
        }

        /// <summary>
        /// Gets or sets resource name.
        /// </summary>
        public override string Name
        {
            get { return this.Data.name; }
            set { this.Data.name = value; }
        }

        /// <summary>
        /// Gets or sets text of SQL command or empty it not applicable.
        /// </summary>
        public string CommandName
        {
            get { return this.Data.data; }
            set { this.Data.data = value; }
        }

        /// <summary>
        /// Gets or sets the dependency type name.
        /// </summary>
        public string DependencyTypeName
        {
            get { return this.Data.dependencyTypeName;  }
            set { this.Data.dependencyTypeName = value; }
        }

        /// <summary>
        /// Gets or sets the date and time when dependency was called by the application.
        /// </summary>
        public override DateTimeOffset StartTime 
        {
            get { return this.Timestamp; }
            set { this.Timestamp = value; } 
        }

        /// <summary>
        /// Gets or sets the amount of time it took the application to handle the request.
        /// </summary>
        public override TimeSpan Duration
        {
            get { return Utils.ValidateDuration(this.Data.duration); }
            set { this.Data.duration = value.ToString(); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the dependency call was successful or not.
        /// </summary>
        public override bool? Success
        {
            get { return this.Data.success; }
            set { this.Data.success = value; }
        }

        /// <summary>
        /// Gets a dictionary of application-defined property names and values providing additional information about this remote dependency.
        /// </summary>
        public override IDictionary<string, string> Properties
        {
            get { return this.Data.properties; }
        }

        /// <summary>
        /// Gets or sets the dependency kind, like SQL, HTTP, Azure, etc.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use DependencyTypeName")]
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
        /// </summary>
        double? ISupportSampling.SamplingPercentage
        {
            get { return this.samplingPercentage; }
            set { this.samplingPercentage = value; }
        }

        /// <summary>
        /// Sanitizes the properties based on constraints.
        /// </summary>
        void ITelemetry.Sanitize()
        {
            this.Name = this.Name.SanitizeName();
            this.Name = Utils.PopulateRequiredStringValue(this.Name, "name", typeof(DependencyTelemetry).FullName);
            this.Id.SanitizeName();
            this.ResultCode = this.ResultCode.SanitizeValue();
            this.DependencyTypeName = this.DependencyTypeName.SanitizeDependencyType();
            this.CommandName = this.CommandName.SanitizeCommandName();
            this.Properties.SanitizeProperties();
        }
    }
}
