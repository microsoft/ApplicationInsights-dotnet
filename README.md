![Build Status](https://mseng.visualstudio.com/DefaultCollection/_apis/public/build/definitions/96a62c4a-58c2-4dbb-94b6-5979ebc7f2af/2678/badge)

[![Nuget](https://img.shields.io/nuget/vpre/Microsoft.ApplicationInsights.Web.svg)](http://nuget.org/packages/Microsoft.ApplicationInsights.Web)

# Microsoft Application Insights for Web Applications

This repository has code for the Web Application SDK for Application Insights. [Application Insights][AILandingPage] is a service that allows developers ensure their applications are available, performing, and succeeding. This SDK provides the ability to auto-collect data such as dependency calls, requests and server performance counters in .NET web applications. 

## Get started

To send data to Application Insights, you need an instrumentation key that you can get by creating an Application Insights resource in the [Azure Preview Portal][AzurePortal] or adding Application Insights to your project in Visual Studio.

To add Application Insights to your project in Visual Studio 

* If it's a new web project, make sure "Add Application Insights to Project" is selected.
* If it's an existing project, right click the project in Solution Explorer, and choose Add Application Insights.

For detailed instructions, see [this][AddInVS] article.

Run your project, and then open your Application Insights resource in the [Azure Preview Portal][AzurePortal] and look for events. [Learn more.][WebDocumentation]

The latest stable and pre-release versions of this library are available on [NuGet][WebNuGet].

## To build

* Visual Studio 2015 Community or Enterprise
* Clone the Git repository
* Open Visual Studio solution (devenv Web\Microsoft.ApplicationInsights.Web.sln)
* Build solution in Visual Studio

If you prefer using build scripts, run ```buildDebug.cmd``` or ```buildRelease.cmd```

## Branches
- [master][master] contains the *latest* published release located on [NuGet][WebNuGet].
- [develop][develop] contains the code for the *next* release.

## Contributing

We strongly welcome and encourage contributions to this project. Please read the [contributor's guide][ContribGuide]. If making a large change we request that you open an [issue][GitHubIssue] first. If we agree that an issue is a bug, we'll add the "bug" label, and issues that we plan to fix are labeled with an iteration number. We follow the [Git Flow][GitFlow] approach to branching.

[Azure]: https://azure.com/
[AILandingPage]: http://azure.microsoft.com/services/application-insights/
[AzurePortal]: https://ms.portal.azure.com/#gallery/Microsoft.AppInsights/
[WebDocumentation]: https://azure.microsoft.com/en-us/documentation/articles/app-insights-asp-net/#monitor
[master]: https://github.com/Microsoft/ApplicationInsights-server-dotnet/tree/master/
[develop]: https://github.com/Microsoft/ApplicationInsights-server-dotnet/tree/develop/
[GitFlow]: http://nvie.com/posts/a-successful-git-branching-model/
[ContribGuide]: https://github.com/Microsoft/ApplicationInsights-server-dotnet/blob/develop/CONTRIBUTING.md/
[GitHubIssue]: https://github.com/Microsoft/ApplicationInsights-server-dotnet/issues/
[WebNuGet]: https://www.nuget.org/packages/Microsoft.ApplicationInsights.Web/
[MyGet]:http://myget.org/gallery/applicationinsights/
[AddInVS]:https://azure.microsoft.com/en-us/documentation/articles/app-insights-asp-net/#ide
