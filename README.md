docker compose -f docker-compose.dev.yml up --build
docker compose -f docker-compose.prod.yml up --build
docker build -t website .
docker run -p 3000:3000 website

docker images
docker ps # List running containers
docker stop <container_id> # Stop the container
docker rm <container_id> # Remove the container

docker compose up --build



[installation](https://pnpm.io/installation)