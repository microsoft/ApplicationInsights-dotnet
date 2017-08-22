@echo off
@setlocal enabledelayedexpansion enableextensions

-CALL buildRelease.cmd		

set BuildRoot=%~dp0..\bin\Release\
set VSTestPath=%PROGRAMFILES(x86)%\Microsoft Visual Studio\2017\Enterprise\Common7\IDE\CommonExtensions\Microsoft\TestWindow\vstest.console.exe

CALL "%VSTestPath%" /UseVsixExtensions:true "%BuildRoot%Test\DependencyCollector\FunctionalTests\FuncTest\FuncTest.dll" "%BuildRoot%Test\PerformanceCollector\FunctionalTests\PerfCollector.FunctionalTests.dll" "%BuildRoot%Test\Web\FunctionalTests\FunctionalTests\Functional.dll" /logger:trx

