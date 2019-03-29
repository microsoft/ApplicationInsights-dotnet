## NuGet packages

- [Microsoft.ApplicationInsights.AspNetCore](https://www.nuget.org/packages/Microsoft.ApplicationInsights.AspNetCore/)
[![Nuget](https://img.shields.io/nuget/vpre/Microsoft.ApplicationInsights.AspNetCore.svg)](https://nuget.org/packages/Microsoft.ApplicationInsights.AspNetCore)

Windows: [![Build Status](https://mseng.visualstudio.com/AppInsights/_apis/build/status/ChuckNorris/AI_ASPNETCore_Develop?branchName=develop)](https://mseng.visualstudio.com/AppInsights/_build/latest?definitionId=3717&branchName=develop)

Linux :[![Build Status](https://mseng.visualstudio.com/AppInsights/_apis/build/status/ChuckNorris/AI-AspNetCoreSDK-develop-linux?branchName=develop)](https://mseng.visualstudio.com/AppInsights/_build/latest?definitionId=6273&branchName=develop)


Microsoft Application Insights for ASP.NET Core applications
=============================================================

This repository has a code for [Application Insights monitoring](http://azure.microsoft.com/en-us/services/application-insights/) of [ASP.NET Core](https://github.com/aspnet/home) applications. Read about contribution policies on Application Insights Home [repository](https://github.com/microsoft/ApplicationInsights-home)

Getting Started
---------------

[Application Insights monitoring](http://azure.microsoft.com/en-us/services/application-insights/) is a service that allows you to collect monitoring and diagnostics information about your application. The [getting started](https://github.com/Microsoft/ApplicationInsights-aspnet5/wiki/Getting-Started) guide shows how you can onboard your ASP.NET Core web application to use the Application Insights SDK.

Application Insights collects a lot of information out-of-the-box such as requests, dependencies, exceptions, and usage. It also allows you to configure additional data collection.  The [configure](https://github.com/Microsoft/ApplicationInsights-aspnet5/wiki/Configure) guide demonstrates the most common tasks you may want to do.


Repository structure
--------------------

```
root\
    ApplicationInsights.AspNetCore.sln - Main Solution

    src\
        ApplicationInsights.AspNetCore - Application Insights package

    test\
        ApplicationInsights.AspNetCore.Tests - Unit tests
        FunctionalTestUtils - Test utilities for functional tests
        MVCFramework.FunctionalTests - functional tests for MVC application targetting NetCore1.1,NetCore2.0 and NET45
        WebApi.FunctionalTests - functional tests for Web API application targetting NetCore1.1,NetCore2.0 and NET45
		EmptyApp.FunctionalTests - functional tests for an Empty application targetting NetCore1.1,NetCore2.0 and NET45
        PerfTest - performance test
```

Developing
----------
To successfully build the sources on your machine, make sure you've installed the following prerequisites:
* Visual Studio 2017 Community or Enterprise. Please make sure to install all the latest updates to Visual Studio
* .NET Framework 4.6
* .NET Core SDK 1.1.7
* .NET Core SDK 2.0 or above.(https://www.microsoft.com/net/download/windows)

## Building
Once you've installed the prerequisites execute ```buildDebug.cmd``` or ```buildRelease.cmd``` script in the repository root to build the project locally.
You can also open the solution in Visual Studio and build the ApplicationInsights.AspNetCore.sln solution directly.

## Testing/Debugging
Execute the ```RunTests.cmd``` script in the repository root.

You can also open the solution in Visual Studio and run tests directly from Visual Studio Test Explorer. However, as the tests has multiple targets, Test Explorer only shows the first target
from <TargetFrameworks> in .csproj. To debug/run tests from a particular TargetFramework with Visual Studio, only option is to re-arrange the <TargetFrameworks>
such that the intented target comes first. This is a Visual Studio limitation and is likely removed in the future.


Running and writing tests
-------------------------
There are two sets of tests unit tests and functional tests. Please use unit tests for all features testing. The purpose of functional tests is just end-to-end validation of functionality on sample applications.

*Functional tests*
Functional tests are regular web applications with unit tests integrated into them. Application can be compiled as a regular web application as well as set of tests. Typical functional tests will do the following:

1. Host the current project in In-Proc server.
2. Initialize application insights telemetry channel.
3. Initiate request to self hosted web application using HttpClient.
4. Check data received in telemetry channel.

The following are modifications made to a regular web application to make it work this way:

Add dependencies to .csproj:

```
"FunctionalTestUtils": "1.0.0-*",
"dotnet.test.xunit": "1.0.0-*",
"xunit": "2.1.0"
```

and test command:

```
"test": "xunit"
```

Add this initialization logic to Startup.cs:

```
services.AddFunctionalTestTelemetryChannel();
```


## Branches
- We follow the [Git Flow](http://nvie.com/posts/a-successful-git-branching-model) model.
- [master](https://github.com/Microsoft/ApplicationInsights-aspnetcore/tree/master) has the _latest_ version released on [NuGet.org](https://www.nuget.org/packages/Microsoft.ApplicationInsights.AspNetCore).
- [develop](https://github.com/Microsoft/ApplicationInsights-aspnetcore/tree/develop) has the code for the _next_ release.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/). For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.
