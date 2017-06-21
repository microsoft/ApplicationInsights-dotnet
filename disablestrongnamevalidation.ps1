Set-Location HKLM:
$registryPath = "HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\.NETFramework"
$Name = "AllowStrongNameBypass"
$value = "1"
New-ItemProperty -Path $registryPath -Name $name -Value $value -PropertyType DWORD -Force
