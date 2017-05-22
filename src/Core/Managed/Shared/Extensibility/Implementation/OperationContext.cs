namespace Microsoft.ApplicationInsights.Extensibility.Implementation
{
    using System.Collections.Generic;
    using System.ComponentModel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.External;

    /// <summary>
    /// Encapsulates information about an operation. Operation normally reflects an end to end scenario that starts from a user action (e.g. button click).
    /// </summary>
    public sealed class OperationContext
    {
        internal OperationContext()
        {
        }

        /// <summary>
        /// Gets or sets the application-defined operation ID for the topmost operation.
        /// </summary>
        public string Id
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the parent operation ID.
        /// </summary>
        public string ParentId
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the correlation vector for the current telemetry item.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public string CorrelationVector
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the application-defined topmost operation's name.
        /// </summary>
        public string Name
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the application-defined operation SyntheticSource.
        /// </summary>
        public string SyntheticSource
        {
            get;
            set;
        }

        internal void UpdateTags(IDictionary<string, string> tags)
        {
            tags.UpdateTagValue(ContextTagKeys.Keys.OperationId, this.Id);
            tags.UpdateTagValue(ContextTagKeys.Keys.OperationParentId, this.ParentId);
            tags.UpdateTagValue(ContextTagKeys.Keys.OperationCorrelationVector, this.CorrelationVector);
            tags.UpdateTagValue(ContextTagKeys.Keys.OperationName, this.Name);
            tags.UpdateTagValue(ContextTagKeys.Keys.OperationSyntheticSource, this.SyntheticSource);
        }

        internal void CopyTo(TelemetryContext telemetryContext)
        {
            var target = telemetryContext.Operation;
            target.Id = Tags.CopyTagValue(target.Id, this.Id);
            target.ParentId = Tags.CopyTagValue(target.ParentId, this.ParentId);
            target.CorrelationVector = Tags.CopyTagValue(target.CorrelationVector, this.CorrelationVector);
            target.Name = Tags.CopyTagValue(target.Name, this.Name);
            target.SyntheticSource = Tags.CopyTagValue(target.SyntheticSource, this.SyntheticSource);
        }
    }
}