namespace Microsoft.ApplicationInsights.Extensibility.Implementation
{
    using System;
    using System.Text.RegularExpressions;

    internal static class InstrumentationKeyValidation
    {
        /// <summary>
        /// Checks if an instrumentation key contains invalid characters.
        /// </summary>
        /// <param name="instrumentationKey">Candidate instrumentation key.</param>
        /// <exception cref="ArgumentNullException">Throws if instrumentation key is null.</exception>
        /// <exception cref="ArgumentException">Throws if instrumentation key is invalid with description.</exception>
        public static void Validate(string instrumentationKey)
        {
            if (instrumentationKey == null)
            {
                throw new ArgumentNullException(nameof(instrumentationKey));
            }
            else if (string.IsNullOrWhiteSpace(instrumentationKey))
            {
                throw new ArgumentException("Instrumentation Key can not be empty.");
            }

            Regex allowedCharacters = new Regex("^[a-zA-Z0-9-:]*$");
            if (!allowedCharacters.IsMatch(instrumentationKey))
            {
                foreach (char c in instrumentationKey)
                {
                    if (char.IsControl(c))
                    {
                        throw new ArgumentException("Instrumentation Key contains non-printable characters.");
                    }
                }

                throw new ArgumentException("Instrumentation Key contains illegal characters. Should only contain A-Z, a-z, 0-9, hyphen '-', or colon ':'.");
            }
        }
    }
}
