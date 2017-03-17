namespace Microsoft.ApplicationInsights.Extensibility
{
    using Implementation;
    using Microsoft.ApplicationInsights.Channel;

#if NET40 || NET45
    /// <summary>
    /// Telemetry initializer that populates OperationContext for the telemetry item based on context stored in CallContext.
    /// </summary>
#else
    /// <summary>
    /// Telemetry initializer that populates OperationContext for the telemetry item based on context stored in an AsyncLocal variable.
    /// </summary>
#endif
    public class OperationCorrelationTelemetryInitializer : ITelemetryInitializer
    {
        /// <summary>
        /// Initializes/Adds operation id to the existing telemetry item.
        /// </summary>
        /// <param name="telemetryItem">Target telemetry item to add operation id.</param>
        public void Initialize(ITelemetry telemetryItem)
        {
            var itemContext = telemetryItem.Context.Operation;
            var parentContext = CallContextHelpers.GetCurrentOperationContext();

            if (string.IsNullOrEmpty(itemContext.ParentId) || string.IsNullOrEmpty(itemContext.Id) || string.IsNullOrEmpty(itemContext.Name))
            {
                if (parentContext != null)
                {
                    if (string.IsNullOrEmpty(itemContext.ParentId)
                        && !string.IsNullOrEmpty(parentContext.ParentOperationId))
                    {
                        itemContext.ParentId = parentContext.ParentOperationId;
                    }

                    if (string.IsNullOrEmpty(itemContext.Id)
                        && !string.IsNullOrEmpty(parentContext.RootOperationId))
                    {
                        itemContext.Id = parentContext.RootOperationId;
                    }

                    if (string.IsNullOrEmpty(itemContext.Name)
                        && !string.IsNullOrEmpty(parentContext.RootOperationName))
                    {
                        itemContext.Name = parentContext.RootOperationName;
                    }
                }
            }

            // update CorrelationContext if there is any parent CorrelationContext
            if (parentContext != null && parentContext.CorrelationContext != null)
            {
                foreach (var item in parentContext.CorrelationContext)
                {
                    if (!telemetryItem.Context.CorrelationContext.ContainsKey(item.Key))
                    {
                        telemetryItem.Context.CorrelationContext.Add(item);
                    }
                }
            }

            // TODO: change backend so it tracts correlaton context as properties, so we don't need to copy them
            // we don't expect any Correlaiton-Context at all, or in the worst case expect a few item,
            // copying it should not affect performance in the meantime.
            foreach (var item in telemetryItem.Context.CorrelationContext)
            {
                if (!telemetryItem.Context.Properties.ContainsKey(item.Key))
                {
                    telemetryItem.Context.Properties.Add(item.Key, item.Value);
                }
            }
        }
    }
}
