
$DnxRuntimes = @(
	'dnx-clr-win-x86.1.0.0-beta4', 
	'dnx-coreclr-win-x86.1.0.0-beta4');

$TestProjects = @(
	'.\test\Microsoft.ApplicationInsights.AspNet.Tests',
	'.\test\Mvc6Framework45.FunctionalTests',
	'.\test\WebApiShimFw46.FunctionalTests'
)

Function Execute-Process {
	Param (
		[String]$RuntimePath,
		[String]$Arguments
	)

	$pinfo = New-Object System.Diagnostics.ProcessStartInfo;
	$pinfo.FileName = $dnxPath;
	$pinfo.RedirectStandardError = $true;
	$pinfo.RedirectStandardOutput = $true;
	$pinfo.UseShellExecute = $false;
	$pinfo.Arguments = $arguments;

	$p = New-Object System.Diagnostics.Process;
	$p.StartInfo = $pinfo;
	$p.Start() | Out-Null;
	$p.WaitForExit();
	
	Return $p.StandardOutput.ReadToEnd();
}

Function Get-OutputSummary {
	Param (
		[String]$Data
	)

	If ($Data -match '\W*Errors:\W*(?<Errors>\d+),\W*Failed:\W*(?<Failed>\d+)') {
		Return New-Object PSObject -Property @{
			Errors = [int]$matches['Errors'];
			Failed = [int]$matches['Failed'];
		};	
	} Else {
		Throw "Input string is not wellformet to extract summary data";
	}
}

[PSObject[]]$global:failed = @();
$DnxRuntimes |% {
	$dnxPath = "%USERPROFILE%\.dnx\runtimes\$($_)\bin\dnx.exe";
	$dnxPath = [System.Environment]::ExpandEnvironmentVariables($dnxPath);

	$TestProjects |% {
		[String]$arguments = "$_ test -nologo -diagnostics";

		Write-Host "=========================================================";
		Write-Host "== Executing tests";
		Write-Host "== Working Folder: $((pwd).Path)";
		Write-Host "== Runtime:$dnxPath";
		Write-Host "== Args:$arguments";
		Write-Host "=========================================================";

		[String]$out = Execute-Process $dnxPath $arguments;
		Write-Host $out;

		$summary = Get-OutputSummary $Out;
		If (($summary.Failed -ne 0) -or ($summary.Errors -ne 0)){
			$global:failed += $summary;
		}
	}
}

If ($global:failed.Count -gt 0){
	Throw "Test execution failed";
}