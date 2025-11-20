namespace Microsoft.ApplicationInsights.Extensibility.Implementation
{
    using System.Collections.Generic;

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
            get { return string.IsNullOrEmpty(this.id) ? null : this.id; }
            set { this.id = value; }
        }

        /// <summary>
        /// Gets or sets the ID of an application-defined account associated with the user.
        /// </summary>
        internal string AccountId
        {
            get { return string.IsNullOrEmpty(this.accountId) ? null : this.accountId; }
            set { this.accountId = value; }
        }

        /// <summary>
        /// Gets or sets the UserAgent of an application-defined account associated with the user.
        /// </summary>
        public string UserAgent
        {
            get { return string.IsNullOrEmpty(this.userAgent) ? null : this.userAgent; }
            set { this.userAgent = value; }
        }

        /// <summary>
        /// Gets or sets the authenticated user id.
        /// Authenticated user id should be a persistent string that uniquely represents each authenticated user in the application or service.
        /// </summary>
        public string AuthenticatedUserId
        {
            get { return string.IsNullOrEmpty(this.authenticatedUserId) ? null : this.authenticatedUserId; }
            set { this.authenticatedUserId = value; }
        }
    }
}
