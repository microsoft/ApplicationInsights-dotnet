namespace Microsoft.ApplicationInsights.AspNetCore.Tests.Helpers
{
    using System;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Http.Internal;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Abstractions;
    using Microsoft.AspNetCore.Mvc.Infrastructure;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Framework.DependencyInjection;
    
    public static class HttpContextAccessorHelper
    {
        public static HttpContextAccessor CreateHttpContextAccessor(RequestTelemetry requestTelemetry = null, ActionContext actionContext = null)
        {
            var services = new ServiceCollection();

            var request = new DefaultHttpContext().Request;
            request.Method = "GET";
            request.Path = new PathString("/Test");
            var contextAccessor = new HttpContextAccessor() { HttpContext = request.HttpContext };

            services.AddSingleton<IHttpContextAccessor>(contextAccessor);

            if (actionContext != null)
            {
                var si = new ActionContextAccessor();
                si.ActionContext = actionContext;
                services.AddSingleton<IActionContextAccessor>(si);
            }

            if (requestTelemetry != null)
            {
                services.AddSingleton<RequestTelemetry>(requestTelemetry);
            }

            IServiceProvider serviceProvider = services.BuildServiceProvider();
            contextAccessor.HttpContext.RequestServices = serviceProvider;

            return contextAccessor;
        }
    }
}
