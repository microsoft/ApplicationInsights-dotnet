# Find our external IP address
$ip = (get-netipaddress | where {$_.AddressFamily -eq "IPv4" -and $_.AddressState -eq "Preferred" -and $_.PrefixOrigin -ne "WellKnown" }[0]).IPAddress

# Rewrite AzureStorageEmulator.exe.config to use it
$config = "C:\Program Files (x86)\Microsoft SDKs\Azure\Storage Emulator\AzureStorageEmulator.exe.config"
(get-content $config) -replace "127.0.0.1",$ip | out-file $config

# Launch the emulator
& "C:\Program Files (x86)\Microsoft SDKs\Azure\Storage Emulator\AzureStorageEmulator.exe" start -inprocess