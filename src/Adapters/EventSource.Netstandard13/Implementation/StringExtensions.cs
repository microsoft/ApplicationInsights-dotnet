//-----------------------------------------------------------------------
// <copyright file="StringExtensions.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System.Text.RegularExpressions;

namespace Microsoft.ApplicationInsights.EventSourceListener.Implementation
{
    internal static class StringExtensions
    {
        public static string WildCardToRegex(this string value)
        {
            return "^" + Regex.Escape(value).Replace("\\?", ".").Replace("\\*", ".*") + "$";
        }
    }
}
