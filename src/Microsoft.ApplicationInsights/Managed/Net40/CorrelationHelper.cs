namespace Microsoft.ApplicationInsights.Net40
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;

    /// <summary>
    /// This code is used by ApplicationInsights Framework and is not intended to be called from user code.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("This code is used by ApplicationInsights Framework and is not intended to be called from user code.")]
    public class CorrelationHelper
    {
        /// <summary>
        /// This code is used by ApplicationInsights Framework and is not intended to be called from user code.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("This code is used by ApplicationInsights Framework and is not intended to be called from user code.")]
        public static IDictionary<string, string> GetCorrelationContext()
        {
            var context = CallContextHelpers.GetCurrentOperationContext();

            return context?.CorrelationContext;
        }

        /// <summary>
        /// This code is used by ApplicationInsights Framework and is not intended to be called from user code.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("This code is used by ApplicationInsights Framework and is not intended to be called from user code.")]
        public static bool SetOperationContext(RequestTelemetry requestTelemetry, IDictionary<string, string> correlationContext)
        {
            var context = CallContextHelpers.GetCurrentOperationContext();
            if (context != null)
            {
                return false;
            }

            context = new OperationContextForCallContext()
            {
                ParentOperationId = requestTelemetry.Id,
                RootOperationId = requestTelemetry.Context.Operation.Id,
                RootOperationName = requestTelemetry.Context.Operation.Name,
                CorrelationContext = correlationContext
            };

            CallContextHelpers.SaveOperationContext(context);
            return true;
        }

        /// <summary>
        /// This code is used by ApplicationInsights Framework and is not intended to be called from user code.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("This code is used by ApplicationInsights Framework and is not intended to be called from user code.")]
        public static void CleanOperationContext()
        {
            CallContextHelpers.SaveOperationContext(null);
        }

        /// <summary>
        /// This code is used by ApplicationInsights Framework and is not intended to be called from user code.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("This code is used by ApplicationInsights Framework and is not intended to be called from user code.")]
        public static bool UpdateOperationName(string operationName)
        {
            var context = CallContextHelpers.GetCurrentOperationContext();
            if (context == null)
            {
                return false;
            }

            context.RootOperationName = operationName;
            CallContextHelpers.SaveOperationContext(context);
            return true;
        }
    }
}
