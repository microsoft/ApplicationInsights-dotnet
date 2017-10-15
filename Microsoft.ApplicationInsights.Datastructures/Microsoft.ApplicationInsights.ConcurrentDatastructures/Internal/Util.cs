using System;
using System.Runtime.CompilerServices;


namespace Microsoft.ApplicationInsights.ConcurrentDatastructures
{
    internal static class Util
    {
        public const string NullString = "null";

        private const string FallbackParemeterName = "specified parameter";


        /// <summary>
        /// Paramater check for Null with a little more informative exception.
        /// </summary>
        /// <param name="value">Value to be checked.</param>
        /// <param name="name">Name of the parameter being checked.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ValidateNotNull(object value, string name)
        {
            if (value == null)
            {
                throw new ArgumentNullException(name ?? Util.FallbackParemeterName);
            }
        }
    }
}
