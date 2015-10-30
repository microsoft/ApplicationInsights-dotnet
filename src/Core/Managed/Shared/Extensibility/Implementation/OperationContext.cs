namespace Microsoft.ApplicationInsights.Extensibility.Implementation
{
    using System.Collections.Generic;
    using System.ComponentModel;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.External;

    /// <summary>
    /// Encapsulates information about an operation. Operation normally reflects an end to end scenario that starts from a user action (e.g. button click).  
    /// </summary>
    public sealed class OperationContext
    {
        private readonly IDictionary<string, string> tags;

        internal OperationContext(IDictionary<string, string> tags)
        {
            this.tags = tags;
        }

        /// <summary>
        /// Gets or sets the application-defined operation ID for the topmost operation.
        /// </summary>
        public string Id
        {
            get { return this.tags.GetTagValueOrNull(ContextTagKeys.Keys.OperationId); }
            set { this.tags.SetStringValueOrRemove(ContextTagKeys.Keys.OperationId, value); }
        }

        /// <summary>
        /// Gets or sets the parent operation ID.
        /// </summary>
        public string ParentId
        {
            get { return this.tags.GetTagValueOrNull(ContextTagKeys.Keys.OperationParentId); }
            set { this.tags.SetStringValueOrRemove(ContextTagKeys.Keys.OperationParentId, value); }
        }

        /// <summary>
        /// Gets or sets the correlation vector for the current telemetry item.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public string CorrelationVector
        {
            get { return this.tags.GetTagValueOrNull(ContextTagKeys.Keys.OperationCorrelationVector); }
            set { this.tags.SetStringValueOrRemove(ContextTagKeys.Keys.OperationCorrelationVector, value); }
        }

        /// <summary>
        /// Gets or sets the application-defined topmost operation's name.
        /// </summary>
        public string Name
        {
            get { return this.tags.GetTagValueOrNull(ContextTagKeys.Keys.OperationName); }
            set { this.tags.SetStringValueOrRemove(ContextTagKeys.Keys.OperationName, value); }
        }

        /// <summary>
        /// Gets or sets the application-defined operation SyntheticSource.
        /// </summary>
        public string SyntheticSource
        {
            get { return this.tags.GetTagValueOrNull(ContextTagKeys.Keys.OperationSyntheticSource); }
            set { this.tags.SetStringValueOrRemove(ContextTagKeys.Keys.OperationSyntheticSource, value); }
        }
    }
}
