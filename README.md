# Microsoft Application Insights for Web Applications

This repository has code for the Web Application SDK for Application Insights. [Application Insights][AILandingPage] is a service that allows developers ensure their application are available, performing, and succeeding. This SDK provides the ability to auto-collect data such as dependency calls, requests and server performance counters in .NET web applications. 

## Get started

To use this SDK, you'll need a subscription to [Microsoft Azure][Azure]. (There's a free package.)

To add Application Insights to your project in Visual Studio

* If it's a new web project, make sure "Add Application Insights to Project" is selected.
* If it's an existing project, right click the project in Solution Explorer, and choose Add Application Insights.

Run your project, and then open your Application Insights resource in the [Azure Preview Portal][AzurePortal] and look for events. [Learn more.][WebDocumentation]

The latest pre-release version of this library is also available on [NuGet][WebNuGet].

## To build

* Visual Studio 2015 Enterprise or Visual Studio 2013 Ultimate with Update 4 or later
* Clone the Git repository
* Open Visual Studio solution (devenv Web\Microsoft.ApplicationInsights.Web.sln)
* Build solution in Visual Studio or 
* Run script ```buildDebug.cmd``` or ```buildRelease.cmd```

## To run tests

See the wiki for instructions:
https://github.com/Microsoft/ApplicationInsights-server-dotnet/wiki/Running-Tests

## Branches
- [master][master] contains the *latest* published release located on [NuGet][WebNuGet].
- [develop][develop] contains the code for the *next* release.

## Contributing

We strongly welcome and encourage contributions to this project. Please read the [contributor's guide][ContribGuide] located in the ApplicationInsights-Home repository. If making a large change we request that you open an [issue][GitHubIssue] first. We follow the [Git Flow][GitFlow] approach to branching. 

[Azure]: https://azure.com/
[AILandingPage]: http://azure.microsoft.com/services/application-insights/
[AzurePortal]: https://portal.azure.com/
[WebDocumentation]: https://azure.microsoft.com/en-us/documentation/articles/app-insights-asp-net/
[master]: https://github.com/Microsoft/ApplicationInsights-server-dotnet/tree/master/
[develop]: https://github.com/Microsoft/ApplicationInsights-server-dotnet/tree/develop/
[GitFlow]: http://nvie.com/posts/a-successful-git-branching-model/
[ContribGuide]: https://github.com/Microsoft/ApplicationInsights-Home/blob/master/CONTRIBUTING.md/
[GitHubIssue]: https://github.com/Microsoft/ApplicationInsights-server-dotnet/issues/
[WebNuGet]: https://www.nuget.org/packages/Microsoft.ApplicationInsights.Web/
