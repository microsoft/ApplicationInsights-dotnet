namespace Microsoft.ApplicationInsights.Common
{
    using System.Diagnostics;

    internal static class ActivityExtensions
    {
        public static Activity UpdateParent(this Activity original, string newParentId)
        {
            if (original == null || original.Parent != null)
            {
                return original;
            }

            var auxActivity = new Activity(original.OperationName)
                .SetParentId(newParentId)
                .SetStartTime(original.StartTimeUtc);

            foreach (var baggageItem in original.Baggage)
            {
                auxActivity.AddBaggage(baggageItem.Key, baggageItem.Value);
            }

            foreach (var tag in original.Tags)
            {
                auxActivity.AddTag(tag.Key, tag.Value);
            }

            return auxActivity.Start();
        }
    }
}
