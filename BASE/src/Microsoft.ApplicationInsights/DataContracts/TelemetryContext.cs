namespace Microsoft.ApplicationInsights.DataContracts
{
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Threading;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;

    /// <summary>
    /// Represents a context for sending telemetry to the Application Insights service.
    /// <a href="https://go.microsoft.com/fwlink/?linkid=525722#telemetrycontext">Learn more</a>
    /// </summary>
    public sealed class TelemetryContext
    {
        internal IDictionary<string, string> GlobalPropertiesValue;
        internal IDictionary<string, string> PropertiesValue;

        private ComponentContext component;
        private DeviceContext device;
        private CloudContext cloud;
        private SessionContext session;
        private UserContext user;
        private OperationContext operation;
        private LocationContext location;

        /// <summary>
        /// Initializes a new instance of the <see cref="TelemetryContext"/> class.
        /// </summary>
        public TelemetryContext()
            : this(null, null)
        {
        }

        internal TelemetryContext(IDictionary<string, string> properties)
            : this(properties, null)
        {
        }

        internal TelemetryContext(IDictionary<string, string> properties, IDictionary<string, string> globalProperties)
        {
            this.PropertiesValue = properties;
            this.GlobalPropertiesValue = globalProperties;
        }

        /// <summary>
        /// Gets a dictionary of application-defined property values which are global in scope.
        /// Future SDK versions could serialize this separately from the item level properties.
        /// <a href="https://go.microsoft.com/fwlink/?linkid=525722#properties">Learn more</a>
        /// </summary>
        public IDictionary<string, string> GlobalProperties
        {
            get { return LazyInitializer.EnsureInitialized(ref this.GlobalPropertiesValue, () => new ConcurrentDictionary<string, string>()); }
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
        /// Gets the object describing the cloud tracked by this <see cref="TelemetryContext"/>.
        /// </summary>
        internal CloudContext Cloud
        {
            get { return LazyInitializer.EnsureInitialized(ref this.cloud, () => new CloudContext()); }
        }

        /// <summary>
        /// Gets the object describing the component tracked by this <see cref="TelemetryContext"/>.
        /// </summary>
        internal ComponentContext Component
        {
            get { return LazyInitializer.EnsureInitialized(ref this.component, () => new ComponentContext()); }
        }

        /// <summary>
        /// Gets the object describing the device tracked by this <see cref="TelemetryContext"/>.
        /// </summary>
        internal DeviceContext Device
        {
#pragma warning disable CS0618 // Type or member is obsolete
            get { return LazyInitializer.EnsureInitialized(ref this.device, () => new DeviceContext(default)); }
#pragma warning restore CS0618 // Type or member is obsolete
        }

        /// <summary>
        /// Gets the object describing a user session tracked by this <see cref="TelemetryContext"/>.
        /// </summary>
        internal SessionContext Session
        {
            get { return LazyInitializer.EnsureInitialized(ref this.session, () => new SessionContext()); }
        }
    }
}