& docker info
& docker images
& docker ps -a

docker images -f "dangling=true" -q | ForEach-Object {docker rmi $_}
& docker rmi -f e2etests_ingestionservice
& docker rmi -f e2etests_e2etestwebapi
& docker rmi -f e2etests_e2etestwebapp 

