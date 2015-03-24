Microsoft Application Insights for Asp.Net vNext applications
=============================================================

This repository has a code for [Application Insights monitoring](http://azure.microsoft.com/en-us/services/application-insights/) of [Asp.Net vNext](https://github.com/aspnet/home) applications. Read about contrubution policies on Application Insights Home [repository](https://github.com/microsoft/appInsights-home)


Getting Started
---------------

Add NuGet feed http://appinsights-aspnet.azurewebsites.net/nuget/. It has NuGet: Microsoft.ApplicationInsights.AspNet.

For standard Asp.Net template you need to modify four files (this will be the default template instrumentation in future).

***project.json*** 
Add new reference:
```
"Microsoft.ApplicationInsights.AspNet": "1.0.0.0-alpha"
```

***config.json*** 
Configure instrumentation key:
```
 "ApplicationInsights": {
 	"InstrumentationKey": "11111111-2222-3333-4444-555555555555"
 }
```

***Startup.cs***
Add service:
```
services.AddApplicationInsightsTelemetry(Configuration);
```

Add middleware and configure developer mode: 

```
// Add Application Insights monitoring to the request pipeline as a very first middleware.
app.UseApplicationInsightsRequestTelemetry();
...
// Add the following to the request pipeline only in development environment.
if (string.Equals(env.EnvironmentName, "Development", StringComparison.OrdinalIgnoreCase))
{
	app.SetApplicationInsightsTelemetryDeveloperMode();
}
...
// Add Application Insights exceptions handling to the request pipeline.
app.UseApplicationInsightsExceptionTelemetry();
```

***_Layout.cshtml***
Define using and injection:

```
@using Microsoft.ApplicationInsights.AspNet
@inject Microsoft.ApplicationInsights.DataContracts.RequestTelemetry RequestTelelemtry
```

And insert HtmlHelper to the end of ```<head>``` section:

```
	@Html.ApplicationInsightsJavaScriptSnippet(RequestTelelemtry.Context.InstrumentationKey);
</head>
```

Developing
----------
1. Repository (private now): https://github.com/microsoft/AppInsights-aspnetv5
2. Asp.Net information: https://github.com/aspnet/home
3. VS 2015 installation: 
 - *(recommended by [Anastasia](https://github.com/abaranch))*: http://blogs.msdn.com/b/visualstudioalm/archive/2014/06/04/visual-studio-14-ctp-now-available-in-the-virtual-machine-azure-gallery.aspx
 - You can just install it on your machine: https://www.visualstudio.com/en-us/news/vs2015-vs.aspx


4. Make sure you have these (and only these) feeds configured in Visual Studio:
 - https://www.myget.org/F/aspnetvnext
 - http://appinsights-aspnet.azurewebsites.net/nuget/


