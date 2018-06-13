namespace Microsoft.ApplicationInsights.DataContracts
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;

    /// <summary>
    /// Represents a context for sending telemetry to the Application Insights service.
    /// <a href="https://go.microsoft.com/fwlink/?linkid=525722#telemetrycontext">Learn more</a>
    /// </summary>
    public sealed class TelemetryContext
    {
        private readonly IDictionary<string, string> properties;
        private readonly IDictionary<string, string> globalProperties;

        private string instrumentationKey;

        private ComponentContext component;
        private DeviceContext device;
        private CloudContext cloud;
        private SessionContext session;
        private UserContext user;
        private OperationContext operation;
        private LocationContext location;
        private InternalContext internalContext = new InternalContext();

        /// <summary>
        /// Initializes a new instance of the <see cref="TelemetryContext"/> class.
        /// </summary>
        public TelemetryContext()
            : this(new ConcurrentDictionary<string, string>(), new ConcurrentDictionary<string, string>())
        {
        }

        internal TelemetryContext(IDictionary<string, string> properties)
            : this(properties, new ConcurrentDictionary<string, string>())
        {            
        }

        internal TelemetryContext(IDictionary<string, string> properties, IDictionary<string, string> globalProperties)
        {
            Debug.Assert(properties != null, nameof(properties));
            Debug.Assert(globalProperties != null, nameof(globalProperties));
            this.properties = properties;
            this.globalProperties = globalProperties;
        }

        /// <summary>
        /// Gets or sets the default instrumentation key for all <see cref="ITelemetry"/> objects logged in this <see cref="TelemetryContext"/>.
        /// </summary>
        /// <remarks>
        /// By default, this property is initialized with the <see cref="TelemetryConfiguration.InstrumentationKey"/> value
        /// of the <see cref="TelemetryConfiguration.Active"/> instance of <see cref="TelemetryConfiguration"/>. You can specify it
        /// for all telemetry tracked via a particular <see cref="TelemetryClient"/> or for a specific <see cref="ITelemetry"/>
        /// instance.
        /// </remarks>
        public string InstrumentationKey
        {
            get { return this.instrumentationKey ?? string.Empty; }
            set { Property.Set(ref this.instrumentationKey, value); }
        }

        /// <summary>
        /// Gets the object describing the component tracked by this <see cref="TelemetryContext"/>.
        /// </summary>
        public ComponentContext Component
        {
            get { return LazyInitializer.EnsureInitialized(ref this.component, () => new ComponentContext()); }
        }

        /// <summary>
        /// Gets the object describing the device tracked by this <see cref="TelemetryContext"/>.
        /// </summary>
        public DeviceContext Device
        {
#pragma warning disable CS0618 // Type or member is obsolete
            get { return LazyInitializer.EnsureInitialized(ref this.device, () => new DeviceContext(this.Properties)); }
#pragma warning restore CS0618 // Type or member is obsolete
        }

        /// <summary>
        /// Gets the object describing the cloud tracked by this <see cref="TelemetryContext"/>.
        /// </summary>
        public CloudContext Cloud
        {
            get { return LazyInitializer.EnsureInitialized(ref this.cloud, () => new CloudContext()); }
        }

        /// <summary>
        /// Gets the object describing a user session tracked by this <see cref="TelemetryContext"/>.
        /// </summary>
        public SessionContext Session
        {
            get { return LazyInitializer.EnsureInitialized(ref this.session, () => new SessionContext()); }
        }

        /// <summary>
        /// Gets the object describing a user tracked by this <see cref="TelemetryContext"/>.
        /// </summary>
        public UserContext User
        {
            get { return LazyInitializer.EnsureInitialized(ref this.user, () => new UserContext()); }
        }

        /// <summary>
        /// Gets the object describing a operation tracked by this <see cref="TelemetryContext"/>.
        /// <a href="https://go.microsoft.com/fwlink/?linkid=525722#operationcontext">Learn more</a>
        /// </summary>
        public OperationContext Operation
        {
            get { return LazyInitializer.EnsureInitialized(ref this.operation, () => new OperationContext()); }
        }

        /// <summary>
        /// Gets the object describing a location tracked by this <see cref="TelemetryContext" />.
        /// </summary>
        public LocationContext Location
        {
            get { return LazyInitializer.EnsureInitialized(ref this.location, () => new LocationContext()); }
        }

        /// <summary>
        /// Gets a dictionary of application-defined property values.
        /// <a href="https://go.microsoft.com/fwlink/?linkid=525722#properties">Learn more</a>
        /// </summary>
        [Obsolete("Use GlobalProperties to set global level properties. For properties at item level, use ISupportProperties.Properties.")]
        public IDictionary<string, string> Properties
        {
            get { return this.properties; }
        }

        /// <summary>
        /// Gets a dictionary of application-defined property values which are global in scope.
        /// Future SDK versions could serialize this separately from the item level properties.
        /// <a href="https://go.microsoft.com/fwlink/?linkid=525722#properties">Learn more</a>
        /// </summary>
        public IDictionary<string, string> GlobalProperties
        {
            get { return this.globalProperties; }
        }

        internal InternalContext Internal => this.internalContext;

        /// <summary>
        /// Gets a dictionary of context tags.
        /// </summary>
        internal IDictionary<string, string> SanitizedTags
        {
            get
            {
                var result = new Dictionary<string, string>();
                this.component?.UpdateTags(result);
                this.device?.UpdateTags(result);
                this.cloud?.UpdateTags(result);
                this.session?.UpdateTags(result);
                this.user?.UpdateTags(result);
                this.operation?.UpdateTags(result);
                this.location?.UpdateTags(result);
                this.Internal.UpdateTags(result);
                return result;
            }
        }

        internal void SanitizeGlobalProperties()
        {
           this.globalProperties.SanitizeProperties();
        }

        internal TelemetryContext DeepClone(IDictionary<string, string> properties)
        {
            Debug.Assert(properties != null, "properties parameter should not be null");
            var other = new TelemetryContext(properties);
            Utils.CopyDictionary(this.globalProperties, other.globalProperties);
            other.InstrumentationKey = this.InstrumentationKey;
            return other;
        }

        /// <summary>
        /// Initialize this instance's Context properties with the values from another TelemetryContext.
        /// First check that source is not null, then copy to this instance.
        /// Note that invoking the public getter instead of the private field will call the LazyInitializer.
        /// </summary>
        internal void Initialize(TelemetryContext source, string instrumentationKey)
        {
            Property.Initialize(ref this.instrumentationKey, instrumentationKey);

            source.component?.CopyTo(this.Component);
            source.device?.CopyTo(this.Device);
            source.cloud?.CopyTo(this.Cloud);
            source.session?.CopyTo(this.Session);
            source.user?.CopyTo(this.User);
            source.operation?.CopyTo(this.Operation);
            source.location?.CopyTo(this.Location);
            source.Internal.CopyTo(this.Internal);
        }
    }
}