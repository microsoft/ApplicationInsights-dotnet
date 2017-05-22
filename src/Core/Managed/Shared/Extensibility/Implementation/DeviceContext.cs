namespace Microsoft.ApplicationInsights.Extensibility.Implementation
{
    using System;
    using System.Collections.Generic;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.External;

    /// <summary>
    /// Encapsulates information about a device where an application is running.
    /// </summary>
    public sealed class DeviceContext
    {
        private readonly IDictionary<string, string> properties;

        internal DeviceContext(IDictionary<string, string> properties)
        {
            this.properties = properties;
        }

        /// <summary>
        /// Gets or sets the type for the current device.
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Gets or sets a device unique ID.
        /// </summary>
        public string Id
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the operating system name.
        /// </summary>
        public string OperatingSystem
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the device OEM for the current device.
        /// </summary>
        public string OemName
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the device model for the current device.
        /// </summary>
        public string Model
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the <a href="http://www.iana.org/assignments/ianaiftype-mib/ianaiftype-mib">IANA interface type</a>
        /// for the internet connected network adapter.
        /// </summary>
        [Obsolete("Use custom properties.")]
        public string NetworkType
        {
            get { return this.properties.GetTagValueOrNull("ai.device.network"); }
            set { this.properties.SetTagValueOrRemove("ai.device.network", value); }
        }

        /// <summary>
        /// Gets or sets the current application screen resolution.
        /// </summary>
        [Obsolete("Use custom properties.")]
        public string ScreenResolution
        {
            get { return this.properties.GetTagValueOrNull("ai.device.screenResolution"); }
            set { this.properties.SetStringValueOrRemove("ai.device.screenResolution", value); }
        }

        /// <summary>
        /// Gets or sets the current display language of the operating system.
        /// </summary>
        [Obsolete("Use custom properties.")]
        public string Language
        {
            get { return this.properties.GetTagValueOrNull("ai.device.language"); }
            set { this.properties.SetStringValueOrRemove("ai.device.language", value); }
        }

        internal void UpdateTags(IDictionary<string, string> tags)
        {
            tags.UpdateTagValue(ContextTagKeys.Keys.DeviceType, this.Type);
            tags.UpdateTagValue(ContextTagKeys.Keys.DeviceId, this.Id);
            tags.UpdateTagValue(ContextTagKeys.Keys.DeviceOSVersion, this.OperatingSystem);
            tags.UpdateTagValue(ContextTagKeys.Keys.DeviceOEMName, this.OemName);
            tags.UpdateTagValue(ContextTagKeys.Keys.DeviceModel, this.Model);
        }

        internal void CopyTo(TelemetryContext telemetryContext)
        {
            var target = telemetryContext.Device;
            target.Type = Tags.CopyTagValue(target.Type, this.Type);
            target.Id = Tags.CopyTagValue(target.Id, this.Id);
            target.OperatingSystem = Tags.CopyTagValue(target.OperatingSystem, this.OperatingSystem);
            target.OemName = Tags.CopyTagValue(target.OemName, this.OemName);
            target.Model = Tags.CopyTagValue(target.Model, this.Model);
        }
    }
}
