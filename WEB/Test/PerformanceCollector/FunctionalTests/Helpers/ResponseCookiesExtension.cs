namespace Functional.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Threading.Tasks;

    public static class ResponseCookiesExtension
    {
        private const string userCookieKey = "ai_user";
        private const string sessionCookieKey = "ai_session";
 
        /// <summary>
        /// Returns the user cookie string from the total available set of cookies
        /// </summary>
        public static string ReceiveUserCookie(this CookieCollection responseCookies)
        {
            var enumerator = responseCookies.GetEnumerator();
            string userCookie = string.Empty;
            foreach(Cookie cookie in responseCookies)
            {
                if (cookie.Name.Equals(userCookieKey))
                {
                    userCookie = cookie.Value;
                }
            }
            return userCookie;
        }

        /// <summary>
        /// Returns the session cookie string from the total available set of cookies
        /// </summary>
        public static string ReceiveSessionCookie(this CookieCollection responseCookies)
        {
            var enumerator = responseCookies.GetEnumerator();
            string sessionCookie = string.Empty;
            foreach (Cookie cookie in responseCookies)
            {
                if (cookie.Name.Equals(sessionCookieKey))
                {
                    sessionCookie = cookie.Value;
                }
            }
            return sessionCookie;
        }
    }
}
