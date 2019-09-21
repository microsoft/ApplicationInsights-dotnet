$directory = $PSScriptRoot;
Write-Host "Scanning $directory";


$oldVersion = "2.11.0-beta2"
Write-Host "Old Version: $oldVersion";


##Use this to get the new version from MyGet##
#$newVersion = .\NuGet.exe list "Microsoft.ApplicationInsights" -Source https://www.myget.org/F/applicationinsights -Pre -NonInteractive | Select-String -Pattern "Microsoft.ApplicationInsights " | %{$_.Line.Split(" ")} | Select -skip 1

##Use this to manually set the new version##
$newVersion = "2.11.0" # this is package version, 2.10.0-beta4 for beta, 2.10.0 for stable
Write-Host "New Version: $newVersion";

$newAssemblyVersion = "2.11.0.0" # this is assembly version 2.10.0-beta4 for beta, 2.10.0.0 for stable
Write-Host "New Asembly Version: $newAssemblyVersion";

Get-ChildItem -Path $directory -Filter packages.config -Recurse | 
foreach-object {
  (Get-Content $_.FullName) | 
  Foreach-Object {$_ -replace $oldVersion, $newVersion} | 
  Set-Content $_.FullName
}

# update     <Reference Include="Microsoft.ApplicationInsights, Version=2.10.0-beta4... to  Version=2.10.0.0
Get-ChildItem -Path $directory -Filter *proj -Recurse | 
foreach-object {
  (Get-Content $_.FullName) | 
  Foreach-Object {$_ -replace "Version=$oldVersion", "Version=$newAssemblyVersion"} | 
  Set-Content $_.FullName
}

Get-ChildItem -Path $directory -Filter *proj -Recurse | 
foreach-object {
  (Get-Content $_.FullName) | 
  Foreach-Object {$_ -replace $oldVersion, $newVersion} | 
  Set-Content $_.FullName
}
  
Get-ChildItem -Path $directory -Filter *.props -Recurse | 
foreach-object {
  (Get-Content $_.FullName) | 
  Foreach-Object {$_ -replace $oldVersion, $newVersion} | 
  Set-Content $_.FullName
}

