# How to Contribute

If you're interested in contributing, take a look at the general [contributer's guide](https://github.com/Microsoft/ApplicationInsights-Home/blob/master/CONTRIBUTING.md) first and continue here.

## Unit Tests

To successfully run all the unit tests on your machine, make sure you've installed the following prerequisites:

* Visual Studio 2017 Community or Enterprise
* .NET 4.6
* .NET Core 2.0

Several tests also require that you configure a strong name verification exception for Microsoft.WindowsAzure.ServiceRuntime.dll using the [Strong Name Tool](https://msdn.microsoft.com/en-us/library/k5b5tt23(v=vs.110).aspx). Run this command from the repository root to configure the exception (after building Microsoft.ApplicationInsights.Web.sln):

    "%ProgramFiles(x86)%\Microsoft SDKs\Windows\v8.1A\bin\NETFX 4.5.1 Tools\sn.exe" -Vr ..\bin\Debug\Src\WindowsServer\WindowsServer.Net40.Tests\Microsoft.WindowsAzure.ServiceRuntime.dll
    
Once you've installed the prerequisites and configured the strong name verification, execute the ```runUnitTests.cmd``` script in the repository root.

You can also run the tests within Visual Studio using the test explorer. If test explorer is not showing all the tests, please make sure you have installed all updates to Visual Studio.

You can remove the strong name verification exception by running this command:

    "%ProgramFiles(x86)%\Microsoft SDKs\Windows\v8.1A\bin\NETFX 4.5.1 Tools\sn.exe" -Vu ..\bin\Debug\Src\WindowsServer\WindowsServer.Net40.Tests\Microsoft.WindowsAzure.ServiceRuntime.dll
    
## Functional Tests

Pre-requisites
	To execute the functional tests, you need to install some additional prerequisites:

	For Web and PerformanceCollector IIS Express should be installed.
		
	For Dependency Collector, you need to install Docker for windows as these tests need several additional dependencies to be deployed like SQL Server, Azure Emulator etc, and these are deployed as Docker containers. 
		* Docker for Windows (https://docs.docker.com/docker-for-windows/install/). After installation switch Docker engine to Windows Containers.(https://blogs.msdn.microsoft.com/webdev/2017/09/07/getting-started-with-windows-containers/)

After you've done this, execute the ```runFunctionalTests.cmd``` script as administrator in the repository root. You can also run and debug the functional tests from Visual Studio by opening the solutions under the Test directory in the repository root.

If all or most of the Dependency Collector functional tests fail with messages like "Assert.AreEqual failed. Expected:<1>. Actual<0>." or "All apps are not healthy", then its likely that Docker installation has some issues.
Please make sure you can run docker run hello-world successfully to confirm that your machine is Docker ready.
Also, the very first time DependencyCollector tests are run, all Docker images are downloaded from web and this could potentially take an hour or so. This is only one time per machine.

## Debugging the functional tests
It is important to note that since the test application is deployed as a separate process/container, debugging the tests itself will not help debug the application code. A debugger need to be attached
to the process hosting the Application. (ISExpress or IIS)
The test apps refers to the Web SDK assemblies from your local build. After making the changes to product code, build locally (from Visual Studio or using ```buildDebug.cmd```). Then start the test application from its publish 
folder in either IISExpress or IIS, and attach debugger to it. Open the .cs file you want your breakpoint in and set it. Now triggering a request to the application will hit the breakpoint.
The exact request to be triggered depends on what you are doing. If investigating functional test failures locally, then the tests logs should contain the url it hit to trigger scenarios.

Dependency Collector tests deploy the test apps, along with dependencies (Fake Ingestion, SQL etc) to Docker containers inside same network, so that apps can access the dependencies with their names. However, if 
the test apps are deployed to IIS or IISExpress, then they are outside the Docker virtual network of dependencies, and so it won't be able to access dependencies without using their IP Address. This is a Docker for windows limitation, and could be fixed in future.
Until then, the test app need to address the dependencies using their IP Address. Instead of manually finding IP Addresses and replacing containers names with IP Address, its easy to just run the following script.
This uses Docker commands to determine the IP Addresses, and replaces them in the necessary configs.
"<repo-root>\bin\Debug\Test\E2ETests\E2ETests\replacecontainernamewithip.ps1"

Following pre-requisite is needed to deploy to IIS locally.
* IIS (Make sure Internet Information Services > World Wide Web Services > Application Development Features > ASP.NET 4.6 is enabled)


## Debugging the SDK in general

* Build the project using ```buildDebug.cmd``` 
* If the build was successful, you'll find that it generated NuGet packages in <repository root>\..\bin\Debug\NuGet
* If your change is confined to one of the nuget packages (say Web sdk), and you are developing on one of VNext branches, you can get the rest of the compatible nuget packages from [myget feed](https://www.myget.org/F/applicationinsights/)  
* Create a web application project to test the SDK on, and install the Microsoft.ApplicationInsights.Web NuGet package from the above directory
* In your web application, point your project references to Microsoft.AI.Web, Microsoft.AI.WindowsServer, Microsoft.AI.PerfCounterCollector and Microsoft.AI.DependencyCollector to those DLLs in the SDK debug output folder (this makes sure you get the symbol files and that your web application is updated when you recompile the SDK).
* From your web application, open the .cs file you want your breakpoint in and set it
* Run your web application
Your breakpoints should be hit now when your web application triggers them.