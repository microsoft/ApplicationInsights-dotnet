# How to Contribute

If you're interested in contributing, take a look at the general [contributer's guide](https://github.com/Microsoft/ApplicationInsights-Home/blob/master/CONTRIBUTING.md) first and continue here.


## Solutions

- Everything.sln - this will build all projects and tests.
- ProjectsForSigning.sln - this builds all shipping projects.
- BASE\Microsoft.ApplicationInsights.sln - this builds the Base SDK and ServerTelemetryChannel.
- WEB\Microsoft.ApplicationInsights.Web.sln - this builds the ASP.Net projects.
- NETCORE\ApplicationInsights.AspNetCore.sln - this builds the .Net Core projects.
- LOGGING\Logging.sln - this builds the logging adapters.



## Build

To successfully build the sources on your machine, make sure you've installed the following prerequisites:
* Visual Studio 2017 Community or Enterprise
* .NET 4.6
* .NET Core SDK 1.1.7
* .NET Core SDK 2.0 or above.(https://www.microsoft.com/net/download/windows)

If using Azure VM, the following image from Microsoft contains the above pre-requisites already installed.

_Visual Studio Enterprise 2017 (latest release) on Windows Server 2016._

Once you've installed the prerequisites execute either ```buildDebug.cmd``` or ```buildRelease.cmd``` script in the repository root to build the project (excluding functional tests) locally.

```buildRelease.cmd``` also runs StlyeCop checks, and is required before merging any pull requests.

You can also open the solutions in Visual Studio and build directly from there.


## Unit Tests

Several tests require that you configure a strong name verification exception for Microsoft.WindowsAzure.ServiceRuntime.dll using the [Strong Name Tool](https://msdn.microsoft.com/en-us/library/k5b5tt23(v=vs.110).aspx). 

Using the Developer Command Prompt as Administrator, run this command from the repository root to register the assembly for verification skipping. (after building Microsoft.ApplicationInsights.Web.sln)

    "%ProgramFiles(x86)%\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.7.1 Tools\sn.exe" -Vr ..\bin\Debug\Src\WindowsServer\WindowsServer.Net45.Tests\Microsoft.WindowsAzure.ServiceRuntime.dll
	
(Depending on you OS version, the above exe may be located in different folder. Modify the path according to local path).	
    
Once you've configured the strong name verification, execute the ```runUnitTests.cmd``` script in the repository root.

If the script fail with errors like unable to find path to Visual Studio Test runner, please edit the helper script to match you local installation of Visual Studio.

You can also run the tests within Visual Studio using the test explorer. If test explorer is not showing all the tests, please make sure you have installed all updates to Visual Studio.

You can remove the strong name verification exception by running this command as Administrator:

    "%ProgramFiles(x86)%\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.7.1 Tools\sn.exe" -Vr ..\bin\Debug\Src\WindowsServer\WindowsServer.Net45.Tests\Microsoft.WindowsAzure.ServiceRuntime.dll

## Functional Tests
It is recommended to rely on unit tests to test functionalities wherever possible. For doing end-to-end validation, functional tests exists for all the modules. Unless doing significant changes,
it is not required to run func-tests locally. All the tests, including functional tests, are automatically run when a PR is submitted.

These tests works like described below:

Functional tests contain test apps which refers to the product dlls from the local build. Tests deploy the Test apps to IIS/Docker and http requests are fired against it to trigger various scenarios.
Tests apps are modified to send telemetry to a fake ingestion endpoint controlled by tests. Tests then validate the telemetry received by this endpoint.

Pre-requisites:

To execute the functional tests, you need to install some additional prerequisites:

For Web and PerformanceCollector tests IIS Express should be installed.
		
For Dependency Collector, you need to install Docker for windows as these tests need several additional dependencies to be deployed like SQL Server, Azure Emulator etc, and these are deployed as Docker containers. 
		Docker for Windows (https://docs.docker.com/docker-for-windows/install/). 		
		After installation switch Docker engine to Windows Containers.(https://blogs.msdn.microsoft.com/webdev/2017/09/07/getting-started-with-windows-containers/)
		And finally, make sure you can run ```docker run hello-world``` successfully to confirm that your machine is Docker ready.
				
Running functional tests:

Before running the functional tests, the product code should be built following 'Build' instructions above.

After building, open the respective solutions from locations given below, build the solution. Tests will appear in Visual Studio Test Explorer and can be run from there.

The following solutions contains the functional tests for various features.

"\Test\Web\FunctionalTests.sln" -- Functional tests using apps onboarded with the nuget Microsoft.ApplicationInsights.Web
Helper script to build product and run all tests in this solution - ```runFunctionalTestsWeb```

"..\bin\Debug\Test\Web\FunctionalTests" -- Binary location for Test and Test apps.

"\Test\PerformanceCollector\FunctionalTests.sln" -- Functional tests using apps onboarded with the nuget Microsoft.ApplicationInsights.PerfCounterCollector
Helper script to build product and run all tests in this solution - ```runFunctionalTestsPerfCollectorAndQuickPulse```

"..\bin\Debug\Test\PerformanceCollector\FunctionalTests" -- Binary location for Test and Test apps.

"\Test\E2ETests\DependencyCollectionTests.sln" -- Functional tests using apps onboarded with the nuget Microsoft.ApplicationInsights.DependencyCollector
Helper script to build product and run all tests in this solution - ```runFunctionalTestsDependencyCollector```

"..bin\Debug\Test\E2ETests" -- Binary location for Test and Test apps.

Special Notes regarding DependencyCollectionTests
1. All Docker images are downloaded from internet when ran for first time and this could take several minutes (depends on network speed as **around 20GB will be downloaded on first time on a machine**.). Tests may appear hung during this time. 
2. If using Visual Studio Test Explorer to run tests, group the tests by namespace and run each namespaces separately to avoid test conflicts. ```runFunctionalTestsDependencyCollector``` does this automatically.


Edit the helper scripts to change between 'Release' and 'Debug' as per your build.
```runAllFunctionalTests.cmd``` script builds the product and runs all the above functional tests.

Its is important to note that functional tests do not trigger product code build, so explicit build of product code is required before running functional tests.
A typical work flow would be make-produce-change followed by build-product followed by build-functest-solution and finally run-func-tests. (This helpers scripts does this.)

## Known issues/workarounds with running functional tests.

If any tests fail, please retry first to see if it helps. If not, try one of the known issues below. 

Tests fail with error like "It was not possible to find any compatible framework version The specified framework 'Microsoft.NETCore.App', version '1.0.4' was not found"

Workaround: Install .NET Core SDK 1.1.7.

Web and PerformanceCollector fails with error related to 'Port conflicts' - its possible that some prior tests has not released ports. 
	Workaround - Kill all running IISExpress processes and re-run tests.
	
All/many functional tests fail with error "Incorrect number of items. Expected: 1 Received: 0" when ran from Visual Studio IDE. 
Look for warnings in Visual Studio output window which contains errors like 'Unable to copy dll file due to file being locked..' etc. 

Workarounds: 
1. Close all instances of Visual Studio/cmd windows where scripts were run and retry.
2. One can use advanced tools like 'process explorer' to find out which process is locking files. Kill the process and retry.
3. Delete bin folder from repository root and rebuild. 
4. Restart machine if none of the above helps. 

Dependency Collector functional tests fail with messages like "Assert.AreEqual failed. Expected:<1>. Actual<0>." or "All apps are not healthy", then its likely that Docker installation has some issues.
	
Workaround if you are trying first time - Make sure you can run ```docker run hello-world``` successfully to confirm that your machine is Docker ready. Also, the very first time DependencyCollector tests are run, all Docker images are downloaded from web and this could potentially take an hour or so. This is only one time per machine.	
Alternate workaround if you have previously run the tests successfully at least once - execute the ```dockercleanup.ps1``` from repository root to cleanup any containers from prior runs.

All DependencyCollectionTests fail at initialization stage itself with error 'All apps are not healthy'. In the logs you'll find that Docker container has exited with some error codes. Eg: "Exited (2147943452) 53 seconds ago".
If this error occurs execute ```dockercleanup.ps1``` from repository root, and re-run the tests.

The test code intentionally does not clean up the containers it spun up. This is to enable fast re-runs of the tests. If the Test App code is changed, then Docker-Compose will detect it, and re-build the container.
If you want to do clean up all the containers created by the test, execute the ```dockercleanup.ps1``` from repository root. This is typically required if tests were aborted in the middle of a run for some reason.

After retrying, it tests still fail, please clear the binaries folder and rebuild the product solution and test solution and run the tests again.

If none of the above helps, please open an issue in Github describing the problem.

## Debugging the functional tests.
It is important to note that since the test application is deployed as a separate process/container, debugging the tests itself will not help debug the application code. A debugger need to be attached
to the process hosting the Application, IISExpress or IIS, after deploying the application there.

The test apps refers to the Web SDK assemblies from your local build. After making the changes to product code, build locally (from Visual Studio or using ```buildDebug.cmd```). Then build and start the test application from its publish folder in either IISExpress or IIS, and attach debugger to it. Open the .cs file you want your breakpoint in and set it. Now triggering a request to the application will hit the breakpoint.
The exact request to be triggered depends on what you are doing. If investigating functional test failures locally, then the tests logs should contain the url it hit to trigger scenarios.

Dependency Collector tests deploy the test apps, along with dependencies (Fake Ingestion, SQL etc) to Docker containers inside same Docker virtual network, so that apps can access the dependencies with their names. However, if 
the test apps are deployed to IIS or IISExpress, then they are outside the Docker virtual network of dependencies, and so it won't be able to access dependencies without using their IP Address. This is a Docker for windows limitation, and could be fixed in future.
Until then, the test app need to address the dependencies using their IP Address. Instead of manually finding IP Addresses and replacing containers names with IP Address, its easy to just run the following script.
This uses Docker commands to determine the IP Addresses, and replaces them in the necessary configs.
"<repo-root>\bin\Debug\Test\E2ETests\E2ETests\replacecontainernamewithip.ps1"

Following pre-requisite is needed to deploy to IIS locally.
* IIS (Make sure Internet Information Services > World Wide Web Services > Application Development Features > ASP.NET 4.6 is enabled)


## Debugging the SDK in general (How to test Application Insights from local build in any Test App)

* Build the project using ```buildDebug.cmd``` 
* If the build was successful, you'll find that it generated NuGet packages in <repository root>\..\bin\Debug\NuGet
* If your change is confined to one of the nuget packages (say Web sdk), and you are developing on one of VNext branches, you can get the rest of the compatible nuget packages from [myget feed](https://www.myget.org/F/applicationinsights/)  
* Create a web application project to test the SDK on, and install the Microsoft.ApplicationInsights.Web NuGet package from the above directory
* In your web application, point your project references to Microsoft.AI.Web, Microsoft.AI.WindowsServer, Microsoft.AI.PerfCounterCollector and Microsoft.AI.DependencyCollector to those DLLs in the SDK debug output folder (this makes sure you get the symbol files and that your web application is updated when you recompile the SDK).
* From your web application, open the .cs file you want your breakpoint in and set it
* Run your web application.

Your breakpoints should be hit now when your web application triggers them.
