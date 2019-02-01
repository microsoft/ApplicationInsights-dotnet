$directory = $PSScriptRoot;
Write-Host "Scanning $directory";


$oldVersion = "2.9.0"
Write-Host "Old Version: $oldVersion";


##Use this to get the new version from MyGet##
#$newVersion = .\NuGet.exe list "Microsoft.ApplicationInsights" -Source https://www.myget.org/F/applicationinsights -Pre -NonInteractive | Select-String -Pattern "Microsoft.ApplicationInsights " | %{$_.Line.Split(" ")} | Select -skip 1

##Use this to manually set the new version##
$newVersion = "2.10.0-beta1"
Write-Host "New Version: $newVersion";


Get-ChildItem -Path $directory -Filter packages.config -Recurse | 
foreach-object {
  (Get-Content $_.FullName) | 
  Foreach-Object {$_ -replace $oldVersion, $newVersion} | 
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

