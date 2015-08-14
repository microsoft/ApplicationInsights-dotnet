namespace Microsoft.ApplicationInsights.DataContracts
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Linq;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.External;

    using BondDependencyKind = Extensibility.Implementation.External.DependencyKind;

    /// <summary>
    /// The class that represents information about the collected dependency.
    /// </summary>
    [DebuggerDisplay(@"Value={Value}; Name={Name}; Count={Count}; Success={Success}; Async={Async}; Timestamp={Timestamp}")]
    public sealed class DependencyTelemetry : OperationTelemetry, ITelemetry, ISupportProperties
    {
        internal const string TelemetryName = "RemoteDependency";

        internal readonly string BaseType = typeof(RemoteDependencyData).Name;

        internal readonly RemoteDependencyData Data;
        private readonly TelemetryContext context;

        /// <summary>
        /// Initializes a new instance of the <see cref="DependencyTelemetry"/> class.
        /// </summary>
        public DependencyTelemetry()
        {
            this.Data = new RemoteDependencyData() { kind = DataPointType.Aggregation };
            this.context = new TelemetryContext(this.Data.properties, new Dictionary<string, string>());
            this.Data.dependencyKind = BondDependencyKind.Other;
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
            get { return this.Data.commandName; }
            set { this.Data.commandName = value; }
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
        /// Gets or sets dependency call duration.
        /// </summary>
        public override TimeSpan Duration
        {
            get { return TimeSpan.FromMilliseconds(this.Data.value); }
            set { this.Data.value = value.TotalMilliseconds; }
        }

        /// <summary>
        /// Gets or sets request count.
        /// </summary>
        public int? Count
        {
            get { return this.Data.count; }
            set { this.Data.count = value; }
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
        /// Gets or sets a value indicating whether the dependency call was made asynchronously or not.
        /// </summary>
        public bool? Async
        {
            get { return this.Data.async; }
            set { this.Data.async = value; }
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
        public string DependencyKind
        {
            get
            {
                return this.Data.dependencyKind.ToString();
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    this.Data.dependencyKind = BondDependencyKind.Other;
                }
                else
                {
                    // There is no TryParse in .Net 3.5
                    if (Enum.GetNames(typeof(BondDependencyKind)).Contains(value))
                    {
                        this.Data.dependencyKind = (BondDependencyKind)Enum.Parse(typeof(BondDependencyKind), value);
                    }
                    else
                    {
                        this.Data.dependencyKind = BondDependencyKind.Other;
                    }
                }
            }
        }

        /// <summary>
        /// Sanitizes the properties based on constraints.
        /// </summary>
        void ITelemetry.Sanitize()
        {
            this.Name = this.Name.SanitizeName();
            this.Name = Utils.PopulateRequiredStringValue(this.Name, "name", typeof(DependencyTelemetry).FullName);
            this.DependencyTypeName = this.DependencyTypeName.SanitizeValue();
            this.CommandName = this.CommandName.SanitizeCommandName();
            this.Properties.SanitizeProperties();
        }
    }
}
