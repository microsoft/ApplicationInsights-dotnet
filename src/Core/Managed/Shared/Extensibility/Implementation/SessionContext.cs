namespace Microsoft.ApplicationInsights.Extensibility.Implementation
{
    using System.Collections.Generic;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.External;

    /// <summary>
    /// Encapsulates information about a user session.
    /// </summary>
    public sealed class SessionContext : IJsonSerializable
    {
        private readonly IDictionary<string, string> tags;

        internal SessionContext(IDictionary<string, string> tags)
        {
            this.tags = tags;
        }

        /// <summary>
        /// Gets or sets the application-defined session ID.
        /// </summary>
        public string Id
        {
            get { return this.tags.GetTagValueOrNull(ContextTagKeys.Keys.SessionId); }
            set { this.tags.SetStringValueOrRemove(ContextTagKeys.Keys.SessionId, value); }
        }

        /// <summary>
        /// Gets or sets the IsFirst Session for the user.
        /// </summary>
        public bool? IsFirst 
        {
            get { return this.tags.GetTagBoolValueOrNull(ContextTagKeys.Keys.SessionIsFirst); }
            set { this.tags.SetTagValueOrRemove<bool?>(ContextTagKeys.Keys.SessionIsFirst, value); }
        }

        void IJsonSerializable.Serialize(IJsonWriter writer)
        {
            writer.WriteStartObject();
            writer.WriteProperty("id", this.Id);
            writer.WriteProperty("firstSession", this.IsFirst);
            writer.WriteEndObject();
        }
    }
}
