param(
    [Parameter(Mandatory=$True)]
    [System.String]
    $Directory
)


# SUMMARY
# This script will delete the BIN and OBJ directories, if they exist.
# This is used in our builds to ensure that there are no left over artifacts from a previous build.


function Clean ([string]$dir) {
    
    Write-Host "`nDirectory: $($dir)";

    if (Test-Path $dir) 
    { 
        Remove-Item $dir -Recurse -Force; 
        Write-Host " removed";
    }
    else 
    { 
        Write-Host " directory not found"
    }
}

Clean "$($Directory)\bin"
Clean "$($Directory)\obj"
