#Run code coverage tests to generate report
.\nuget.exe install "OpenCover" -Version "4.6.519"

$assemblies = get-childitem "..\bin\Release" -Filter *Tests.dll -Exclude *netcoreapp11.Tests.dll -recurse | foreach-object { $_.FullName }

foreach ($assembly in $assemblies)
{
    Write-Host "==========================================="
    Write-Host "Running tests for $assembly"
    Write-Host "==========================================="
    ..\packages\OpenCover.4.6.519\tools\OpenCover.Console.exe `
        -register:user `
        "-target:${env:ProgramFiles(x86)}\Microsoft Visual Studio\2017\Enterprise\Common7\IDE\CommonExtensions\Microsoft\TestWindow\vstest.console.exe" `
        "-targetargs:$assembly /logger:trx" `
        "-filter:+[Microsoft.ApplicationInsights*]* +[Microsoft.AI*]* -[*Tests]* -[*TestFramework*]*" `
        -hideskipped:All `
        -output:.\coverage.xml `
        -mergeoutput
}

$netcoreassemblies = get-childitem "..\bin\Release" -Filter *netcoreapp11.Tests.dll -recurse | foreach-object { $_.FullName }

foreach ($assembly in $netcoreassemblies)
{
    Write-Host "==========================================="
    Write-Host "Running tests for $assembly"
    Write-Host "==========================================="
    # Net Core tests should be run by a different version of vstest.console.exe
    ..\packages\OpenCover.4.6.519\tools\OpenCover.Console.exe `
        -register:user `
        "-target:${env:ProgramFiles(x86)}\Microsoft Visual Studio\2017\Enterprise\Common7\IDE\Extensions\TestPlatform\vstest.console.exe" `
        "-targetargs:$assembly /logger:trx /Framework:FrameworkCore10" `
        "-filter:+[Microsoft.ApplicationInsights*]* +[Microsoft.AI*]* -[*Tests]* -[*TestFramework*]*" `
        -hideskipped:All `
        -output:.\coverage.xml `
        -mergeoutput
}

#Download report uploader
(New-Object System.Net.WebClient).DownloadFile("https://codecov.io/bash", ".\CodecovUploader.sh")

#On the Agent box repo is in a detached state. So get branchName by commit hash
$lastCommit = $(git rev-parse HEAD)
Write-Host "Last commit:" $lastCommit

$branchNames = $(git branch --all --contains $lastCommit) 
Write-Host "All branches that have this commit:" $branchNames

$i=0
Foreach ($branchName in $branchNames)
{
    $i++
    # First element in the array is trash because repo is detached
    if ($i -gt 1)
    {
        $branchName = $branchName.Trim()
        # Check for what branches current commit (for which we have coverage) is the last commit
        $lastCommitOnBranch = $(git rev-parse $branchName)
        if ($lastCommitOnBranch -eq  $lastCommit)
        {
			#Cut the prefix that CodeCov does not handle well
            if ($branchName.StartsWith("remotes/origin/"))
            {
                $branchName = $branchName.Substring("remotes/origin/".Length)
            }
            Write-Host "We will upload report to:"  $branchName
			
			#Upload report
			.\CodecovUploader.sh -f coverage.xml -t $env:CODECOVACCESSKEY -X gcov -B $branchName
        }
    }
}

