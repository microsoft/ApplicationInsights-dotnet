#$oldFileVersion = ""

##Use this to get the new version from MyGet##
$newVersion = .\NuGet.exe list "Microsoft.ApplicationInsights" -Source https://www.myget.org/F/applicationinsights -Pre -NonInteractive | Select-String -Pattern "Microsoft.ApplicationInsights " | %{$_.Line.Split(" ")} | Select -skip 1

##Use this to manually set the new version##
$newVersion = "2.7.0-beta3"

Write-Host "New Version: " $newVersion

#$oldVersion = cat .\Directory.Build.props | Select-String -Pattern "CoreSdkVersion" | %{$_.Line.Split("<>")} | Select -skip 2 | Select -First 1
$oldVersion = "2.7.0-beta2"

Write-Host "Old Version: " $oldVersion

(Get-Content Directory.Build.props) | 
Foreach-Object {$_ -replace $oldVersion, $newVersion} | 
Set-Content Directory.Build.props 


Get-ChildItem -Filter packages.config -Recurse | 
foreach-object {
  (Get-Content $_.FullName) | 
  Foreach-Object {$_ -replace $oldVersion, $newVersion} | 
  Set-Content $_.FullName
}


Get-ChildItem -Filter *proj -Recurse | 
foreach-object {
  (Get-Content $_.FullName) | 
  Foreach-Object {$_ -replace $oldVersion, $newVersion} | 
  Set-Content $_.FullName


#  (Get-Content $_.FullName) | 
#  Foreach-Object {$_ -replace $oldFileVersion, $newFileVersion} | 
#  Set-Content $_.FullName
}