namespace System
{
    using System.Globalization;

    internal static class TimeSpanEx
    {
        public static readonly TimeSpan InfiniteTimeSpan = new TimeSpan(0, 0, 0, 0, -1);

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.TimeSpan.Parse(System.String)", Justification = "TimeSpan.Parse(string, CultureInfo) doesn't exist in .NET 3.5")]
        public static TimeSpan Parse(string value, CultureInfo info)
        {
            return TimeSpan.Parse(value);
        }

        public static bool TryParse(string value, CultureInfo info, out TimeSpan output)
        {
            return TimeSpan.TryParse(value, out output);
        }


        public static string ToString(this TimeSpan timeSpan, CultureInfo info, string format)
        {
            return timeSpan.ToString();
        }
    }
}
