@echo off

FOR %%A IN (dnx-clr-win-x86.1.0.0-beta4 dnx-coreclr-win-x86.1.0.0-beta4) DO (
	SET DnxPath=%USERPROFILE%\.dnx\runtimes\%%A\bin\dnx.exe

	ECHO Execting tests on DNX: "%DnxPath%

	%DnxPath% .\Microsoft.ApplicationInsights.AspNet.Tests test -nologo -diagnostics
	%DnxPath% .\Mvc6Framework45.FunctionalTests test -nologo -diagnostics
	%DnxPath% .\WebApiShimFw46.FunctionalTests test -nologo -diagnostics
)