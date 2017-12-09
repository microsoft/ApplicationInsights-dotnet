Param
(
    [Parameter(Mandatory=$false,
            Position=0,
            ValueFromPipelineByPropertyName=$true,
            HelpMessage="Root path to discover all test assemblies from. Default is ..\bin")]
    [string[]]
    $BinariesPath = "../bin",
    [Parameter(Mandatory=$false,HelpMessage="Skip test categories in this list.")]
    [string[]]
    $SkipTestCategories = @("RequiresNet"),
    [Parameter(Mandatory=$false,HelpMessage="Skip tests by name by adding them to this list.")]
    [string[]]
    $SkipTests
)

$testFiles = gci -Filter *Test*.dll -Recurse -Path $BinariesPath
Write-Verbose "Found ${$testFiles.Count} test assemblies."

$testFilters = ""
if ($SkipTestCategories -or $SkipTests)
{
    $testFilters = "/TestCaseFilter:"
    $testFilterSuffix = ""
    if ($SkipTestCategories)
    {
        if ($SkipTestCategories.Count -gt 1)
        {
            $testFilters += "("
            $testFilterSuffix = ")"
        }
        $separator = ""
        $SkipTestCategories | ForEach-Object {
            $category = "${$separator}TestCategory!=$PSItem"
            $testFilters += $category
            Write-Verbose "Added filter '$category'"
            $separator = "|"
        }

        $testFilters += $testFilterSuffix
    }

    if ($SkipTests)
    {
        if ($testFilters.Count -gt 0)
        {
            $testFilters += "|"
        }
        $skipTestFilterSuffix = ""
        if ($SkipTests.Count -gt 1)
        {
            $testFilters += "("
            $skipTestFilterSuffix = ")"
        }
        $separator = ""
        $SkipTests | ForEach-Object {
            $category = $separator + "Name!=$PSItem"
            $testFilters += $category
            Write-Verbose "Added filter '$category'"
            $separator = "|"
        }
        $testFilters += $skipTestFilterSuffix
    }
}

Write-Verbose "Test filters string = '$testFilters'"

$testFiles | ForEach-Object {
    $testAssembly = $PSItem.FullName
    Write-Verbose "Testing assembly: '${Split-Path -Leaf $testAssembly}'"
    Write-Verbose "Command used: & vstest.console.exe $testAssembly /Parallel $testFilters"
    & vstest.console.exe $testAssembly /Parallel $testFilters 
}
