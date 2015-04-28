namespace Microsoft.ApplicationInsights.AspNet.Tests.Helpers
{
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.AspNet.Hosting;
    using Microsoft.AspNet.Http;
    using Microsoft.AspNet.Http.Core;
    using Microsoft.AspNet.Mvc;
    using Microsoft.Framework.DependencyInjection;
    using System;

    public static class HttpContextAccessorHelper
    {
        public static HttpContextAccessor CreateHttpContextAccessor(RequestTelemetry requestTelemetry = null, ActionContext actionContext = null)
        {
            var services = new ServiceCollection();

            var request = new DefaultHttpContext().Request;
            request.Method = "GET";
            request.Path = new PathString("/Test");
            var contextAccessor = new HttpContextAccessor() { HttpContext = request.HttpContext };

            services.AddInstance<IHttpContextAccessor>(contextAccessor);

            if (actionContext != null)
            {
                var si = new ScopedInstance<ActionContext>();
                si.Value = actionContext;
                services.AddInstance<IScopedInstance<ActionContext>>(si);
            }

            if (requestTelemetry != null)
            {
                services.AddInstance<RequestTelemetry>(requestTelemetry);
            }

            IServiceProvider serviceProvider = services.BuildServiceProvider();
            contextAccessor.HttpContext.RequestServices = serviceProvider;

            return contextAccessor;
        }
    }
}
