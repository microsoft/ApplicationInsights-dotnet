namespace Microsoft.ApplicationInsights.Extensibility.Implementation
{
    /// <summary>
    /// Encapsulates information about a user session.
    /// </summary>
    internal sealed class SessionContext
    {
        private string id;
        private bool? isFirst;

        internal SessionContext()
        {
        }

        /// <summary>
        /// Gets or sets the application-defined session ID.
        /// </summary>
        public string Id
        {
            get { return string.IsNullOrEmpty(this.id) ? null : this.id; }
            set { this.id = value; }
        }

        /// <summary>
        /// Gets or sets the IsFirst Session for the user.
        /// </summary>
        public bool? IsFirst
        {
            get { return this.isFirst; }
            set { this.isFirst = value; }
        }
    }
}
