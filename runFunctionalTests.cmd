@echo off
@setlocal enabledelayedexpansion enableextensions

CALL buildRelease.cmd

set BuildRoot=%~dp0..\bin\release\

CALL "%VS140COMNTOOLS%..\IDE\CommonExtensions\Microsoft\TestWindow\vstest.console.exe" /UseVsixExtensions:true "%BuildRoot%Web\FunctionalTests\FunctionalTests\Functional.dll" "%BuildRoot%PerformanceCollector\PerformanceCollector.Tests\FunctionalTests\PerfCollector.FunctionalTests.dll" "%BuildRoot%Web\DependencyCollector\FunctionalTests\FuncTest\FuncTest.dll" /logger:trx

