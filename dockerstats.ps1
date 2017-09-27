& docker info

Write-Host "Images before cleanup"
& docker images

Write-Host "Containers before cleanup"
& docker ps -a

& docker rmi -f e2etests_ingestionservice
& docker rmi -f e2etests_e2etestwebapi
& docker rmi -f e2etests_e2etestwebapp 

docker images -f "dangling=true" -q | ForEach-Object {docker rmi $_}

Write-Host "Images after cleanup"
& docker images
Write-Host "Containers after cleanup"
& docker ps -a

