namespace Microsoft.ApplicationInsights.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Globalization;

    /// <summary>
    /// Provides a set of extension methods for <see cref="Exception"/>.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class ExceptionExtensions
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

        public static string ToLogString(this Exception ex)
        {
            string msg = "Type: '{0}' Message: '{1}'";
            return string.Format(CultureInfo.InvariantCulture, msg, ex.GetType().ToString(), ex.FlattenMessages());
        }
    }
}
