namespace Microsoft.ApplicationInsights.Extensions
{
    using System;
    using System.Collections.Generic;

    internal static class ExceptionExtensions
    {
        public static string FlattenMessages(this Exception ex)
        {
            var list = new List<string>();

            for (var exWalk = ex; exWalk != null; exWalk = exWalk.InnerException)
            {
                list.Add(exWalk.Message);
            }

            return string.Join(";", list);
        }
    }
}
