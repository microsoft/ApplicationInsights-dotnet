namespace Microsoft.ApplicationInsights.Extensibility.Implementation
{
#if NET40 || NET45
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
        internal static void SaveOperationContext(OperationContextForCallContext operationContext)
        {
            CallContext.FreeNamedDataSlot(OperationContextSlotName);
            CallContext.LogicalSetData(OperationContextSlotName, operationContext);
        }

        /// <summary>
        /// Returns the current operation context store present in the call context.
        /// </summary>
        internal static OperationContextForCallContext GetCurrentOperationContext()
        {
            return CallContext.LogicalGetData(OperationContextSlotName) as OperationContextForCallContext;
        }

        /// <summary>
        /// Clears the call context and restores the parent operation.
        /// </summary>
        /// <param name="parentContext">Parent operation context store to replace child operation context store.</param>
        internal static void RestoreOperationContext(OperationContextForCallContext parentContext)
        {
            CallContext.FreeNamedDataSlot(OperationContextSlotName);
            if (parentContext != null)
            {
                CallContext.LogicalSetData(OperationContextSlotName, parentContext);
            }
        }
    }
#else // Use AsyncLocal<T> where available
    using System.Threading;

    internal static class CallContextHelpers
    {
        internal static AsyncLocal<OperationContextForCallContext> AsyncLocalContext = new AsyncLocal<OperationContextForCallContext>();

        /// <summary>
        /// Saves the context store to the call context.
        /// </summary>
        /// <param name="operationContext">Operation context store instance.</param>
        internal static void SaveOperationContext(OperationContextForCallContext operationContext)
        {
            AsyncLocalContext.Value = operationContext;
        }

        /// <summary>
        /// Returns the current operation context store present in the call context.
        /// </summary>
        internal static OperationContextForCallContext GetCurrentOperationContext()
        {
            return AsyncLocalContext.Value;
        }

        /// <summary>
        /// Clears the call context and restores the parent operation.
        /// </summary>
        /// <param name="parentContext">Parent operation context store to replace child operation context store.</param>
        internal static void RestoreOperationContext(OperationContextForCallContext parentContext)
        {
            AsyncLocalContext.Value = null;
            if (parentContext != null)
            {
                AsyncLocalContext.Value = parentContext;
            }
        }
    }
#endif
}
