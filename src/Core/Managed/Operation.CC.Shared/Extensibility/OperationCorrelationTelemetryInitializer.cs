namespace Microsoft.ApplicationInsights.Extensibility
{
    using Implementation;
    using Microsoft.ApplicationInsights.Channel;

    /// <summary>
    /// Telemetry initializer that populates OperationContext for the telemetry item based on context stored in CallContext.
    /// </summary>
    public class OperationCorrelationTelemetryInitializer : ITelemetryInitializer
    {
        /// <summary>
        /// Initializes/Adds operation id to the existing telemetry item.
        /// </summary>
        /// <param name="telemetryItem">Target telemetry item to add operation id.</param>
        public void Initialize(ITelemetry telemetryItem)
        {
            var itemOperationContext = telemetryItem.Context.Operation;
            var itemContext = telemetryItem.Context;
            OperationContextForCallContext parentContext;

            if (string.IsNullOrEmpty(itemOperationContext.ParentId) 
                || string.IsNullOrEmpty(itemOperationContext.Id) 
                || string.IsNullOrEmpty(itemOperationContext.Name)
                || string.IsNullOrEmpty(itemContext.User.Id)
                || string.IsNullOrEmpty(itemContext.Session.Id))
            {
                parentContext = CallContextHelpers.GetCurrentOperationContext();
                if (parentContext != null)
                {
                    if (string.IsNullOrEmpty(itemOperationContext.ParentId)
                        && !string.IsNullOrEmpty(parentContext.ParentOperationId))
                    {
                        itemOperationContext.ParentId = parentContext.ParentOperationId;
                    }

                    if (string.IsNullOrEmpty(itemOperationContext.Id)
                        && !string.IsNullOrEmpty(parentContext.RootOperationId))
                    {
                        itemOperationContext.Id = parentContext.RootOperationId;
                    }

                    if (string.IsNullOrEmpty(itemOperationContext.Name)
                        && !string.IsNullOrEmpty(parentContext.RootOperationName))
                    {
                        itemOperationContext.Name = parentContext.RootOperationName;
                    }

                    if (string.IsNullOrEmpty(itemContext.User.Id)
                        && !string.IsNullOrEmpty(parentContext.UserId))
                    {
                        itemContext.User.Id = parentContext.UserId;
                    }

                    if (string.IsNullOrEmpty(itemContext.Session.Id)
                        && !string.IsNullOrEmpty(parentContext.SessionId))
                    {
                        itemContext.Session.Id = parentContext.SessionId;
                    }
                }
            }
        }
    }
}
