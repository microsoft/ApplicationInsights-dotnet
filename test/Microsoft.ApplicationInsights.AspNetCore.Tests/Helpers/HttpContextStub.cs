namespace Microsoft.ApplicationInsights.AspNetCore.Tests.Helpers
{
    using System;
    using Microsoft.AspNetCore.Http;

    /// <summary>
    /// Class that is used in unit tests and allows to override main HttpContext properties. 
    /// </summary>
    public class HttpContextStub : DefaultHttpContext
    {
        public Func<HttpRequest> OnRequestGetter = () => null;

        public override HttpRequest Request => this.OnRequestGetter();
    }
}
