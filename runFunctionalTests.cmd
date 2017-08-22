@echo off
@setlocal enabledelayedexpansion enableextensions

set BuildRoot=%~dp0..\bin\debug\
set VSTestPath=%PROGRAMFILES(x86)%\Microsoft Visual Studio\2017\Enterprise\Common7\IDE\CommonExtensions\Microsoft\TestWindow\vstest.console.exe

CALL "%VSTestPath%" /UseVsixExtensions:true "%BuildRoot%Test\DependencyCollector\FunctionalTests\FuncTest\FuncTest.dll"  /TestCaseFilter:"TestCategory=.NET Core 2.0" /logger:trx

