# Knowledgebank


Here is a simple tutorial to get you started with the project, this project is 

## Docker
docker compose -f docker-compose.dev.yml up --build

docker-compose -f docker-compose-dev.yml down --rmi all --volumes

docker compose -f docker-compose.prod.yml up --build
docker build -t website .
docker run -p 3000:3000 website

docker images
docker ps # List running containers
docker stop <container_id> # Stop the container
docker rm <container_id> # Remove the container

docker compose up --build

## Extensions
### VSCode Extensions
Here are some VSCode extensions that you might find useful:
- [Prettier](https://marketplace.visualstudio.com/items?itemName=esbenp.prettier-vscode)
- [ESLint](https://marketplace.visualstudio.com/items?itemName=dbaeumer.vscode-eslint)
- [Docker](https://marketplace.visualstudio.com/items?itemName=ms-azuretools.vscode-docker)
- [Dev Containers](https://marketplace.visualstudio.com/items?itemName=ms-vscode-remote.remote-containers)
- [C# Dev Kit](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csdevkit)
- [Tailwind CSS IntelliSense](https://marketplace.visualstudio.com/items?itemName=bradlc.vscode-tailwindcss)

This extension may be useful later on when we start working with Azure:
- [Azure Tools](https://marketplace.visualstudio.com/items?itemName=ms-vscode.vscode-node-azure-pack)