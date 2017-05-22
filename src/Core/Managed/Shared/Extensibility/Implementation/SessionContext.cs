namespace Microsoft.ApplicationInsights.Extensibility.Implementation
{
    using System.Collections.Generic;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.External;

    /// <summary>
    /// Encapsulates information about a user session.
    /// </summary>
    public sealed class SessionContext
    {
        internal SessionContext()
        {
        }

        /// <summary>
        /// Gets or sets the application-defined session ID.
        /// </summary>
        public string Id
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the IsFirst Session for the user.
        /// </summary>
        public bool? IsFirst
        {
            get;
            set;
        }

        internal void UpdateTags(IDictionary<string, string> tags)
        {
            tags.UpdateTagValue(ContextTagKeys.Keys.SessionId, this.Id);
            tags.UpdateTagValue(ContextTagKeys.Keys.SessionIsFirst, this.IsFirst);
        }

        internal void CopyTo(TelemetryContext telemetryContext)
        {
            var target = telemetryContext.Session;
            target.Id = Tags.CopyTagValue(target.Id, this.Id);
            target.IsFirst = Tags.CopyTagValue(target.IsFirst, this.IsFirst);
        }
    }
}
