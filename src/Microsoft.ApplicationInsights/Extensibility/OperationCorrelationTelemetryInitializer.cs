namespace Microsoft.ApplicationInsights.Extensibility
{
    using System.Diagnostics;
    using Implementation;
    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;

#if NET45
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
        
        private static string FormatRequestId(Activity activity)
        {
            return string.Concat("|", activity.TraceId.AsHexString, ".", activity.SpanId.AsHexString, ".");
        }

        /// <summary>
        /// Initializes/Adds operation id to the existing telemetry item.
        /// </summary>
        /// <param name="telemetryItem">Target telemetry item to add operation id.</param>
        public void Initialize(ITelemetry telemetryItem)
        {
            var itemContext = telemetryItem.Context.Operation;
            var telemetryProp = telemetryItem as ISupportProperties;
            bool isActivityAvailable = false;
            isActivityAvailable = ActivityExtensions.TryRun(() =>
            { 
                var currentActivity = Activity.Current;
                if (currentActivity != null)
                {
                    if (string.IsNullOrEmpty(itemContext.Id))
                    {
                        if (currentActivity.IdFormat == ActivityIdFormat.W3C)
                        {
                            itemContext.Id = currentActivity.TraceId.AsHexString;

                            if (string.IsNullOrEmpty(itemContext.ParentId))
                            {
                                itemContext.ParentId = FormatRequestId(currentActivity);
                            }

                            if (!string.IsNullOrEmpty(currentActivity.TraceStateString) && telemetryProp != null && !telemetryProp.Properties.ContainsKey("tracestate"))
                            {
                                telemetryProp.Properties.Add("tracestate", currentActivity.TraceStateString);
                            }

                        }
                        else
                        {
                            itemContext.Id = currentActivity.RootId;

                            if (string.IsNullOrEmpty(itemContext.ParentId))
                            {
                                itemContext.ParentId = currentActivity.ParentId;
                            }

                        }

                        foreach (var baggage in currentActivity.Baggage)
                        {
                            if (telemetryProp != null && !telemetryProp.Properties.ContainsKey(baggage.Key))
                            {
                                telemetryProp.Properties.Add(baggage);
                            }
                        }
                    }

                    string operationName = currentActivity.GetOperationName();

                    if (string.IsNullOrEmpty(itemContext.Name) && !string.IsNullOrEmpty(operationName))
                    {
                        itemContext.Name = operationName;
                    }
                }
            });

            if (!isActivityAvailable)
            {
                if (string.IsNullOrEmpty(itemContext.ParentId) || string.IsNullOrEmpty(itemContext.Id) || string.IsNullOrEmpty(itemContext.Name))
                {
                    var parentContext = CallContextHelpers.GetCurrentOperationContext();
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

                        if (parentContext.CorrelationContext != null)
                        {
                            foreach (var item in parentContext.CorrelationContext)
                            {
                                if (telemetryProp != null && !telemetryProp.Properties.ContainsKey(item.Key))
                                {
                                    telemetryProp.Properties.Add(item);
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
