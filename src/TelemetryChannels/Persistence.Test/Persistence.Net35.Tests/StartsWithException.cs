namespace Xunit.Sdk
{
    using System;
    using System.Globalization;
    using System.Runtime.Serialization;

    /// <summary>
    /// Exception thrown when a string does not start with the expected value.
    /// </summary>
    [Serializable]
    public class StartsWithException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StartsWithException"/> class.
        /// </summary>
        /// <param name="expected">The expected string value.</param>
        /// <param name="actual">The actual value.</param>
        public StartsWithException(string expected, string actual)
            : base(
                string.Format(
                    CultureInfo.CurrentCulture,
                    "Assert.StartsWith() Failure:{2}Expected: {0}{2}Actual:   {1}",
                    expected ?? "(null)",
                    ShortenActual(expected, actual) ?? "(null)",
                    Environment.NewLine))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StartsWithException"/> class with serialized data.
        /// </summary>
        /// <param name="info">The <see cref="T:System.Runtime.Serialization.SerializationInfo"/> that holds the serialized object data about the exception being thrown. </param><param name="context">The <see cref="T:System.Runtime.Serialization.StreamingContext"/> that contains contextual information about the source or destination. </param><exception cref="T:System.ArgumentNullException">The <paramref name="info"/> parameter is null. </exception><exception cref="T:System.Runtime.Serialization.SerializationException">The class name is null or <see cref="P:System.Exception.HResult"/> is zero (0). </exception>
        protected StartsWithException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        private static string ShortenActual(string expected, string actual)
        {
            if (expected == null || actual == null || actual.Length <= expected.Length)
            {
                return actual;
            }

            return actual.Substring(0, expected.Length) + "...";
        }
    }
}