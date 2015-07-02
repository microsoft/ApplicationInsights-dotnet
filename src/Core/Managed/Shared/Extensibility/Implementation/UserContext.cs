namespace Microsoft.ApplicationInsights.Extensibility.Implementation
{
    using System;
    using System.Collections.Generic;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.External;

    /// <summary>
    /// Encapsulates information about a user using an application.
    /// </summary>
    public sealed class UserContext : IJsonSerializable
    {
        private readonly IDictionary<string, string> tags;

        internal UserContext(IDictionary<string, string> tags)
        {
            this.tags = tags;
        }

        /// <summary>
        /// Gets or sets the ID of user accessing the application.
        /// </summary>
        /// <remarks>
        /// Unique user ID is automatically generated in default Application Insights configuration. 
        /// </remarks>
        public string Id 
        {
            get { return this.tags.GetTagValueOrNull(ContextTagKeys.Keys.UserId); }
            set { this.tags.SetStringValueOrRemove(ContextTagKeys.Keys.UserId, value); }
        }

        /// <summary>
        /// Gets or sets the ID of an application-defined account associated with the user.
        /// </summary>
        public string AccountId
        {
            get { return this.tags.GetTagValueOrNull(ContextTagKeys.Keys.UserAccountId); }
            set { this.tags.SetStringValueOrRemove(ContextTagKeys.Keys.UserAccountId, value); }
        }

        /// <summary>
        /// Gets or sets the UserAgent of an application-defined account associated with the user.
        /// </summary>
        public string UserAgent
        {
            get { return this.tags.GetTagValueOrNull(ContextTagKeys.Keys.UserAgent); }
            set { this.tags.SetStringValueOrRemove(ContextTagKeys.Keys.UserAgent, value); }
        }
        
        /// <summary>
        /// Gets or sets the date when the user accessed the application for the first time.
        /// </summary>
        /// <remarks>
        /// Acquisition date is automatically supplied in default Application Insights configuration.
        /// </remarks>
        public DateTimeOffset? AcquisitionDate 
        {
            get { return this.tags.GetTagDateTimeOffsetValueOrNull(ContextTagKeys.Keys.UserAccountAcquisitionDate); }
            set { this.tags.SetDateTimeOffsetValueOrRemove(ContextTagKeys.Keys.UserAccountAcquisitionDate, value); }
        }

        /// <summary>
        /// Gets or sets the store region of an application-defined account associated with the user.
        /// </summary>
        public string StoreRegion
        {
            get { return this.tags.GetTagValueOrNull(ContextTagKeys.Keys.UserStoreRegion); }
            set { this.tags.SetStringValueOrRemove(ContextTagKeys.Keys.UserStoreRegion, value); }
        }

        void IJsonSerializable.Serialize(IJsonWriter writer)
        {
            writer.WriteStartObject();
            writer.WriteProperty("id", this.Id);
            writer.WriteProperty("userAgent", this.UserAgent);
            writer.WriteProperty("accountId", this.AccountId);
            writer.WriteProperty("anonUserAcquisitionDate", this.AcquisitionDate);
            writer.WriteProperty("storeRegion", this.StoreRegion);
            writer.WriteEndObject();
        }      
    }
}
