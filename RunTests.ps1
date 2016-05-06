#enable verbose mode
$VerbosePreference = "Continue";

$TestProjects = @(
	'.\test\Microsoft.ApplicationInsights.AspNetCore.Tests',
	'.\test\MVCFramework45.FunctionalTests',
	'.\test\WebApiShimFw46.FunctionalTests',
    '.\test\EmptyApp.FunctionalTests'
)

Function Execute-DotnetProcess {
	Param (
		[Parameter(Mandatory=$True)]
		[String]$RuntimePath,
		[Parameter(Mandatory=$True)]
		[String]$Arguments,
		[Parameter(Mandatory=$True)]
		[String]$WorkingDirectory
	)

	$pinfo = New-Object System.Diagnostics.ProcessStartInfo;
	$pinfo.FileName = $RuntimePath;
	$pinfo.RedirectStandardOutput = $true;
	$pinfo.UseShellExecute = $false;
	$pinfo.Arguments = $Arguments;
	$pinfo.WorkingDirectory = $WorkingDirectory;

	$p = New-Object System.Diagnostics.Process;
	$p.StartInfo = $pinfo;
	$p.Start() | Out-Null;
	$p.WaitForExit();

	Return New-Object PSObject -Property @{
		RuntimePath =  [String]$RuntimePath;
		Arguments = [String]$Arguments;
		WorkingDirectory = [String]$WorkingDirectory;
		Output = [String]$p.StandardOutput.ReadToEnd();
		ExitCode = [int]$p.ExitCode;
	};
}

[PSObject[]]$global:failed = @();
$global:WorkingDirectory = (pwd).Path;

$dotnetPath = "C:\Program Files\dotnet\dotnet.exe";

$TestProjects |% {
	[String]$arguments = "test";
	[String]$currentWorkingDirectory = Join-Path $global:WorkingDirectory -ChildPath $_;
	Write-Host "=========================================================";
	Write-Host "== Executing tests";
	Write-Host "== Working Folder: $currentWorkingDirectory";
	Write-Host "== Runtime:$dotnetPath";
	Write-Host "== Args:$arguments";
	Write-Host "=========================================================";
	$executeResult = Execute-DotnetProcess `
	-RuntimePath $dotnetPath `
	-Arguments $arguments `
	-WorkingDirectory $currentWorkingDirectory;
	Write-Host "Test process executed, ExitCode:$($executeResult.ExitCode)";
	Write-Host "Output:";
	Write-Host $executeResult.Output;
	If ($executeResult.ExitCode -ne 0) {
    	$global:failed += $executeResult;
	}
}

If ($global:failed.Count -gt 0) {
	Throw "Test execution failed";
}