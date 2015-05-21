
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

	$pinfo = New-Object System.Diagnostics.ProcessStartInfo
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

$DnxRuntimes |% {
	$dnxPath = "%USERPROFILE%\.dnx\runtimes\$($_)\bin\dnx.exe";
	$dnxPath = [System.Environment]::ExpandEnvironmentVariables($dnxPath);

	$TestProjects |% {
		[String]$arguments = "$_ test -nologo -diagnostics";
		[String]$Out = Execute-Process $dnxPath $arguments;

		Write-Host $Out;
	}
}