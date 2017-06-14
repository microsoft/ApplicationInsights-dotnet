#Run code coverage tests to generate report
..\packages\OpenCover.4.6.519\tools\OpenCover.Console.exe -register:user "-target:C:\Program Files (x86)\Microsoft Visual Studio\2017\Enterprise\Common7\IDE\CommonExtensions\Microsoft\TestWindow\vstest.console.exe" "-targetargs:..\a\Release\Src\DependencyCollector\Net40.Tests\Microsoft.ApplicationInsights.DependencyCollector.Net40.Tests.dll ..\a\Release\Src\DependencyCollector\Net45.Tests\Microsoft.ApplicationInsights.DependencyCollector.Net45.Tests.dll   ..\a\Release\Src\DependencyCollector\Nuget.Tests\Microsoft.ApplicationInsights.DependencyCollector.NuGet.Tests.dll ..\a\Release\Src\PerformanceCollector\Unit.Tests\Unit.Tests.dll ..\a\Release\Src\PerformanceCollector\Xdt.Tests\Xdt.Tests.dll ..\a\Release\Src\Web\Web.Net40.Tests\Microsoft.ApplicationInsights.Web.Net40.Tests.dll ..\a\Release\Src\Web\Web.Net45.Tests\Microsoft.ApplicationInsights.Web.Net45.Tests.dll ..\a\Release\Src\Web\Web.Nuget.Tests\Microsoft.ApplicationInsights.Platform.Web.Nuget.Tests.dll ..\a\Release\Src\WindowsServer\WindowsServer.Net40.Tests\WindowsServer.Net40.Tests.dll ..\a\Release\Src\WindowsServer\WindowsServer.Net45.Tests\WindowsServer.Net45.Tests.dll  ..\a\Release\Src\WindowsServer\WindowsServer.Nuget.Tests\WindowsServer.Nuget.Tests.dll /TestCaseFilter:TestCategory!=Required_4_5_1 /logger:trx" "-filter:+[Microsoft.AI*]* -[*Tests]* -[*TestFramework*]* -[*]Microsoft.ApplicationInsights.Extensibility.Implementation.External*" -hideskipped:All -output:.\coverage.xml

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

