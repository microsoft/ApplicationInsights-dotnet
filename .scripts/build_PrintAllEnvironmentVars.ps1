# SUMMARY
# This script will print all environment variables to console.
# This is useful when working with hosted build environments.


$var = (Get-ChildItem env:*).GetEnumerator() | Sort-Object Name
$out = ""
Foreach ($v in $var) {$out = $out + "`t{0,-28} = {1,-28}`n" -f $v.Name, $v.Value}

write-output $out

Write-Host "`nDocker:";
Docker --version

Write-Host "`nDotNet List SDKs:";
dotnet --list-sdks