@echo off

call "%VS140COMNTOOLS%\VsDevCmd.bat"
SET ProjectName=Msbuild.All
REM Set the configuration to either release or debug based on the requirement. Accepts this as a parameter to build.cmd. By default it is chosen to be Release.
SET Configuration=Release
if NOT "%1"=="" (SET Configuration=%1)  
SET Platform=Any CPU
SET NugetOrg_Feed=https://www.nuget.org/api/v2/
SET IsOfficialBuild=False
SET DefaultFeed=https://www.nuget.org/api/v2/

msbuild dirs.proj /nologo /m:1  /fl /flp:logfile="%ProjectName%.%Platform%.log";v=d /flp1:logfile="%ProjectName%.%Platform%.wrn";warningsonly /flp2:logfile="%ProjectName%.%Platform%.err";errorsonly /p:Configuration=%Configuration% /p:Platform="%Platform%" /p:RunCodeAnalysis="False" /flp3:logfile="%ProjectName%.%Platform%.prf";performancesummary /flp4:logfile="%ProjectName%.%Platform%.exec.log";showcommandline /p:BuildSingleFilePackage=true /p:IsOfficialBuild=%IsOfficialBuild% /p:RunTests=True

pause