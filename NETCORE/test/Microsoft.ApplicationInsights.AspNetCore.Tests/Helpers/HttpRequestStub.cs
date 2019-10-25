namespace Microsoft.ApplicationInsights.AspNetCore.Tests.Helpers
{
    using System;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Http.Internal;

    public class HttpRequestStub : DefaultHttpRequest
    {
        public Func<IHeaderDictionary> OnGetHeaders = () => null;

        public HttpRequestStub(HttpContext context) : base(context)
        {
        }

        public override IHeaderDictionary Headers => this.OnGetHeaders();
    }
}
