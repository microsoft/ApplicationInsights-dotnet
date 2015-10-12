namespace Microsoft.ApplicationInsights.Extensibility.Implementation
{
    using System;

    /// <summary>
    /// Operation class that holds operation id and operation name for the current call context.
    /// </summary>
    [Serializable]
    internal class OperationContextForCallContext : MarshalByRefObject
    {
        /// <summary>
        /// Operation id that will be assigned to all the child telemetry items.
        /// Parent Operation id that will be assigned to all the child telemetry items.
        /// </summary>
        public string ParentOperationId;

        /// <summary>
        /// Root Operation id that will be assigned to all the child telemetry items.
        /// </summary>
        public string RootOperationId;

        /// <summary>
        /// Operation name that will be assigned to all the child telemetry items.
        /// </summary>
        public string OperationName;
    }
}
