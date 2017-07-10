namespace Microsoft.ApplicationInsights.Extensibility.Implementation
{
    using System;
    using System.Collections.Generic;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.External;

    /// <summary>
    /// Encapsulates information about a user using an application.
    /// </summary>
    public sealed class UserContext
    {
        private string id;
        private string accountId;
        private string userAgent;
        private string authenticatedUserId;

        internal UserContext()
        {
        }

        /// <summary>
        /// Gets or sets the ID of user accessing the application.
        /// </summary>
        /// <remarks>
        /// Unique user ID is automatically generated in default Application Insights configuration.
        /// </remarks>
        public string Id
        {
            get { return this.id == string.Empty ? null : this.id; }
            set { this.id = value; }
        }

        /// <summary>
        /// Gets or sets the ID of an application-defined account associated with the user.
        /// </summary>
        public string AccountId
        {
            get { return this.accountId== string.Empty ? null : this.accountId; }
            set { this.accountId = value; }
        }

        /// <summary>
        /// Gets or sets the UserAgent of an application-defined account associated with the user.
        /// </summary>
        public string UserAgent
        {
            get { return this.userAgent == string.Empty ? null : this.userAgent; }
            set { this.userAgent = value; }
        }

        /// <summary>
        /// Gets or sets the authenticated user id.
        /// Authenticated user id should be a persistent string that uniquely represents each authenticated user in the application or service.
        /// </summary>
        public string AuthenticatedUserId
        {
            get { return this.authenticatedUserId == string.Empty ? null : this.authenticatedUserId; }
            set { this.authenticatedUserId = value; }
        }

        internal void UpdateTags(IDictionary<string, string> tags)
        {
            tags.UpdateTagValue(ContextTagKeys.Keys.UserId, this.Id);
            tags.UpdateTagValue(ContextTagKeys.Keys.UserAccountId, this.AccountId);
            tags.UpdateTagValue(ContextTagKeys.Keys.UserAgent, this.UserAgent);
            tags.UpdateTagValue(ContextTagKeys.Keys.UserAuthUserId, this.AuthenticatedUserId);
        }

        internal void CopyFrom(TelemetryContext telemetryContext)
        {
            var source = telemetryContext.User;
            Tags.CopyTagValue(source.Id, ref this.id);
            Tags.CopyTagValue(source.AccountId, ref this.accountId);
            Tags.CopyTagValue(source.UserAgent, ref this.userAgent);
            Tags.CopyTagValue(source.AuthenticatedUserId, ref this.authenticatedUserId);
        }
    }
}
