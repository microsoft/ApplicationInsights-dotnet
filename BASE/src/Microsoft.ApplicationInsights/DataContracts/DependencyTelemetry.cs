namespace Microsoft.ApplicationInsights.DataContracts
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Threading;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;

    /// <summary>
    /// The class that represents information about the collected dependency.
    /// <a href="https://go.microsoft.com/fwlink/?linkid=839889">Learn more.</a>
    /// </summary>
    public sealed class DependencyTelemetry : OperationTelemetry, ITelemetry, ISupportProperties
    {
        internal const string EtwEnvelopeName = "RemoteDependency";
        internal string EnvelopeName = "AppDependencies";
        
        private readonly TelemetryContext context;
        private bool successFieldSet;
        private bool success = true;

        /// <summary>
        /// Initializes a new instance of the <see cref="DependencyTelemetry"/> class.
        /// </summary>
        public DependencyTelemetry()
        {            
            this.successFieldSet = true;
            this.context = new TelemetryContext();
            this.Name = string.Empty;
            this.ResultCode = string.Empty;
            this.Duration = TimeSpan.Zero;
            this.Target = string.Empty;
            this.Type = string.Empty;
            this.Data = string.Empty;
            this.Properties = new Dictionary<string, string>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DependencyTelemetry"/> class with the given <paramref name="dependencyTypeName"/>, <paramref name="target"/>,
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
        /// Initializes a new instance of the <see cref="DependencyTelemetry"/> class with the given <paramref name="dependencyTypeName"/>, <paramref name="target"/>,
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
        /// Gets or sets date and time when telemetry was recorded.
        /// </summary>
        public override DateTimeOffset Timestamp { get; set; }

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
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the Result Code.
        /// </summary>
        public string ResultCode
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets resource name.
        /// </summary>
        public override string Name
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets data associated with the current dependency instance. Command name/statement for SQL dependency, URL for http dependency.
        /// </summary>
        public string Data
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets target of dependency call. SQL server name, url host, etc.
        /// </summary>
        public string Target
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the dependency type name.
        /// </summary>
        public string Type
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the amount of time it took the application to handle the request.
        /// </summary>
        public override TimeSpan Duration
        {
            get;
            set;
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
                    return this.success;
                }

                return null;
            }

            set
            {
                if (value != null && value.HasValue)
                {
                    this.success = value.Value;
                    this.successFieldSet = true;
                }
                else
                {
                    this.success = true;
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
            get;
        }
    }
}
