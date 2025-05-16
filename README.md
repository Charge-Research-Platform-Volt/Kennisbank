# Knowledgebank

Welcome to the Knowledgebank!

## Docker
To compose the services you can use the npm scripts

Compose Development:\
npm run dev

Compose Production:\
npm run prod

Delete images and containers:\
npm run down

Which will work regardless of future changes, but the command prompt behaves somewhat strange after calling Ctrl+C.


Alternatively, call docker compose directly

Compose Development:\
docker compose -f docker-compose.yml -f docker-compose.dev.yml up --build

Compose Production:\
docker compose -f docker-compose.yml -f docker-compose.prod.yml up --build

Delete images and containers:\
docker compose down --rmi local

If these are outdated, you can copy them from package.json (which is where the scripts are stored)

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