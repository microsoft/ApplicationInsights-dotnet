namespace Microsoft.ApplicationInsights.Extensibility.Implementation
{
    using System.Collections.Generic;
    using System.ComponentModel;

    /// <summary>
    /// Encapsulates information about an operation. Operation normally reflects an end to end scenario that starts from a user action (e.g. button click).
    /// </summary>
    public sealed class OperationContext
    {
        private string id;
        private string parentId;
        private string correlationVector;
        private string syntheticSource;
        private string name;

        internal OperationContext()
        {
        }

        /// <summary>
        /// Gets or sets the application-defined topmost operation's name.
        /// </summary>
        public string Name
        {
            get { return string.IsNullOrEmpty(this.name) ? null : this.name; }
            set { this.name = value; }
        }

        /// <summary>
        /// Gets or sets the application-defined operation SyntheticSource.
        /// </summary>
        public string SyntheticSource
        {
            get { return string.IsNullOrEmpty(this.syntheticSource) ? null : this.syntheticSource; }
            set { this.syntheticSource = value; }
        }

        /// <summary>
        /// Gets or sets the application-defined operation ID for the topmost operation.
        /// </summary>
        internal string Id
        {
            get { return string.IsNullOrEmpty(this.id) ? null : this.id; }
            set { this.id = value; }
        }

        /// <summary>
        /// Gets or sets the parent operation ID.
        /// </summary>
        internal string ParentId
        {
            get { return string.IsNullOrEmpty(this.parentId) ? null : this.parentId; }
            set { this.parentId = value; }
        }

        /// <summary>
        /// Gets or sets the correlation vector for the current telemetry item.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        internal string CorrelationVector
        {
            get { return string.IsNullOrEmpty(this.correlationVector) ? null : this.correlationVector; }
            set { this.correlationVector = value; }
        }
    }
}