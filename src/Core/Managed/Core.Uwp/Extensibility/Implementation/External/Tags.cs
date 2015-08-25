// <copyright file="Tags.cs" company="Microsoft">
// Copyright © Microsoft. All Rights Reserved.
// </copyright>

//------------------------------------------------------------------------------
//
//     This code was updated from a master copy in the DataCollectionSchemas repo.
// 
//     Repo     : DataCollectionSchemas
//     File     : Tags.cs
//
//     Changes to this file may cause incorrect behavior and will be lost when
//     the code is updated.
//
//------------------------------------------------------------------------------

#if DATAPLATFORM
namespace Microsoft.Developer.Analytics.DataCollection.Model.v2
#else
namespace Microsoft.ApplicationInsights.Extensibility.Implementation.External
#endif
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

        internal static int? GetTagIntValueOrNull(this IDictionary<string, string> tags, string tagKey)
        {
            string tagValue = GetTagValueOrNull(tags, tagKey);
            if (string.IsNullOrEmpty(tagValue))
            {
                return null;
            }

            return int.Parse(tagValue, CultureInfo.InvariantCulture);
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
                string tagValueString = tagValue.Value.ToString("o", CultureInfo.InvariantCulture);
                SetTagValueOrRemove(tags, tagKey, tagValueString);
            }
        }

        internal static void SetTagValueOrRemove<T>(this IDictionary<string, string> tags, string tagKey, T tagValue)
        {
            SetTagValueOrRemove(tags, tagKey, Convert.ToString(tagValue, CultureInfo.InvariantCulture));
        }

        internal static void InitializeTagValue<T>(this IDictionary<string, string> tags, string tagKey, T tagValue)
        {
            if (!tags.ContainsKey(tagKey))
            {
                SetTagValueOrRemove(tags, tagKey, tagValue);
            }
        }

        internal static void InitializeTagDateTimeOffsetValue(this IDictionary<string, string> tags, string tagKey, DateTimeOffset? tagValue)
        {
            if (!tags.ContainsKey(tagKey))
            {
                SetDateTimeOffsetValueOrRemove(tags, tagKey, tagValue);
            }
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
