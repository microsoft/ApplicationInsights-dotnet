namespace Microsoft.ApplicationInsights.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;

    internal static class ExceptionExtensions
    {
        public static string FlattenMessages(this Exception ex)
        {
            var list = new List<string>();

            for (var exWalk = ex; exWalk != null; exWalk = exWalk.InnerException)
            {
                list.Add(exWalk.Message);
            }

            return string.Join(" | ", list);
        }

        public static string ToLogString(this Exception ex, bool includeStackTrace = false)
        {
            string msg = "Type: '{0}' Message: '{1}'";

            var log = string.Format(CultureInfo.InvariantCulture, msg, ex.GetType().ToString(), ex.FlattenMessages());

            if (includeStackTrace)
            {
                log = string.Concat(log, "StackTrace: ", ex.StackTrace);
            }

            return log;
        }
    }
}
