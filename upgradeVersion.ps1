#$oldFileVersion = "2.4.0-beta1"
#$newVersion = "2.4.0-beta2"

$newVersion = .\NuGet.exe list "Microsoft.ApplicationInsights" -Source https://www.myget.org/F/applicationinsights -Pre -NonInteractive | Select-String -Pattern "Microsoft.ApplicationInsights " | %{$_.Line.Split(" ")} | Select -skip 1

#$newVersion ="2.2.0-beta4"

Write-Host $newVersion

$oldVersion = cat .\Directory.Build.props | Select-String -Pattern "CoreSdkVersion" | %{$_.Line.Split("<>")} | Select -skip 2 | Select -First 1

Write-Host $oldVersion

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


  (Get-Content $_.FullName) | 
  Foreach-Object {$_ -replace $oldFileVersion, $newFileVersion} | 
  Set-Content $_.FullName
}