namespace Microsoft.ApplicationInsights.W3C.Internal
{
    using System;
    using System.Diagnostics;
#if DEPENDENCY_COLLECTOR
    using Microsoft.ApplicationInsights.Common;
#else
    using Microsoft.ApplicationInsights.Common.Internal;
#endif

    internal static class W3CUtilities
    {
        internal static string GetRootId(string legacyId)
        {
            Debug.Assert(!string.IsNullOrEmpty(legacyId), "diagnosticId must not be null or empty");

            if (legacyId[0] == '|')
            {
                var dot = legacyId.IndexOf('.');

                return legacyId.Substring(1, dot - 1);
            }

            return StringUtilities.EnforceMaxLength(legacyId, InjectionGuardConstants.RequestHeaderMaxLength);
        }

        internal static bool TryGetTraceId(string legacyId, out ReadOnlySpan<char> traceId)
        {
            Debug.Assert(!string.IsNullOrEmpty(legacyId), "diagnosticId must not be null or empty");

            traceId = default;
            if (legacyId[0] == '|' && legacyId.Length >= 33 && legacyId[33] == '.')
            {
                for (int i = 1; i < 33; i++)
                {
                    if (!((legacyId[i] >= '0' && legacyId[i] <= '9') || (legacyId[i] >= 'a' && legacyId[i] <= 'f')))
                    {
                        return false;
                    }
                }

                traceId = legacyId.AsSpan().Slice(1, 32);
                return true;
            }

            return false;
        }

        internal static string FormatTelemetryId(string traceId, string spanId)
        {
            return string.Concat('|', traceId, '.', spanId, '.');
        }
    }
}
