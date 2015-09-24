namespace Microsoft.ApplicationInsights.AspNet.Tests.Helpers
{
    using System;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.AspNet.Hosting;
    using Microsoft.AspNet.Http;
    using Microsoft.AspNet.Mvc;
    using Microsoft.AspNet.Mvc.Abstractions;
    using Microsoft.AspNet.Mvc.Infrastructure;
    using Microsoft.Framework.DependencyInjection;
    using Microsoft.AspNet.Http.Internal;
    
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
                var si = new ActionContextAccessor();
                si.ActionContext = actionContext;
                services.AddInstance<IActionContextAccessor>(si);
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
