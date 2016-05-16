Microsoft Application Insights for ASP.NET Core applications
=============================================================

This repository has a code for [Application Insights monitoring](http://azure.microsoft.com/en-us/services/application-insights/) of [ASP.NET Core](https://github.com/aspnet/home) applications. Read about contribution policies on Application Insights Home [repository](https://github.com/microsoft/ApplicationInsights-home)

Recent updates
--------------
**Microsoft.ApplicationInsights.AspNet** is renamed to **Microsoft.ApplicationInsights.AspNetCore**. We have updated ASP.NET Core SDK to use .NET Core CLI runtime environment that picks the latest set of RC2 dependencies. Please note that this version will not support rc1 bits of DNX environment. Metrics stream is by default enabled in .NET Framework of ASP.NET Core.

Getting Started
---------------

[Application Insights monitoring](http://azure.microsoft.com/en-us/services/application-insights/) is a service that allows you to collect monitoring and diagnostics information about your application. [Getting started](https://github.com/Microsoft/ApplicationInsights-aspnet5/wiki/Getting-Started) guide shows how you can onboard your ASP.NET Core web application to use Application Insights SDK.

Application Insights collects lots of out-of-the-box information like requests, exceptions and usage. It also allows to configure additional data collection.  [Configure](https://github.com/Microsoft/ApplicationInsights-aspnet5/wiki/Configure) guide demonstrates the most common tasks you may want to do.


Repository structure
--------------------

```
root\
    ApplicationInsights.AspNetCore.sln - Main Solution

    src\
        ApplicationInsights.AspNetCore - Application Insights package

    test\
        ApplicationInsights.AspNetCore.Tests - Unit tests
        FunctionalTestUtils - test utilities for functional tests
        MVCFramework45.FunctionalTests - functional tests for MVC application
        WebApiShimFw46.FunctionalTests - functional tests for Web API application
        PerfTest - performance test
```

Developing
----------

## Pre-requisites
- [Visual Studio 2015 Update 2](https://www.visualstudio.com/en-us/downloads/visual-studio-2015-downloads-vs.aspx).
- [.NET Core CLI](https://github.com/dotnet/cli#installers-and-binaries).
- [.NET Core RC2 Tooling]()
- [Node.js](https://nodejs.org/download).
- [Git](http://git-scm.com/download).
- Source Code.
```
git clone https://github.com/Microsoft/ApplicationInsights-aspnetcore.git
```

## Building
From Visual Studio 2015
```
devenv ApplicationInsights.AspNetCore.sln
```

From Visual Studio 2015 Developer Command Prompt: Navigate to the source project folder and use the following commands to build the project:

```
dotnet restore &REM Restores the dependency packages
dotnet build &REM Builds the project
```
- If you get NPM package restore errors, make sure Node and NPM are added to PATH.
- If you get Bower pacakge restore errors, make sure Git is added to PATH.
- If you get dotnet package restore errors, make sure [.NET CLI is installed](https://github.com/dotnet/cli/blob/rel/1.0.0/Documentation/cli-installation-scenarios.md) and the nuget feeds are up to date.

## Branches
- We follow the [Git Flow](http://nvie.com/posts/a-successful-git-branching-model) model.
- [master](https://github.com/Microsoft/ApplicationInsights-aspnetcore/tree/master) has the _latest_ version released on [NuGet.org](https://www.nuget.org/packages/Microsoft.ApplicationInsights.AspNetCore).
- [develop](https://github.com/Microsoft/ApplicationInsights-aspnetcore/tree/develop) has the code for the _next_ release.

Running and writing tests
-------------------------
There are two sets of tests unit tests and functional tests. Please use unit tests for all features testing. The purpose of functional tests is just end-to-end validation of functionality on sample applications.


*Functional tests*
Functional tests are regular web applications with unit tests integrated into them. Application can be compiled as a regular web application as well as set of tests. Typical functional test will do the following:

1. Host the current project in In-Proc server.
2. Initialize application insights telemetry channel.
3. Initiate request to self hosted web application using HttpClient.
4. Check data received in telemetry channel.

Those are modifications made for regular web application to make it work this way:

Add dependencies to project.json:


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

*Running Tests*
You can run unit tests using Visual Studio.

You can run unit tests using .NET CLI from command line. Prerequisite to this is that you should make sure you have the latest version of .NET CLI. You can check the available runtime using the following command:
```
dotnet --version
```

If you are seeing that ```dotnet``` is not available (or defined), install .NET CLI: [.NET Core + CLI tools](https://github.com/dotnet/cli).

After that you can open a developer command prompt, navigate to each test folder and run:
```
dotnet restore &REM Restores the dependency packages
dotnet build &REM Builds the test project
dotnet test &REM Runs the tests within the test project
```

You can also run all tests using the following Powershell from root directory.

```
powershell .\RunTestsCore.ps1
```

