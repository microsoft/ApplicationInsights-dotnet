namespace Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel.Implementation.SamplingInternals
{
    using System;
    using System.Collections.Generic;
    using Microsoft.ApplicationInsights.DataContracts;

    internal static class SamplingIncludesUtility
    {
        private const string DependencyTelemetryName = "DEPENDENCY";
        private const string EventTelemetryName = "EVENT";
        private const string ExceptionTelemetryName = "EXCEPTION";
        private const string PageViewTelemetryName = "PAGEVIEW";
        private const string RequestTelemetryName = "REQUEST";
        private const string TraceTelemetryName = "TRACE";

        private static readonly char[] ListSeparators = { ';' };
        private static readonly IDictionary<string, SamplingTelemetryItemTypes> AllowedTypes;

        static SamplingIncludesUtility()
        {
            AllowedTypes = new Dictionary<string, SamplingTelemetryItemTypes>(6, StringComparer.OrdinalIgnoreCase)
            {
                { DependencyTelemetryName, SamplingTelemetryItemTypes.RemoteDependency }, //DependencyTelemetry
                { EventTelemetryName, SamplingTelemetryItemTypes.Event }, //EventTelemetry
                { ExceptionTelemetryName, SamplingTelemetryItemTypes.Exception }, //ExceptionTelemetry
                { PageViewTelemetryName, SamplingTelemetryItemTypes.PageView }, //PageViewTelemetry
                { RequestTelemetryName, SamplingTelemetryItemTypes.Request }, //RequestTelemetry
                { TraceTelemetryName, SamplingTelemetryItemTypes.Message }, //TraceTelemetry
            };
        }

        public static SamplingTelemetryItemTypes CalculateFromIncludes(string includesString)
        {
            return Calculate(operation: IncludeOperator, flags: SamplingTelemetryItemTypes.None, input: includesString);
        }

        public static SamplingTelemetryItemTypes CalculateFromExcludes(string excludesString)
        {
            return Calculate(operation: ExcludeOperator, flags: ~SamplingTelemetryItemTypes.None, input: excludesString);
        }

        private static SamplingTelemetryItemTypes Calculate(Func<SamplingTelemetryItemTypes, SamplingTelemetryItemTypes, SamplingTelemetryItemTypes> operation, SamplingTelemetryItemTypes flags, string input)
        {
            if (!string.IsNullOrEmpty(input))
            {
                foreach (string item in SplitInput(input))
                {
                    if (AllowedTypes.TryGetValue(item, out SamplingTelemetryItemTypes value))
                    {
                        flags = operation(flags, value);
                    }
                }
            }

            return flags;
        }

        public static SamplingTelemetryItemTypes IncludeOperator(SamplingTelemetryItemTypes flags, SamplingTelemetryItemTypes value) => flags |= value;
        public static SamplingTelemetryItemTypes ExcludeOperator(SamplingTelemetryItemTypes flags, SamplingTelemetryItemTypes value) => flags &= ~value;

        internal static string[] SplitInput(string input) => input.Split(ListSeparators, StringSplitOptions.RemoveEmptyEntries);
    }
}
