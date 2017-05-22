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
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the ID of an application-defined account associated with the user.
        /// </summary>
        public string AccountId
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the UserAgent of an application-defined account associated with the user.
        /// </summary>
        public string UserAgent
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the authenticated user id.
        /// Authenticated user id should be a persistent string that uniquely represents each authenticated user in the application or service.
        /// </summary>
        public string AuthenticatedUserId
        {
            get;
            set;
        }

        internal void UpdateTags(IDictionary<string, string> tags)
        {
            tags.UpdateTagValue(ContextTagKeys.Keys.UserId, this.Id);
            tags.UpdateTagValue(ContextTagKeys.Keys.UserAccountId, this.AccountId);
            tags.UpdateTagValue(ContextTagKeys.Keys.UserAgent, this.UserAgent);
            tags.UpdateTagValue(ContextTagKeys.Keys.UserAuthUserId, this.AuthenticatedUserId);
        }

        internal void CopyTo(TelemetryContext telemetryContext)
        {
            var target = telemetryContext.User;
            target.Id = Tags.CopyTagValue(target.Id, this.Id);
            target.AccountId = Tags.CopyTagValue(target.AccountId, this.AccountId);
            target.UserAgent = Tags.CopyTagValue(target.UserAgent, this.UserAgent);
            target.AuthenticatedUserId = Tags.CopyTagValue(target.AuthenticatedUserId, this.AuthenticatedUserId);
        }
    }
}
