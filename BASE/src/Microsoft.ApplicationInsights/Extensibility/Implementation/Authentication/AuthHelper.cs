namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Authentication
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    internal static class AuthHelper
    {
        internal static string GetScope(string audience)
        {
            if (audience == null)
            {
                throw new ArgumentNullException(nameof(audience));
            }
            else if (audience.Length > AuthConstants.AudienceStringMaxLength)
            {
                throw new ArgumentOutOfRangeException(nameof(audience), FormattableString.Invariant($"Values greater than {AuthConstants.AudienceStringMaxLength} characters are not allowed."));
            }

            if (audience.EndsWith("/"))
            {
                return $"{audience}{AuthConstants.DefaultAzureMonitorPermission}";
            }
            else
            {
                return $"{audience}/{AuthConstants.DefaultAzureMonitorPermission}";
            }
        }
    }
}
