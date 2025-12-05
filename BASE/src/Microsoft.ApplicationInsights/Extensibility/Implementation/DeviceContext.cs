namespace Microsoft.ApplicationInsights.Extensibility.Implementation
{
    using System.Collections.Generic;

    /// <summary>
    /// Encapsulates information about a device where an application is running.
    /// </summary>
    internal sealed class DeviceContext
    {
        private readonly IDictionary<string, string> properties;

        private string type;
        private string id;
        private string operatingSystem;
        private string oemName;
        private string model;

        internal DeviceContext(IDictionary<string, string> properties)
        {
            this.properties = properties;
        }

        /// <summary>
        /// Gets or sets the type for the current device.
        /// </summary>
        public string Type
        {
            get { return string.IsNullOrEmpty(this.type) ? null : this.type; }
            set { this.type = value; }
        }

        /// <summary>
        /// Gets or sets a device unique ID.
        /// </summary>
        public string Id
        {
            get { return string.IsNullOrEmpty(this.id) ? null : this.id; }
            set { this.id = value; }
        }

        /// <summary>
        /// Gets or sets the operating system name.
        /// </summary>
        public string OperatingSystem
        {
            get { return string.IsNullOrEmpty(this.operatingSystem) ? null : this.operatingSystem; }
            set { this.operatingSystem = value; }
        }

        /// <summary>
        /// Gets or sets the device OEM for the current device.
        /// </summary>
        public string OemName
        {
            get { return string.IsNullOrEmpty(this.oemName) ? null : this.oemName; }
            set { this.oemName = value; }
        }

        /// <summary>
        /// Gets or sets the device model for the current device.
        /// </summary>
        public string Model
        {
            get { return string.IsNullOrEmpty(this.model) ? null : this.model; }
            set { this.model = value; }
        }
    }
}
