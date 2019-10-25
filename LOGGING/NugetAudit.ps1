
# This script checks the Authoring Requirements listed here: 
# Full Requirements: https://microsoft.sharepoint.com/teams/NuGet/MicrosoftWiki/MicrosoftPackages.aspx
# Authoring Requirements: https://microsoft.sharepoint.com/teams/NuGet/MicrosoftWiki/AuthoringRequirements.aspx
# Signing Requirements: https://microsoft.sharepoint.com/teams/NuGet/MicrosoftWiki/SigningMicrosoftPackages.aspx
Param(
    [Parameter(Mandatory=$true,HelpMessage="Path to Artifact files (nupkg):")]
    [string]
    $path,
    [Parameter(Mandatory=$true,HelpMessage="Full Log?:")] #Include Pass with Fail output?
    [bool]
    $verboseLog
) 


$requiredCopyright = "$([char]0x00A9) Microsoft Corporation. All rights reserved.";#"© Microsoft Corporation. All rights reserved.";
$expectedProjectUrl = "https://go.microsoft.com/fwlink/?LinkId=392727"; # Application Insights Project Url
$expectedLicense = "MIT"; # MIT license SPDX ID
$expectedOwner = "AppInsightsSdk"; # Application Insights Nuget Account
$expectedTags = @("Azure","Monitoring");

$sb = [System.Text.StringBuilder]::new();

$script:isValid = $true;


# Get the latest Nuget.exe from here:
if (!(Test-Path ".\Nuget.exe")) {

    Write-Host "Nuget.exe not found. Attempting download...";
    Write-Host "Start time:" (Get-Date -Format G);
    $downloadNugetUrl = "https://dist.nuget.org/win-x86-commandline/latest/nuget.exe";
    $saveFile = "$PSScriptRoot\Nuget.exe";
    (New-Object System.Net.WebClient).DownloadFile($downloadNugetUrl, $saveFile);
    Write-Host "Finish time:" (Get-Date -Format G);
    
    if (!(Test-Path ".\Nuget.exe")) {
        throw "Error: Nuget.exe not found! Please download latest from: https://www.nuget.org/downloads";
    }
}

function Write-Break() {
    $displayMessage = "========== ========== ========== ========== ==========";
    Write-Host "";
    Write-Host $displayMessage;
    $null = $sb.AppendLine();
    $null = $sb.AppendLine($displayMessage);
}

function Write-Name([string]$message) {
    Write-Host $message;
    $null = $sb.AppendLine($message);
}

function Write-IsValid ([string]$message) {
    if ($verboseLog) {
        $displayMessage = "`tPass:`t$message";
        Write-Host $displayMessage -ForegroundColor Green;
        $null = $sb.AppendLine($displayMessage);
    }
}

function Write-SemiValid ([string]$message, [string]$recommendedDescription) {
    $displayMessage = "`tReview:`t$message [Recommended: $recommendedDescription]";
    Write-Host $displayMessage -ForegroundColor Yellow;
    $null = $sb.AppendLine($displayMessage);
}

function Write-NotValid ([string]$message, [string]$requiredDescription) {
    $displayMessage = "`tFail:`t$message [Required: $requiredDescription]";
    Write-Host $displayMessage -ForegroundColor Red;
    $null = $sb.AppendLine($displayMessage);
	$script:isValid = $false;
}

function Test-Condition ([bool]$condition, [string]$message, [string]$requiredDescription) {
    if ($condition) {
        Write-IsValid $message
    } else {
        Write-NotValid $message $requiredDescription
    }
}

function Test-MultiCondition ([bool]$requiredCondition, [bool]$recommendedCondition, [string]$message, [string]$requiredDescription, [string]$recommendedDescription) {
    if ($requiredCondition) {
        if($recommendedCondition) {
            Write-IsValid $message
        } else {
            Write-SemiValid $message $recommendedDescription
        }
    } else {
        Write-NotValid $message $requiredDescription
    }
}

function Get-IsPackageSigned([string]$nupkgPath) {
    $verifyOutput = "";
    $null = .\Nuget.exe verify -signature -CertificateFingerprint 3F9001EA83C560D712C24CF213C3D312CB3BFF51EE89435D3430BD06B5D0EECE $nupkgPath -verbosity detailed 2>&1 | Tee-Object -Variable verifyOutput
    
	#TEST OUTPUT
	Write-Host $verifyOutput

    $output = $verifyOutput[$verifyOutput.Length-1]

    $success = ($output -like "Successfully verified*");

    if (!$success){
        $verifyOutput | Where-Object { $_ -like "NU*" -and $_ -notlike "NUGET*"} | ForEach-Object {
            $output = "$output $_" -replace "`n","" -replace "`r","";
        }
    }
    
    $message = "Is Signed: $output";
    $requirement = "Must be signed."

    Test-Condition ($success) $message $requirement;
}


function Get-IsDllSigned ([string]$dllPath) {
    $test = Get-AuthenticodeSignature $dllPath; # this is  equivalent to signtool.exe verify /pa
    $SignedStatus = $test.Status;
    $SignatureValid = [System.Management.Automation.SignatureStatus]::Valid;

    $message = "Is Signed: $SignedStatus";
    $requirement = "Must be signed."

    Test-Condition ($SignedStatus -eq $SignatureValid) $message $requirement;
}

function Get-DoesXmlDocExist ([string]$dllPath) {
    # CONFIRM .XML DOCUMENTATION FILE EXISTS WITH EACH DLL
    [string]$docFile = $dllPath -replace ".dll", ".xml";

    $message = "XML Documentation:";
    $requirement = "Must exist."
    Test-Condition (Test-Path $docFile) $message $requirement;
}

function Get-IsValidPackageId([xml]$nuspecXml) {
    $id = $nuspecXml.package.metadata.id;

    $message = "Package Id: $id";
    $requirement = "Must begin with 'Microsoft.'"

    Test-Condition ($id.StartsWith("Microsoft.")) $message $requirement;
}

function Get-IsValidAuthors([xml]$nuspecXml) {
    $authors = $nuspecXml.package.metadata.authors;

    $message = "Authors: $authors";
    $requirement = "Microsoft must be the only author.";

    Test-Condition ($authors -eq "Microsoft") $message $requirement;
}

function Get-IsValidOwners([xml]$nuspecXml) {
    $owners = $nuspecXml.package.metadata.owners;
    $ownersList = $owners -split ',';

    $message = "Owners: $owners";
    $requirement = "Must include Microsoft."
    $recommendation = "Should include nuget owner."

    Test-MultiCondition ($ownersList -contains "Microsoft") ($ownersList -contains $expectedOwner -and $ownersList.Length -eq 2) $message $requirement $recommendation;
}

function Get-IsValidProjectUrl([xml]$nuspecXml) {
    $projectUrl = $nuspecXml.package.metadata.projectUrl;

    $message = "Project Url: $projectUrl";
    $requirement = "Must match expected."

    Test-Condition ($projectUrl -eq $expectedProjectUrl) $message $requirement;
}

function Get-IsValidLicense([xml]$nuspecXml) {
    $license = $nuspecXml.package.metadata.license.InnerText;

    $message = "License Url: $license";
    $requirement = "Must match expected."    

    Test-Condition ($license -eq $expectedLicense) $message $requirement;
}

function Get-IsValidLicenseAcceptance([xml]$nuspecXml) {
    $requireLicenseAcceptance = $nuspecXml.package.metadata.requireLicenseAcceptance;

    $message = "Require License Acceptance: $requireLicenseAcceptance";
    $requirement = "Not mandatory requirement."
    $recommendation = "Should require.";

    Test-MultiCondition ($true) ($requireLicenseAcceptance -eq $true) $message $requirement $recommendation;
}

function Get-IsValidCopyright([xml]$nuspecXml) {
    $copyright = $nuspecXml.package.metadata.copyright;
    
    $message = "Copyright: $copyright";
    $requirement = "Must match '$requiredCopyright'";

    Test-Condition ($copyright -eq $requiredCopyright) $message $requirement;
}

function Get-IsValidDescription([xml]$nuspecXml) {
    $description = $nuspecXml.package.metadata.description;
    $hasDescription = !([System.String]::IsNullOrEmpty($description));

    $message = "Description: $description";
    $requirement = "Must have a description."

    Test-Condition $hasDescription $message $requirement;
}

function Get-IsValidTags([xml]$nuspecXml) {
    $tags = $nuspecXml.package.metadata.tags;
    $hasTags = !([System.String]::IsNullOrEmpty($tags));

    $message = "Tags: $tags";
        $requirement = "Must have tags."
        Test-Condition $hasTags $message $requirement;

    $tagsArray = @();
    if($hasTags) {
        $tagsArray = $tags -split " ";
    } 

    $expectedTags | ForEach-Object {
        $hasTag = $tagsArray.Contains($_);
        $requirement = "Must include tag: $_";
        Test-Condition $hasTag $message $requirement;
    }
}

function Get-IsValidLogoUrl([xml]$nuspecXml, $path) {
    $logoUrl = $nuspecXml.package.metadata.iconUrl;
    $isEmpty = [System.String]::IsNullOrEmpty($logoUrl);
    $dimension = "";

    try {
        $filePath = Join-Path $path "logo.png";
        $wc = New-Object System.Net.WebClient;
        $wc.DownloadFile($logoUrl, $filePath);
        add-type -AssemblyName System.Drawing
        $png = New-Object System.Drawing.Bitmap $filePath
        $dimension = "$($png.Height)x$($png.Width)";
        
        # Release lock on png file
        Remove-Variable png;
        Remove-Variable wc;
    } catch [System.SystemException] {
        $_.Exception.Message;
    }

    [string[]]$expectedDimensions = ("32x32","48x48","64x64","128x128");

    $message = "Logo Url: $logoUrl Dimensions: $dimension";
    $requirement = "Must have a logo."
    $recommendation = "Should be one of these sizes: $expectedDimensions";
    
    $isExpected = ($expectedDimensions -contains $dimension);

    Test-MultiCondition (!$isEmpty) ($isExpected) $message $requirement $recommendation;
}

function Invoke-UnZip([string]$zipfile, [string]$outpath) {
    Write-Verbose "Unzip - source: $zipfile"
    Write-Verbose "Unzip - target: $outpath"
    Add-Type -assembly "system.io.compression.filesystem"
    [System.IO.Compression.ZipFile]::ExtractToDirectory($zipfile, $outpath)
}

function Start-EvaluateNupkg ($nupkgPath) {
    Write-Break;
    Write-Name $nupkgPath;

    Get-IsPackageSigned $nupkgPath;


    $unzipPath = $nupkgPath+"_unzip";
    Remove-Item $unzipPath -Recurse -ErrorAction Ignore
    $null = Invoke-UnZip $nupkgPath $unzipPath;

    # LOOK FOR ALL NUSPEC WITHIN NUPKG
    Get-ChildItem -Path $unzipPath -Recurse -Filter *.nuspec | ForEach-Object { 
        Write-Name $_.FullName;
        [xml]$nuspecXml = Get-Content $_.FullName
        Get-IsValidPackageId $nuspecXml;
        Get-IsValidAuthors $nuspecXml;
        Get-IsValidOwners $nuspecXml;
        Get-IsValidProjectUrl $nuspecXml;
        Get-IsValidLicense $nuspecXml;
        Get-IsValidLicenseAcceptance $nuspecXml;
        Get-IsValidCopyright $nuspecXml;
        Get-IsValidLogoUrl $nuspecXml $unzipPath;
        Get-IsValidDescription $nuspecXml;
        Get-IsValidTags $nuspecXml;
        }
    
    # LOOK FOR ALL DLL WITHIN NUPKG
    Get-ChildItem -Path $unzipPath -Recurse -Filter *.dll | ForEach-Object {
        Write-Name $_.FullName;
        Get-IsDllSigned $_.FullName;
        Get-DoesXmlDocExist $_.FullName;
        }
}

# LOOK FOR ALL NUPKG IN A DIRECTORY. 
Get-ChildItem -Path $path -Recurse -Filter *.nupkg -Exclude *.symbols.nupkg | 
    ForEach-Object { Start-EvaluateNupkg $_.FullName }

$sb.ToString() | Add-Content (Join-Path $path "log.txt");

if (!$script:isValid){
	throw "NUPKG or DLL is not valid. Please review log...";
    }
