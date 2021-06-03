$url="https://{IngestionEndpoint-from-Component-ConnectionString}/v2/track"
$iKey="{test-iKey}"
Write-Host $url, $iKey

$time = Get-Date
$time = $time.ToUniversalTime().ToString('o')

$availabilityData = @"
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
	"time": "$time",
	"sampleRate": 100,
	"iKey": "$iKey",
	"flags": 0
}
"@

Invoke-WebRequest -Uri $url -Method POST -Body $availabilityData -Verbose -Debug -UseBasicParsing
# Expected Output: {"itemsReceived":1,"itemsAccepted":1,"errors":[]}
