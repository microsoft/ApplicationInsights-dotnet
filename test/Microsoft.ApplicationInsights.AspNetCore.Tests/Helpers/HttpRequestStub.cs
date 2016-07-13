using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;

namespace Microsoft.ApplicationInsights.AspNetCore.Tests.Helpers
{
    public class HttpRequestStub : DefaultHttpRequest
    {
        public Func<IHeaderDictionary> OnGetHeaders = () => null;

        public HttpRequestStub(HttpContext context) : base(context)
        {
        }

        public override IHeaderDictionary Headers => this.OnGetHeaders();
    }
}
