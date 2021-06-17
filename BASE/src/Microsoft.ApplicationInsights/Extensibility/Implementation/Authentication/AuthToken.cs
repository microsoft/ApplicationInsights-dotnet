namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Authentication
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// This class represents the Azure.Core.AccessToken returned by Azure.Core.TokenCredential.
    /// (https://github.com/Azure/azure-sdk-for-net/blob/master/sdk/core/Azure.Core/src/AccessToken.cs).
    /// </summary>
    public struct AuthToken
    {
        public AuthToken(string token, DateTimeOffset expiresOn)
        {
            this.Token = token;
            this.ExpiresOn = expiresOn;
        }

        /// <summary>
        /// Get the access token value.
        /// </summary>
        public string Token { get; set; }

        /// <summary>
        /// Gets the time when the provided token expires.
        /// </summary>
        public DateTimeOffset ExpiresOn { get; set; }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            if (obj is AuthToken authToken)
            {
                return authToken.ExpiresOn == ExpiresOn && authToken.Token == Token;
            }

            return false;
        }

        /// <inheritdoc />
        public static bool operator == (AuthToken left, AuthToken right) => left.Equals(right);

        /// <inheritdoc />
        public static bool operator != (AuthToken left, AuthToken right) => !left.Equals(right);
    }
}
