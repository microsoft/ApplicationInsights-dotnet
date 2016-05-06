#enable verbose mode
$VerbosePreference = "Continue";

$Projects = @(
    '.\src\Microsoft.ApplicationInsights.AspNetCore',
	'.\test\Microsoft.ApplicationInsights.AspNetCore.Tests',
	'.\test\MVCFramework45.FunctionalTests',
	'.\test\WebApiShimFw46.FunctionalTests',
    '.\test\EmptyApp.FunctionalTests'
)

$Commands = @(
    'restore',
    'build',
    '-v pack'
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
	$pinfo.FileName = $dotnetPath;
	$pinfo.RedirectStandardOutput = $true;
	$pinfo.UseShellExecute = $false;
	$pinfo.Arguments = $arguments;
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

$Projects |% {
	[String]$currentWorkingDirectory = Join-Path $global:WorkingDirectory -ChildPath $_;
    $Commands |% {
        $command = $_;
        Write-Host "=========================================================";
	    Write-Host "== Executing tests";
	    Write-Host "== Working Folder: $currentWorkingDirectory";
	    Write-Host "== Runtime:$dotnetPath";
	    Write-Host "== Args:$command";
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
}

If ($global:failed.Count -gt 0) {
	Throw "Test execution failed";
}