namespace Microsoft.ApplicationInsights
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.CompilerServices;

    internal static class ActivityExtensions
    {
        private const string OperationNameTag = "OperationName";

        internal static string GetOperationName(this Activity activity)
        {
            Debug.Assert(activity != null, "Activity must not be null");
            return activity.Tags.FirstOrDefault(tag => tag.Key == OperationNameTag).Value;
        }

        internal static void SetOperationName(this Activity activity, string operationName)
        {
            Debug.Assert(!string.IsNullOrEmpty(operationName), "OperationName must not be null or empty");
            Debug.Assert(activity != null, "Activity must not be null");
            activity.AddTag(OperationNameTag, operationName);
        }
    }
}
