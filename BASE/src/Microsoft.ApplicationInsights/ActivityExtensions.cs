namespace Microsoft.ApplicationInsights
{
    using System.Diagnostics;

    internal static class ActivityExtensions
    {
        private const string OperationNameTag = "OperationName";

        internal static void SetOperationName(this Activity activity, string operationName)
        {
            Debug.Assert(!string.IsNullOrEmpty(operationName), "OperationName must not be null or empty");
            Debug.Assert(activity != null, "Activity must not be null");
            activity.AddTag(OperationNameTag, operationName);
        }
    }
}
