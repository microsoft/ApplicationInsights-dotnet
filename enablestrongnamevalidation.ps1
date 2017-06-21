Set-Location HKLM:
$registryPath = "HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\.NETFramework"
$Name = "AllowStrongNameBypass"
$s = Get-ItemProperty -Path $registryPath -Name $name
if($s -ne $null)
{
    Remove-ItemProperty -Path $registryPath -Name $name -Force -ErrorAction SilentlyContinue
}
else
{
    Write-Host "Nothing to remove"
}
