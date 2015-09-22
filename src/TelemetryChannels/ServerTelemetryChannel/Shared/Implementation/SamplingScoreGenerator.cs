namespace Microsoft.ApplicationInsights.WindowsServer.Channel.Implementation
{
    using System;

    using Microsoft.ApplicationInsights.Channel;

    /// <summary>
    /// Utility class for sampling score generation.
    /// </summary>
    internal static class SamplingScoreGenerator
    {
        /// <summary>
        /// Generates telemetry sampling score between 0 and 100.
        /// </summary>
        /// <param name="telemetry">Telemetry item to score.</param>
        /// <returns>Item sampling score.</returns>
        public static double GetSamplingScore(ITelemetry telemetry)
        {
            double samplingScore = 0;

            if (telemetry.Context.User.Id != null)
            {
                samplingScore = (double)telemetry.Context.User.Id.GetSamplingHashCode() / int.MaxValue;
            }
            else if (telemetry.Context.Operation.Id != null)
            {
                samplingScore = (double)telemetry.Context.Operation.Id.GetSamplingHashCode() / int.MaxValue;
            }
            else
            {
                samplingScore = (double)WeakConcurrentRandom.Instance.Next() / ulong.MaxValue;
            }

            return samplingScore * 100;
        }

        internal static int GetSamplingHashCode(this string input)
        {
            if (input == null)
            {
                return 0;
            }

            int hash = 5381;

            for (int i = 0; i < input.Length; i++)
            {
                hash = ((hash << 5) + hash) + (int)input[i];
            }

            return hash == int.MinValue ? int.MaxValue : Math.Abs(hash);
        }
    }
}
