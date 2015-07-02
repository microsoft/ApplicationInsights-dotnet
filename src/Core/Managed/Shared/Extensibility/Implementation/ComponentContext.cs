namespace Microsoft.ApplicationInsights.Extensibility.Implementation
{
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
    public sealed class ComponentContext : IJsonSerializable
    {
        private readonly IDictionary<string, string> tags;

        internal ComponentContext(IDictionary<string, string> tags)
        {
            this.tags = tags;
        }

        /// <summary>
        /// Gets or sets the application version.
        /// </summary>
        public string Version
        {
            get { return this.tags.GetTagValueOrNull(ContextTagKeys.Keys.ApplicationVersion); }
            set { this.tags.SetStringValueOrRemove(ContextTagKeys.Keys.ApplicationVersion, value); }
        }

        void IJsonSerializable.Serialize(IJsonWriter writer)
        {
            writer.WriteStartObject();
            writer.WriteProperty("version", this.Version);
            writer.WriteEndObject();
        }
    }
}
