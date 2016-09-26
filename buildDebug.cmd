@echo off

call "%VS140COMNTOOLS%\VsDevCmd.bat"

SET dotNetPath=%systemdrive%\Windows\Microsoft.NET\Framework\v4.0.30319\;
IF NOT "x!PATH:%dotNetPath%=!"=="x%PATH%" SET PATH=%PATH%;%dotNetPath%
SET ProjectName=Msbuild.All

SET Configuration=Debug

SET Platform="Mixed Platforms"
msbuild dirs.proj /nologo /m:1  /fl /toolsversion:14.0 /flp:logfile=%ProjectName%.%Platform%.log;v=d /flp1:logfile=%ProjectName%.%Platform%.wrn;warningsonly /flp2:logfile=%ProjectName%.%Platform%.err;errorsonly /p:Configuration=%Configuration% /p:Platform=%Platform% /p:RunCodeAnalysis="False" /flp3:logfile=%ProjectName%.%Platform%.prf;performancesummary /flp4:logfile=%ProjectName%.%Platform%.exec.log;showcommandline /p:BuildSingleFilePackage=true

PAUSE


