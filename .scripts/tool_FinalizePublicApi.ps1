# After every stable release, all PublicApi text files (Shipped & Unshipped) should be merged.

# When running this script locally, you may get an error saying: "is not digitally signed. You cannot run this script on the current system."
# To fix run this command: "Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope Process"

$path = "..\.publicApi";
$directory = $PSScriptRoot;

$searchPath = Join-Path -Path $directory -ChildPath $path;
Write-Host "Search Directory: $searchPath";
Write-Host "";

Get-ChildItem -Path $searchPath -Recurse -Filter *.Shipped.txt | 
    ForEach-Object {
        Write-Host $_.FullName;

        [string]$shipped = $_.FullName;
        [string]$unshipped = $shipped -replace ".shipped.txt", ".Unshipped.txt";

        if (Test-Path $unshipped) {
            Write-Host $unshipped;

            Get-Content $shipped, $unshipped |  # get contents of both text files
                Where-Object {$_ -ne ""} |      # filter empty lines
                Sort-Object |                   # sort lines
                Get-Unique |                    # filter duplicates
                Set-Content $shipped;           # write to shipped.txt

            Clear-Content $unshipped;           # empty unshipped.txt

            Write-Host "...MERGED and SORTED";
        }

        Write-Host "";
    }