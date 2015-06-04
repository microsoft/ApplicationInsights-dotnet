#enable verbose mode
$VerbosePreference = "Continue";

$TestProjects = @(
	'.\test\Microsoft.ApplicationInsights.AspNet.Tests',
	'.\test\Mvc6Framework45.FunctionalTests',
	'.\test\WebApiShimFw46.FunctionalTests'
)

Function Execute-DnxProcess {
	Param (
		[Parameter(Mandatory=$True)]
		[String]$RuntimePath,
		[Parameter(Mandatory=$True)]
		[String]$Arguments,
		[Parameter(Mandatory=$True)]
		[String]$WorkingDirectory
	)

	$pinfo = New-Object System.Diagnostics.ProcessStartInfo;
	$pinfo.FileName = $dnxPath;
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

Function Get-DnxRuntimePaths {
	[String]$runtimesRoot = [System.Environment]::ExpandEnvironmentVariables(
		'%USERPROFILE%\.dnx\runtimes');

	Write-Verbose "Start discovering DNX runtimes, rutimeRoot:$runtimesRoot";

	[string[]]$results = @();
	# lists folders only
	Get-ChildItem $runtimesRoot | ?{ $_.PSIsContainer } |%{
		$runtimePath = """$runtimesRoot\$($_.Name)\bin\dnx.exe""";

		Write-Verbose "DNX runtime path discovered, path:$runtimePath";

		$results += $runtimePath;
	};

	Write-Verbose "Stop discovering DNX runtimes";

	Return $results;
}

[PSObject[]]$global:failed = @();
$global:WorkingDirectory = (pwd).Path;

$dnxRuntimePaths = Get-DnxRuntimePaths;

If ($dnxRuntimePaths.Count -ne 4){
	Throw "Unexpected number of DNX runtimes were discovered, $($dnxRuntimePaths.Count)";
}

$dnxRuntimePaths |% {
	
	$dnxPath = $_;

	$TestProjects |% {
		[String]$arguments = ". test";
		[String]$currentWorkingDirectory = Join-Path $global:WorkingDirectory -ChildPath $_;

		Write-Host "=========================================================";
		Write-Host "== Executing tests";
		Write-Host "== Working Folder: $currentWorkingDirectory";
		Write-Host "== Runtime:$dnxPath";
		Write-Host "== Args:$arguments";
		Write-Host "=========================================================";

		$executeResult = Execute-DnxProcess `
			-RuntimePath $dnxPath `
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