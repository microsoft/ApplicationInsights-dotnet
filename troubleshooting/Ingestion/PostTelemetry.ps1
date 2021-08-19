param (
    [Parameter(Mandatory=$true)][string]$ConnectionString
)

function ParseConnectionString {
	param (
		[string]$ConnectionString
	)

	$Map = @{}

	foreach ($Part in $ConnectionString.Split(";")) {
		$KeyValue = $Part.Split("=")
		$Map.Add($KeyValue[0], $KeyValue[1])
	}

	return $Map
}

$Map = ParseConnectionString($ConnectionString)
$Url = $Map["IngestionEndpoint"] + "v2/track"
$IKey = $Map["InstrumentationKey"]
$Time = (Get-Date).ToUniversalTime().ToString("o")
$AvailabilityData = @"
{
	"data": {
		"baseData": {
			"ver": 2,
			"id": "TestId",
			"name": "Post Telemetry Test",
			"duration": "10.00:00:00",
			"success": true,
			"runLocation": "TestLocation",
			"message": "Test Message",
			"properties": {
				"TestProperty": "TestValue"
			}
		},
		"baseType": "AvailabilityData"
	},
	"ver": 1,
	"name": "Microsoft.ApplicationInsights.Metric",
	"time": "$Time",
	"sampleRate": 100,
	"iKey": "$IKey",
	"flags": 0
}
"@

Write-Host "URL: $Url, IKey: $IKey"
# Expected Output: {"itemsReceived":1,"itemsAccepted":1,"errors":[]}
Invoke-WebRequest -Uri $Url -Method POST -Body $AvailabilityData -Verbose -Debug -UseBasicParsing
