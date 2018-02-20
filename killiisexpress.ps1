Write-Host "Cleaning up iisexpress"

$s = Get-Process -Name iisexpress -ErrorAction SilentlyContinue

if($s -ne $null)
{
    $s | Stop-Process
}
else
{
    Write-Host "IISExpress.exe not found"
}

Write-Host "Cleaning up iisexpress completed"


