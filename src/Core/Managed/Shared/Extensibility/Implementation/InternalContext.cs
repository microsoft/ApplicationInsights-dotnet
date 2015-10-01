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
        private readonly IDictionary<string, string> tags;

        internal InternalContext(IDictionary<string, string> tags)
        {
            this.tags = tags;
        }

        /// <summary>
        /// Gets or sets application insights SDK version.
        /// </summary>
        public string SdkVersion
        {
            get { return this.tags.GetTagValueOrNull(ContextTagKeys.Keys.InternalSdkVersion); }
            set { this.tags.SetStringValueOrRemove(ContextTagKeys.Keys.InternalSdkVersion, value); }
        }

        /// <summary>
        /// Gets or sets application insights agent version.
        /// </summary>
        public string AgentVersion
        {
            get { return this.tags.GetTagValueOrNull(ContextTagKeys.Keys.InternalAgentVersion); }
            set { this.tags.SetStringValueOrRemove(ContextTagKeys.Keys.InternalAgentVersion, value); }
        }
    }
}
