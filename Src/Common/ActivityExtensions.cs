namespace Microsoft.ApplicationInsights.Common
{
    using System.Diagnostics;

    internal static class ActivityExtensions
    {
        public static Activity UpdateParent(this Activity original, string newParentId)
        {
            Debug.Assert(original != null, "original Activity cannot be null");
            Debug.Assert(original.ParentId == null, "cannot update parent - parentId is not null");
            Debug.Assert(original.Parent == null, "cannot update parent - parent is not null");

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
