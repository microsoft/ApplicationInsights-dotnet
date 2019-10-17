$directory = $PSScriptRoot;
Write-Host "Scanning $directory";


$oldVersion = "2.12.0-beta1-build2175"
Write-Host "Old Version: $oldVersion";

##Use this to get the new version from MyGet##
#$newVersion = .\NuGet.exe list "Microsoft.ApplicationInsights" -Source https://www.myget.org/F/applicationinsights -Pre -NonInteractive | Select-String -Pattern "Microsoft.ApplicationInsights " | %{$_.Line.Split(" ")} | Select -skip 1

##Use this to manually set the new version##
$newVersion = "2.12.0-beta1-build4530" # this is package version, 2.10.0-beta4 for beta, 2.10.0 for stable
Write-Host "New Version: $newVersion";

$oldAssemblyVersion = "2.11.0.0"
$newAssemblyVersion = "2.12.0.0" # this is assembly version 2.10.0-beta4 for beta, 2.10.0.0 for stable
Write-Host "Old Asembly Version: $oldAssemblyVersion";
Write-Host "New Asembly Version: $newAssemblyVersion";


function Replace ([string] $Filter, [string] $Old, [string] $New) {
  Write-Host "";
  Write-Host "FILTER: $($Filter) REPLACE: $($Old) with $($New)";

  Get-ChildItem -Path $directory -Filter $Filter -Recurse | 
    foreach-object {
      Write-Host " - $($_.FullName)";
      (Get-Content $_.FullName) | Foreach-Object { $_ -replace $Old, $New; } | Set-Content $_.FullName
    }
}

# <package id="Microsoft.ApplicationInsights" version="2.11.0" targetFramework="net452" />
Replace -Filter "packages.config" -Old $oldVersion -New $newVersion;

# <Reference Include="Microsoft.ApplicationInsights, Version=2.11.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35, processorArchitecture=MSIL">
Replace -Filter "*proj" -Old "Version=$oldAssemblyVersion" -New "Version=$newAssemblyVersion";

# <HintPath>..\..\..\..\packages\Microsoft.ApplicationInsights.2.11.0\lib\net45\Microsoft.ApplicationInsights.dll</HintPath>
# <PackageReference Include="Microsoft.ApplicationInsights" Version="2.11.0" />
Replace -Filter "*proj" -Old $oldVersion -New $newVersion;


Replace -Filter "*.props" -Old $oldVersion -New $newVersion;
