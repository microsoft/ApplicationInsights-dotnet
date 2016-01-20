# Microsoft Application Insights for ASP.NET
.NET Server SDK

## Get started

To use this SDK, you'll need a subscription to [Microsoft Azure](https://azure.com). (There's a free package.)

To add Application Insights to your project in Visual Studio

* If it's a new web project, make sure "Add Application Insights to Project" is selected.
* If it's an existing project, right click the project in Solution Explorer, and choose Add Application Insights.

Run your project, and then open your Application Insights resource in the [Azure Preview Portal](https://portal.azure.com) and look for events.

[Learn more.](https://azure.microsoft.com/en-us/documentation/articles/app-insights-asp-net/)

## To build

* Visual Studio 2015 Enterprise or Visual Studio 2013 Ultimate with Update 4 or later
* Clone the Git repository
* Open Visual Studio solution (devenv Web\Microsoft.ApplicationInsights.Web.sln)
* Build solution in Visual Studio or 
* Run script ```buildDebug.cmd``` or ```buildRelease.cmd```

## To run tests

See the wiki for instructions:
https://github.com/Microsoft/ApplicationInsights-server-dotnet/wiki/Running-Tests

