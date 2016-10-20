namespace Microsoft.ApplicationInsights.Extensibility.Implementation
{
    using System;
    using System.Collections.Generic;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.External;

    /// <summary>
    /// Encapsulates information about a device where an application is running.
    /// </summary>
    public sealed class DeviceContext
    {
        private readonly IDictionary<string, string> tags;
        private readonly IDictionary<string, string> properties;

        internal DeviceContext(IDictionary<string, string> tags, IDictionary<string, string> properties)
        {
            this.tags = tags;
            this.properties = properties;
        }
        
        /// <summary>
        /// Gets or sets the type for the current device.
        /// </summary>
        public string Type
        {
            get { return this.tags.GetTagValueOrNull(ContextTagKeys.Keys.DeviceType); }
            set { this.tags.SetStringValueOrRemove(ContextTagKeys.Keys.DeviceType, value); }
        }

        /// <summary>
        /// Gets or sets a device unique ID.
        /// </summary>
        public string Id
        {
            get { return this.tags.GetTagValueOrNull(ContextTagKeys.Keys.DeviceId); }
            set { this.tags.SetStringValueOrRemove(ContextTagKeys.Keys.DeviceId, value); }
        }

        /// <summary>
        /// Gets or sets the operating system name.
        /// </summary>
        public string OperatingSystem
        {
            get { return this.tags.GetTagValueOrNull(ContextTagKeys.Keys.DeviceOSVersion); }
            set { this.tags.SetStringValueOrRemove(ContextTagKeys.Keys.DeviceOSVersion, value); }
        }

        /// <summary>
        /// Gets or sets the device OEM for the current device.
        /// </summary>
        public string OemName
        {
            get { return this.tags.GetTagValueOrNull(ContextTagKeys.Keys.DeviceOEMName); }
            set { this.tags.SetStringValueOrRemove(ContextTagKeys.Keys.DeviceOEMName, value); }
        }

        /// <summary>
        /// Gets or sets the device model for the current device.
        /// </summary>
        public string Model
        {
            get { return this.tags.GetTagValueOrNull(ContextTagKeys.Keys.DeviceModel); }
            set { this.tags.SetStringValueOrRemove(ContextTagKeys.Keys.DeviceModel, value); }
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
    }
}
