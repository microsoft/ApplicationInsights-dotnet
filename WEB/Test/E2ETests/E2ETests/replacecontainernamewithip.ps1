$ErrorActionPreference = "silentlycontinue"

$ingestionIP = docker inspect -f "{{ .NetworkSettings.Networks.nat.IPAddress }}" e2etests_ingestionservice_1
$ingestionContainerName = "e2etests_ingestionservice_1"
$webapiIp = docker inspect -f "{{ .NetworkSettings.Networks.nat.IPAddress }}" e2etests_e2etestwebapi_1
$webapiContainerName = "e2etestwebapi"
$sqlIp = docker inspect -f "{{ .NetworkSettings.Networks.nat.IPAddress }}" e2etests_sql-server_1
$sqlContainerName = "sql-server"
$azureIP = docker inspect -f "{{ .NetworkSettings.Networks.nat.IPAddress }}" e2etests_azureemulator_1
$azureContainerName = "e2etests_azureemulator_1"

# NetCore App
(Get-Content ..\\TestApps\\NetCore20\\E2ETestAppCore20\\netcoreapp2.0\\Publish\\appsettings.json).replace($ingestionContainerName, $ingestionIP) | Set-Content ..\\TestApps\\NetCore20\\E2ETestAppCore20\\netcoreapp2.0\\Publish\\appsettings.json
(Get-Content ..\\TestApps\\NetCore20\\E2ETestAppCore20\\netcoreapp2.0\\Publish\\appsettings.json).replace($sqlContainerName, $sqlIp) | Set-Content ..\\TestApps\\NetCore20\\E2ETestAppCore20\\netcoreapp2.0\\Publish\\appsettings.json

#WebApp
(Get-Content ..\\TestApps\\Net452\\E2ETestApp\\Publish\Web.config).replace($ingestionContainerName, $ingestionIP) | Set-Content ..\\TestApps\\Net452\\E2ETestApp\\Publish\Web.config
(Get-Content ..\\TestApps\\Net452\\E2ETestApp\\Publish\Web.config).replace($sqlContainerName, $sqlIp) | Set-Content ..\\TestApps\\Net452\\E2ETestApp\\Publish\Web.config
(Get-Content ..\\TestApps\\Net452\\E2ETestApp\\Publish\Web.config).replace($azureContainerName, $azureIP) | Set-Content ..\\TestApps\\Net452\\E2ETestApp\\Publish\Web.config
(Get-Content ..\\TestApps\\Net452\\E2ETestApp\\Publish\Web.config).replace($webapiContainerName, $webapiIp) | Set-Content ..\\TestApps\\Net452\\E2ETestApp\\Publish\Web.config
(Get-Content ..\\TestApps\\Net452\\E2ETestApp\\Publish\ApplicationInsights.config).replace($azureContainerName, $azureIP) | Set-Content ..\\TestApps\\Net452\\E2ETestApp\\Publish\ApplicationInsights.config