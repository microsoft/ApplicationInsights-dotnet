@echo off

call "%VS120COMNTOOLS%\VsDevCmd.bat"

SET PATH=%PATH%;%systemdrive%\Windows\Microsoft.NET\Framework\v4.0.30319\;
SET ProjectName=Msbuild.All

SET Configuration=Release

SET Platform="Mixed Platforms"
msbuild dirs.proj /nologo /m:1  /fl /toolsversion:14.0 /flp:logfile=%ProjectName%.%Platform%.log;v=d /flp1:logfile=%ProjectName%.%Platform%.wrn;warningsonly /flp2:logfile=%ProjectName%.%Platform%.err;errorsonly /p:Configuration=%Configuration% /p:Platform=%Platform% /p:RunCodeAnalysis="False" /flp3:logfile=%ProjectName%.%Platform%.prf;performancesummary /flp4:logfile=%ProjectName%.%Platform%.exec.log;showcommandline /p:BuildSingleFilePackage=true

PAUSE


