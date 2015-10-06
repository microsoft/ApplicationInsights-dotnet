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
        private readonly IDictionary<string, string> tags;

        internal DeviceContext(IDictionary<string, string> tags)
        {
            this.tags = tags;
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
        public string NetworkType
        {
            get { return this.tags.GetTagValueOrNull(ContextTagKeys.Keys.DeviceNetwork); }
            set { this.tags.SetTagValueOrRemove(ContextTagKeys.Keys.DeviceNetwork, value); }
        }

        /// <summary>
        /// Gets or sets the current application screen resolution.
        /// </summary>
        public string ScreenResolution
        {
            get { return this.tags.GetTagValueOrNull(ContextTagKeys.Keys.DeviceScreenResolution); }
            set { this.tags.SetStringValueOrRemove(ContextTagKeys.Keys.DeviceScreenResolution, value); }
        }

        /// <summary>
        /// Gets or sets the current display language of the operating system.
        /// </summary>
        public string Language
        {
            get { return this.tags.GetTagValueOrNull(ContextTagKeys.Keys.DeviceLanguage); }
            set { this.tags.SetStringValueOrRemove(ContextTagKeys.Keys.DeviceLanguage, value); }
        }

        /// <summary>
        /// Gets or sets the role name.
        /// </summary>
        [Obsolete("Use TelemetryContext.Cloud.RoleName")]
        public string RoleName
        {
            get { return this.tags.GetTagValueOrNull(ContextTagKeys.Keys.DeviceRoleName); }
            set { this.tags.SetStringValueOrRemove(ContextTagKeys.Keys.DeviceRoleName, value); }
        }

        /// <summary>
        /// Gets or sets the role instance.
        /// </summary>
        [Obsolete("Use TelemetryContext.Cloud.RoleInstance")]
        public string RoleInstance
        {
            get { return this.tags.GetTagValueOrNull(ContextTagKeys.Keys.DeviceRoleInstance); }
            set { this.tags.SetStringValueOrRemove(ContextTagKeys.Keys.DeviceRoleInstance, value); }
        }
    }
}
