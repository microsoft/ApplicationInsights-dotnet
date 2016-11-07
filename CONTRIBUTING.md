# How to Contribute

If you're interested in contributing, take a look at the general [contributer's guide](https://github.com/Microsoft/ApplicationInsights-Home/blob/master/CONTRIBUTING.md) first.

## Unit Tests

To successfully run all the unit tests on your machine, make sure you've installed the following prerequisites:

* Visual Studio 2015 Community or Enterprise
* .NET 4.6

Several tests also require that you configure a strong name verification exception for Microsoft.WindowsAzure.ServiceRuntime.dll using the [Strong Name Tool](https://msdn.microsoft.com/en-us/library/k5b5tt23(v=vs.110).aspx). Run this command from the repository root to configure the exception (after building Microsoft.ApplicationInsights.Web.sln):

    "%ProgramFiles(x86)%\Microsoft SDKs\Windows\v8.1A\bin\NETFX 4.5.1 Tools\sn.exe" -Vr ..\bin\Debug\Src\WindowsServer\WindowsServer.Net40.Tests\Microsoft.WindowsAzure.ServiceRuntime.dll
    
Once you've installed the prerequisites and configured the strong name verification, execute the ```runUnitTests.cmd``` script in the repository root.

You can also run the tests within Visual Studio using the test explorer.

You can remove the strong name verification exception by running this command:

    "%ProgramFiles(x86)%\Microsoft SDKs\Windows\v8.1A\bin\NETFX 4.5.1 Tools\sn.exe" -Vu ..\bin\Debug\Src\WindowsServer\WindowsServer.Net40.Tests\Microsoft.WindowsAzure.ServiceRuntime.dll
    
## Functional Tests

To execute the functional tests, you need to install some additional prerequisites:

* IIS (Make sure Internet Information Services > World Wide Web Services > Application Development Features > ASP.NET 4.6 is enabled)
* SQL Express 2014
* Microsoft Azure Storage Emulator v4.2

You also need to configure the Azure Storage Emulator to use specific ports. Edit the file ```%ProgramFiles(x86)%\Microsoft SDKs\Azure\Storage Emulator\AzureStorageEmulator.exe.config``` and change the ```<services>``` section to the following:

    <StorageEmulatorConfig>
        <services>
          <service name="Blob" url="http://127.0.0.1:11000/"/>
          <service name="Queue" url="http://127.0.0.1:11001/"/>
          <service name="Table" url="http://127.0.0.1:11002/"/>
        </services>
    ...
    </StorageEmulatorConfig>

After you've done this, execute the ```runFunctionalTests.cmd``` script as administrator in the repository root. You can also run and debug the functional tests from Visual Studio by opening the solutions under the Test directory in the repository root.

If all or most of the Dependency Collector functional tests fail with messages like "Assert.AreEqual failed. Expected:<1>. Actual<0>.", these steps might help you troubleshoot:

* Open Test\DependencyCollector\FunctionalTests.sln
* Set a breakpoint in MyClassInitialize() after this line: ```aspx451TestWebApplicationWin32.Deploy(true);``` in [RDDTests.cs](https://github.com/Microsoft/ApplicationInsights-server-dotnet/blob/develop/Test/DependencyCollector/FunctionalTests/FuncTest/RDDTests.cs) and debug one of the failing tests.
* When the breakpoint is hit, go to this url in your browser: [http://localhost:789/ExternalCalls.aspx?type=sql](http://localhost:789/ExternalCalls.aspx?type=sql)
* If you see a 500 error or exception message, that is likely the cause of the test failures.

## Debugging the SDK

* Build the project using ```buildDebug.cmd``` 
* If the build was successful, you'll find that it generated NuGet packages in <repository root>\..\bin\Debug\NuGet
* If your change is confined to one of the nuget packages (say Web sdk), and you are developing on one of VNext branches, you can get the rest of the compatible nuget packages from [myget feed](https://www.myget.org/F/applicationinsights/)  
* Create a web application project to test the SDK on, and install the Microsoft.ApplicationInsights.Web NuGet package from the above directory
* In your web application, point your project references to Microsoft.AI.Web, Microsoft.AI.WindowsServer, Microsoft.AI.PerfCounterCollector and Microsoft.AI.DependencyCollector to those DLLs in the SDK debug output folder (this makes sure you get the symbol files and that your web application is updated when you recompile the SDK).
* From your web application, open the .cs file you want your breakpoint in and set it
* Run your web application

Your breakpoints should be hit now when your web application triggers them.
