$ErrorActionPreference = "silentlycontinue"

Write-Host "Cleaning up iisexpress"
Get-Process -Name iisexpress | Stop-Process
Write-Host "Cleaning up iisexpress completed"


