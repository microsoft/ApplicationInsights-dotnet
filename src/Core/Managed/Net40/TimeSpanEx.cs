namespace System
{
    using System.Globalization;

    internal static class TimeSpanEx
    {
        public static TimeSpan Parse(string value, CultureInfo info)
        {
            return TimeSpan.Parse(value, info);
        }

        public static bool TryParse(string value, CultureInfo info, out TimeSpan output)
        {
            return TimeSpan.TryParse(value, info, out output);
        }

        public static string ToString(this TimeSpan timeSpan, CultureInfo info, string format)
        {
            return timeSpan.ToString(format, info);
        }
    }
}
