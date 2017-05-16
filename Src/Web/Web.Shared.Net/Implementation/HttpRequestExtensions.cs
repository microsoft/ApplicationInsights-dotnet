namespace Microsoft.ApplicationInsights.Web.Implementation
{
    using System;
    using System.Web;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing;

    /// <summary>
    /// HttpRequest Extensions.
    /// </summary>
    internal static partial class HttpRequestExtensions
    {
        public static string GetUserHostAddress(this HttpRequest httpRequest)
        {
            if (httpRequest == null)
            {
                return null;
            }

            try
            {
                return httpRequest.UserHostAddress;
            }
            catch (ArgumentException exp)
            {
                // System.ArgumentException: Value does not fall within the expected range. Fails in IIS7, WCF OneWay.
                WebEventSource.Log.UserHostNotCollectedWarning(exp.ToInvariantString());
                return null;
            }
        }
    }
}
