& docker info

Write-Host "Images before cleanup"
& docker images

Write-Host "Containers before cleanup"
& docker ps -a

Write-Host "Stopping E2E Containers"
& docker stop -f e2etests_ingestionservice
& docker stop -f e2etests_e2etestwebapi
& docker stop -f e2etests_e2etestwebapp 

Write-Host "Removing E2E Containers"
& docker rm e2etests_ingestionservice
& docker rm e2etests_e2etestwebapi
& docker rm e2etests_e2etestwebapp

Write-Host "Removing E2E Images"
& docker rmi -f e2etests_ingestionservice
& docker rmi -f e2etests_e2etestwebapi
& docker rmi -f e2etests_e2etestwebapp 

Write-Host "Removing dangling images"
docker images -f "dangling=true" -q | ForEach-Object {docker rmi $_}

Write-Host "Images after cleanup"
& docker images
Write-Host "Containers after cleanup"
& docker ps -a

