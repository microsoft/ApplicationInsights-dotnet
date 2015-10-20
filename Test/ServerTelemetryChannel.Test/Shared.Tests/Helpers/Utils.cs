namespace Microsoft.ApplicationInsights.WindowsServer.Channel.Helpers
{
    using System;

    public static class Utils
    {
        internal static bool EqualsWithPrecision(this double value1, double value2, double precision)
        {
            return (value1 >= value2 - precision) && (value1 <= value2 + precision);
        }
    }
}
