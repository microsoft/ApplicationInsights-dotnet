namespace Microsoft.ApplicationInsights.Extensibility.Implementation
{
    using System.Runtime.Remoting.Messaging;

    internal static class CallContextHelpers
    {
        /// <summary>
        /// Name of the operation context store item present in the context.
        /// </summary>
        internal const string OperationContextSlotName = "Microsoft.ApplicationInsights.Operation.OperationContextStore";

        /// <summary>
        /// Saves the context store to the call context.
        /// </summary>
        /// <param name="operationContext">Operation context store instance.</param>
        internal static void SaveOperationContextToCallContext(OperationContextForCallContext operationContext)
        {
            CallContext.FreeNamedDataSlot(OperationContextSlotName);
            CallContext.LogicalSetData(OperationContextSlotName, operationContext);
        }

        /// <summary>
        /// Returns the current operation context store present in the call context.
        /// </summary>
        internal static OperationContextForCallContext GetCurrentOperationContextFromCallContext()
        { 
            return CallContext.LogicalGetData(OperationContextSlotName) as OperationContextForCallContext;
        }

        /// <summary>
        /// Clears the call context and restores the parent operation.
        /// </summary>
        /// <param name="parentContext">Parent operation context store to replace child operation context store.</param>
        internal static void RestoreCallContext(OperationContextForCallContext parentContext)
        {
            CallContext.FreeNamedDataSlot(OperationContextSlotName);
            if (parentContext != null)
            {
                CallContext.LogicalSetData(OperationContextSlotName, parentContext);
            }
        }
    }
}
