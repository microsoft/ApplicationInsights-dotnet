@echo off
@setlocal enabledelayedexpansion enableextensions

CALL buildRelease.cmd

set BuildRoot=%~dp0..\bin\release\

CALL "%VS140COMNTOOLS%..\IDE\CommonExtensions\Microsoft\TestWindow\vstest.console.exe" /UseVsixExtensions:true "%BuildRoot%Test\DependencyCollector\FunctionalTests\FuncTest\FuncTest.dll" "%BuildRoot%Test\PerformanceCollector\FunctionalTests\PerfCollector.FunctionalTests.dll" "%BuildRoot%Test\Web\FunctionalTests\FunctionalTests\Functional.dll" /logger:trx

