Microsoft Application Insights for Asp.Net vNext applications
=============================================================

This repository has a code for [Application Insights monitoring](http://azure.microsoft.com/en-us/services/application-insights/) of [Asp.Net vNext](https://github.com/aspnet/home) applications. Read about contribution policies on Application Insights Home [repository](https://github.com/microsoft/appInsights-home)


Getting Started
---------------

[Application Insights monitoring](http://azure.microsoft.com/en-us/services/application-insights/) is a service that allows you to collect monitoring and diagnostics information about your application. [Getting started](https://github.com/Microsoft/ApplicationInsights-aspnetv5/wiki/Getting-Started) guide shows how you can onboard your Asp.Net v5 web application to use Application Insights SDK.

Application Insights collects lots of out-of-the-box information like requests, exceptions and usage. It also allows to configure additional data collection.  [Configure](https://github.com/Microsoft/ApplicationInsights-aspnetv5/wiki/Configure) guide demonstrates the most common tasks you may want to do.


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
1. Repository: https://github.com/microsoft/AppInsights-aspnetv5
2. Asp.Net information: https://github.com/aspnet/home
3. SDK is build with beta4 asp.net nuget packages so it cannot run with Visual Studio 2015 CTP6. You'll need to use dnx directly like explained in this [article](http://www.dzone.com/articles/developing-and-self-hosting). Please note, that recently "k" was renamed to "dnx" - you'll need to adjust instructions accordingly.

Development is in [develop](https://github.com/Microsoft/ApplicationInsights-aspnetv5/tree/develop) branch. Master branch has latest stable release.

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
