Microsoft Application Insights for Asp.Net vNext applications
=============================================================

This repository has a code for [Application Insights monitoring](http://azure.microsoft.com/en-us/services/application-insights/) of [Asp.Net vNext](https://github.com/aspnet/home) applications. Read about contribution policies on Application Insights Home [repository](https://github.com/microsoft/appInsights-home)


Getting Started
---------------

[Application Insights monitoring](http://azure.microsoft.com/en-us/services/application-insights/) is a service that allows you to collect monitoring and diagnostics information about your application. [Getting started](https://github.com/Microsoft/ApplicationInsights-aspnetv5/wiki/Getting-Started) guide shows how you can onboard your Asp.Net v5 web application to use Application Insights SDK.

Application Insights collects lots of out-of-the-box information like requests, exceptions and usage. It also allows to configure additional data collection.  [Configure](https://github.com/Microsoft/ApplicationInsights-aspnetv5/wiki/Configure) guide demonstrates the most common tasks you may want to do.


Release notes
-------------

**[0.30.0.1-beta]**(https://github.com/Microsoft/ApplicationInsights-aspnetv5/releases/tag/v0.30.0.1-beta)

Preview of Application Insights. Works only with full framework. Will compile for Core framework, but will not work for it.

Repository structure
--------------------

```
root\
    ApplicationInsights.AspNet.sln - Main Solution

    src\
        ApplicationInsights.AspNet - Application Insights package

    test\
        ApplicationInsights.AspNet.Tests - Unit tests
        FunctionalTestUtils - test utilities for functional tests
        Mvc6Framework45.FunctionalTests - functional tests for MVC application
        WebApiShimFw46.FunctionalTests - functional tests for Web API application
        PerfTest - performance test
```

Developing
----------

## Pre-requisites
- [Visual Studio 2015 RC](https://www.visualstudio.com/en-us/downloads/visual-studio-2015-downloads-vs.aspx).
- [Node.js](https://nodejs.org/download).
- [Git](http://git-scm.com/download).
- Source Code.
```
git clone https://github.com/Microsoft/ApplicationInsights-aspnetv5.git
```

## Building
From Visual Studio 2015
```
devenv ApplicationInsights.AspNet.sln
```

From Visual Studio 2015 Developer Command Prompt.
```
msbuild ApplicationInsights.AspNet.sln
```
- If you get NPM package restore errors, make sure Node and NPM are added to PATH.
- If you get Bower pacakge restore errors, make sure Git is added to PATH.
- If you get Dnu package restore errors, make sure [Dnx is installed](https://github.com/dotnet/coreclr/blob/master/Documentation/get-dotnetcore-dnx-windows.md) or open the solution in Visual Studio 2015, which will take care of this.

## Branches
- We follow the [Git Flow](http://nvie.com/posts/a-successful-git-branching-model) model.
- [master](https://github.com/Microsoft/ApplicationInsights-aspnetv5/tree/master) has the _latest_ version released on [NuGet.org](https://www.nuget.org/packages/Microsoft.ApplicationInsights.AspNet).
- [develop](https://github.com/Microsoft/ApplicationInsights-aspnetv5/tree/develop) has the code for the _next_ release.

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
"xunit.runner.aspnet": "2.0.0-aspnet-beta5-*",
```

and test command:

```
"test": "xunit.runner.aspnet"
```

Add this initialization logic to Startup.cs:

```
services.AddFunctionalTestTelemetryChannel();
```

*Running Tests*
Open a developer command prompt, navigate to project folder and run:
```
dnx . test
```
