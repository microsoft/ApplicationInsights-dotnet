namespace Microsoft.ApplicationInsights.Extensibility.Implementation
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.External;

    /// <summary>
    /// Encapsulates Internal information.
    /// </summary>
    public sealed class InternalContext
    {
        internal InternalContext()
        {
        }

        /// <summary>
        /// Gets or sets application insights SDK version.
        /// </summary>
        public string SdkVersion
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets application insights agent version.
        /// </summary>
        public string AgentVersion
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets node name for the billing purposes. Use this filed to override the standard way node names got detected.
        /// </summary>
        public string NodeName
        {
            get;
            set;
        }

        internal void UpdateTags(IDictionary<string, string> tags)
        {
            tags.UpdateTagValue(ContextTagKeys.Keys.InternalSdkVersion, this.SdkVersion);
            tags.UpdateTagValue(ContextTagKeys.Keys.InternalAgentVersion, this.AgentVersion);
            tags.UpdateTagValue(ContextTagKeys.Keys.InternalNodeName, this.NodeName);
        }

        internal void CopyTo(TelemetryContext telemetryContext)
        {
            var target = telemetryContext.Internal;
            target.SdkVersion = Tags.CopyTagValue(target.SdkVersion, this.SdkVersion);
            target.AgentVersion = Tags.CopyTagValue(target.AgentVersion, this.AgentVersion);
            target.NodeName = Tags.CopyTagValue(target.NodeName, this.NodeName);
        }
    }
}
