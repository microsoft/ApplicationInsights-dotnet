namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Authentication
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    internal static class AuthConstants
    {
        /// <summary>
        /// https://docs.microsoft.com/en-us/azure/active-directory/develop/msal-acquire-cache-tokens#scopes-when-acquiring-tokens
        /// 
        /// Other APIs might require that no scheme or host is included in the scope value, and expect only the app ID (a GUID) and the scope name, for example: 11111111-1111-1111-1111-111111111111/api.read
        /// </summary>
        public const string Scope = "https://storage.azure.com/.default"; // example from Blob Storage. TODO: NEED OUR OWN SCOPE

        public static string[] GetScopes() => new string[] { Scope };
    }
}
