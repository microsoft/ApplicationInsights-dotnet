& docker info

Write-Host "Images before cleanup"
& docker images -a

Write-Host "Containers before cleanup"
& docker ps -a

Write-Host "Stopping E2E Containers"
& docker stop e2etests_ingestionservice_1
& docker stop e2etests_e2etestwebapi_1
& docker stop e2etests_e2etestwebapp_1
& docker stop e2etests_sql-server_1

Write-Host "Removing E2E Containers"
& docker rm e2etests_ingestionservice_1
& docker rm e2etests_e2etestwebapi_1
& docker rm e2etests_e2etestwebapp_1
& docker rm e2etests_sql-server_1

Write-Host "Removing E2E Images"
& docker rmi -f e2etests_ingestionservice
& docker rmi -f e2etests_e2etestwebapi
& docker rmi -f e2etests_e2etestwebapp 
& docker rmi -f e2etests_sql-server


Write-Host "Removing dangling images"
docker images -f "dangling=true" -q | ForEach-Object {docker rmi $_}

Write-Host "Images after cleanup"
& docker images -a
Write-Host "Containers after cleanup"
& docker ps -a

Write-Host "Checking SQL Docker inspect"
docker inspect e2etests_sql-server_1

Write-Host "Checking SQL"
$serverip = docker inspect -f "{{ .NetworkSettings.Networks.nat.IPAddress }}" e2etests_sql-server_1
Write-Host "IP: $serverip"
& "C:\Program Files\Microsoft SQL Server\110\Tools\Binn\sqlcmd.exe" -S $serverip -U sa -P MSDNm4g4z!n4 -d master -q "SELECT name FROM master.dbo.sysdatabases"
