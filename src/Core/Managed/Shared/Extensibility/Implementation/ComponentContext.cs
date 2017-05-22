namespace Microsoft.ApplicationInsights.Extensibility.Implementation
{
    using System;
    using System.Collections.Generic;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.External;

    /// <summary>
    /// Encapsulates information describing an Application Insights component.
    /// </summary>
    /// <remarks>
    /// This class matches the "Application" schema concept. We are intentionally calling it "Component" for consistency
    /// with terminology used by our portal and services and to encourage standardization of terminology within our
    /// organization. Once a consensus is reached, we will change type and property names to match.
    /// </remarks>
    public sealed class ComponentContext
    {
        internal ComponentContext()
        {
        }

        /// <summary>
        /// Gets or sets the application version.
        /// </summary>
        public string Version { get; set; }

        internal void UpdateTags(IDictionary<string, string> tags)
        {
            tags.UpdateTagValue(ContextTagKeys.Keys.ApplicationVersion, this.Version);
        }

        internal void CopyTo(TelemetryContext telemetryContext)
        {
            var target = telemetryContext.Component;
            target.Version = Tags.CopyTagValue(target.Version, this.Version);
        }
    }
}
