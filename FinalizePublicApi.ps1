# After every stable release, all PublicApi text files (Shipped & Unshipped) should be merged.
$path = "\PublicAPI";
$directory = $PSScriptRoot;

$searchPath = Join-Path -Path $directory -ChildPath $path;
Write-Host $searchPath;

Get-ChildItem -Path $searchPath -Recurse -Filter *.Shipped.txt | 
    ForEach-Object {
        Write-Host $_.FullName;

        [string]$shipped = $_.FullName;
        [string]$unshipped = $shipped -replace ".shipped.txt", ".Unshipped.txt";

        if (Test-Path $unshipped) {
            Write-Host $unshipped;

            Get-Content $shipped, $unshipped |
                Sort-Object |
                Set-Content $shipped;

            "" | Set-Content $unshipped;

            Write-Host "merged and sorted.";
        }

        Write-Host "";
    }