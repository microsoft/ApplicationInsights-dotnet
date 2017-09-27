& docker info
& docker images
& docker ps -a

docker images -f "dangling=true" -q | ForEach-Object {docker rmi $_}