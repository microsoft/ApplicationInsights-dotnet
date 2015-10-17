namespace Microsoft.ApplicationInsights.Extensibility.Implementation
{
    using System.Threading;

    internal static class AsyncLocalHelpers
    {
        internal static AsyncLocal<OperationContextForAsyncLocal> AsyncLocalContext = new AsyncLocal<OperationContextForAsyncLocal>();

        /// <summary>
        /// Saves the context store to the call context.
        /// </summary>
        /// <param name="operationContext">Operation context store instance.</param>
        internal static void SaveOperationContext(OperationContextForAsyncLocal operationContext)
        {
            AsyncLocalContext.Value = operationContext;
        }

        /// <summary>
        /// Returns the current operation context store present in the call context.
        /// </summary>
        internal static OperationContextForAsyncLocal GetCurrentOperationContext()
        {
            return AsyncLocalContext.Value;
        }

        /// <summary>
        /// Clears the call context and restores the parent operation.
        /// </summary>
        /// <param name="parentContext">Parent operation context store to replace child operation context store.</param>
        internal static void RestoreOperationContext(OperationContextForAsyncLocal parentContext)
        {
            AsyncLocalContext.Value = null;
            if (parentContext != null)
            {
                AsyncLocalContext.Value = parentContext;
            }
        }
    }
}
