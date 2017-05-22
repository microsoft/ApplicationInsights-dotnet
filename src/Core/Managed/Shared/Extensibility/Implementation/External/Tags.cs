// <copyright file="Tags.cs" company="Microsoft">
// Copyright © Microsoft. All Rights Reserved.
// </copyright>

namespace Microsoft.ApplicationInsights.Extensibility.Implementation.External
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;

    /// <summary>
    /// Base class for tags backed context.
    /// </summary>
    internal static class Tags
    {
        internal static bool? GetTagBoolValueOrNull(this IDictionary<string, string> tags, string tagKey)
        {
            string tagValue = GetTagValueOrNull(tags, tagKey);
            if (string.IsNullOrEmpty(tagValue))
            {
                return null;
            }

            return bool.Parse(tagValue);
        }

        internal static DateTimeOffset? GetTagDateTimeOffsetValueOrNull(this IDictionary<string, string> tags, string tagKey)
        {
            string tagValue = GetTagValueOrNull(tags, tagKey);
            if (string.IsNullOrEmpty(tagValue))
            {
                return null;
            }

            return DateTimeOffset.Parse(tagValue, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
        }

        internal static int? GetTagIntValueOrNull(this IDictionary<string, string> tags, string tagKey)
        {
            string tagValue = GetTagValueOrNull(tags, tagKey);
            if (string.IsNullOrEmpty(tagValue))
            {
                return null;
            }

            return int.Parse(tagValue, CultureInfo.InvariantCulture);
        }

        internal static void SetStringValueOrRemove(this IDictionary<string, string> tags, string tagKey, string tagValue)
        {
            SetTagValueOrRemove(tags, tagKey, tagValue);
        }

        internal static void SetDateTimeOffsetValueOrRemove(this IDictionary<string, string> tags, string tagKey, DateTimeOffset? tagValue)
        {
            if (tagValue == null)
            {
                SetTagValueOrRemove(tags, tagKey, tagValue);
            }
            else
            {
                string tagValueString = tagValue.Value.ToString("O", CultureInfo.InvariantCulture);
                SetTagValueOrRemove(tags, tagKey, tagValueString);
            }
        }

        internal static void SetTagValueOrRemove<T>(this IDictionary<string, string> tags, string tagKey, T tagValue)
        {
            SetTagValueOrRemove(tags, tagKey, Convert.ToString(tagValue, CultureInfo.InvariantCulture));
        }

        internal static string CopyTagValue(string sourceValue, string targetValue)
        {
            if (string.IsNullOrEmpty(sourceValue) && !string.IsNullOrEmpty(targetValue))
            {
                return targetValue;
            }

            return sourceValue;
        }

        internal static bool? CopyTagValue(bool? sourceValue, bool? targetValue)
        {
            if (!sourceValue.HasValue && targetValue.HasValue)
            {
                return targetValue;
            }

            return sourceValue;
        }

        internal static string GetTagValueOrNull(this IDictionary<string, string> tags, string tagKey)
        {
            string tagValue;
            if (tags.TryGetValue(tagKey, out tagValue))
            {
                return tagValue;
            }

            return null;
        }

        internal static void UpdateTagValue(this IDictionary<string, string> tags, string tagKey, string tagValue)
        {
            if (!string.IsNullOrEmpty(tagValue))
            {
                int limit;
                if (Property.TagSizeLimits.TryGetValue(tagKey, out limit) && tagValue.Length > limit)
                {
                    tagValue = Property.TrimAndTruncate(tagValue, limit);
                }

                tags.Add(tagKey, tagValue);
            }
        }

        internal static void UpdateTagValue(this IDictionary<string, string> tags, string tagKey, bool? tagValue)
        {
            if (tagValue.HasValue)
            {
                tags.Add(tagKey, tagValue.Value.ToString());
            }
        }

        private static void SetTagValueOrRemove(this IDictionary<string, string> tags, string tagKey, string tagValue)
        {
            if (string.IsNullOrEmpty(tagValue))
            {
                tags.Remove(tagKey);
            }
            else
            {
                tags[tagKey] = tagValue;
            }
        }
    }
}
