### Requirements
- Server capable of running Docker
- S3 Bucket Storage
- A domain name with DNS Record access
- An e-mail address with SMTP details
- Azure Document Intelligence
- Azure LLM Access (Like gpt-5-nano)
- Azure CohereV4 Embedding Access

# Setting the domain name
For this server two subdomains are needed, one for the VPN service and one for frontend access. This can be set up by finding the public IP of your server and creating two A-records on your domain pointing to that IP. For example, `vpn.example.com` and `app.example.com`.

# Setting up Docker
For this service, Docker is required. The installation instructions for docker are found on the [Docker Documentation](https://docs.docker.com/engine/install/). For managing all the composes it might be easier to use [Dockge](https://github.com/louislam/dockge) which can be accessed using [SSH Tunneling](https://www.ssh.com/academy/ssh/tunneling). Please look into SSH Tunneling since this can be used to securely access all admin services securely without exposing any ports to the web.
After installing docker we will continue with setting up the different services.

### NGINX Reverse Proxy Manager
For handing HTTPS and pointing different subdomains to different services, we will be using a NGINX Reverse Proxy. The easiest way to set this up is using the NGINX Proxy Manager image available for Docker. The compose for this is simple:

```yaml
services:
	app:
		image: jc21/nginx-proxy-manager:latest
		container_name: nginx-proxy-manager
		restart: unless-stopped
		network_mode: host
		volumes:
			- ./data:/data
			- ./letsencrypt:/etc/letsencrypt
```
*Note: The network mode must be host here*

After deploying this compose, access the manager on port 8000 and create an account. Go to the tab Access Lists and create a new access list which will be used for limiting access to the frontend for non-VPN connections. Call it `VPN Access` and in the rules tab in the allow field enter `100.64.0.0/10` and save.

Now under hosts go to proxy hosts and add a proxy host. This host will be the VPN access point, so we will be using the subdomain we set up earlier (`vpn.example.com`):
1. Enter the subdomain in the `Domain Names` field
2. Keep the scheme on HTTP (this is only for internal connections, external connections will still be secure)
3. Enter `127.0.0.1` in the `Forward Hostname / IP` field
4. Enter `8080` in the port field
5. Keep `Access List` on `Publicly Accessible` since this domain needs to be accessible for creating a VPN connection
6. Enable `Block Common Exploits` and `Websockets Support`

Under the SSL tab do the following:
1. Under `SSL Certificate` select `Request a new Certificate`
2. Enable `Force SSL`
3. Enable `HTTP/2 Support`
4. Enable `HSTS Enabled`

Next we will add the proxy host for the frontend application, so we will be using the subdomain we set up earlier (`app.example.com`):
1. Enter the subdomain in the `Domain Names` field
2. Keep the scheme on HTTP
3. Enter `127.0.0.1` in the `Forward Hostname / IP` field
4. Enter `3001` in the port field
5. Change `Access List` to the `VPN Access` access list we created earlier
6. Enable `Block Common Exploits` and `Websockets Support`

Under the SSL tab do the following:
1. Under `SSL Certificate` select `Request a new Certificate`
2. Enable `Force SSL`
3. Enable `HTTP/2 Support`
4. Enable `HSTS Enabled`

And that is it for the NGINX setup, this will keep SSL certificates up to date and make sure only people who are connected to the VPN can access the app.

### Headscale
For the VPN connection we will be using [Headscale](https://headscale.net/stable/) which is the self-host version of [Tailscale](https://tailscale.com/). This service was chosen, because it requires a one time setup for the user and does not redirect all internet traffic of the user over the VPN but only the traffic required for the frontend application.
The compose for headscale:
```yaml
services:
	headscale:
		image: headscale/headscale:latest
		container_name: headscale
		restart: unless-stopped
		ports:
			- 8080:8080 # Web/API
			- 9090:9090 # Metrics (optional)
		volumes:
			- /opt/headscale/config:/etc/headscale
			- /opt/headscale/data:/var/lib/headscale
		command: serve
```
*Note: The serve command is required for the service to start up*

Deploy the service and then access the config file at `/opt/headscale/config/config.yaml` using your favorite text editor. Then edit the following values:
1. `server_url`: Enter the subdomain we have created with the `https://` prefix. In the example this would be `https://vpn.example.com`
2. Under `dns` you will find a `extra_records` section, there you need to add our frontend access subdomain from earlier (`app.example.com`) like this:
   ```yaml
   extra_records:
	   - { name: "app.example.com", type: "A", value: "100.64.0.1" }
   ```
3. Save and Exit
4. Restart the Headscale compose to load the new config

Next we need to connect the server to the Headscale network. It is important that we connect the server first, since that makes sure the `100.64.0.1` IP is given to the server. To do this we first need to install Tailscale on the server by following the [documentation](https://tailscale.com/docs/install). After installing Tailscale do the following:
1. Create a new user by running the command `docker exec headscale headscale users create server`
2. Generate a pre-auth key by running the command `docker exec headscale headscale preauthkeys create --user 1`
3. Connect to the network by running the command `tailscale up --login-server=https://YOUR_SUBDOMAIN --authkey=YOUR_AUTH_KEY` where the subdomain is the one we made before (in the example it is `vpn.example.com`)
4. Verify the connection went well by running `docker exec headscale headscale nodes list` and see if the user `server` is connected and verify that the IP is `100.64.0.1`. If not, change the config again and update the value of the `extra_records` to the IP you see here and restart Headscale.

That is it for the Headscale setup. VPN users will be automatically created when inviting users to the platform, we will go over on how to access the platform for the first to create the first user later in this documentation after creating the frontend service.

### PostgreSQL Database
The platform needs a PostgreSQL database in order to run, so we need to set this up first before getting the server to run. First we need to figure out where our database storage will be stored, since preferably this would be on a scalable disk, on [Hetzner](https://www.hetzner.com/) this is called a volume. This preference is because on usage the database will get larger and larger, so when you reach max. capacity this disk easily allows to add more capacity without moving the whole database. For this documentation we assume a Hetzner setup with a volume called `db-volume`, but it works the same on other systems. The path to this volume will therefore be `/mnt/db-volume`, which we will use in the compose. In this compose we will also create a Docker network, in this example called `app-net`. This is an internal Docker network, making sure all the traffic for the application is isolated on that network, adding another layer of security. The Docker compose is as follows:

```yaml
services:
	database:
		image: pgvector/pgvector:pg16
		container_name: postgres-db
		environment:
			- POSTGRES_USER=postgres
			- POSTGRES_PASSWORD=CHANGE_ME
			- POSTGRES_DB=postgres
		volumes:
			- /mnt/db-volume/postgres:/var/lib/postgresql/data
		healthcheck:
			test:
				- CMD-SHELL
				- pg_isready -U postgres
			interval: 10s
			timeout: 5s
			retries: 5
		restart: unless-stopped
		networks:
			- app-net
networks:
	app-net:
		name: app-net
		driver: bridge
```

Deploy this compose and you are ready for the database setup. If you want to connect to the database to see the fields, it is recommended to set up a PgAdmin container in docker connected on the same Docker network and using SSH tunneling to securely access PgAdmin.

### Application Backend
Since we now have all the infrastructure up and running, it is now time to bring the backend online. The compose for the backend (Note that many of the values in the environment section are left empty and explained below in the table on what you need to fill in):
```yaml
services:
	backend:
		image: ghcr.io/charge-research-platform-volt/kennisbank-backend:latest
		container_name: backend
		environment:
			- ASPNETCORE_ENVIRONMENT=Production
			- HOST_URL=
			- DATABASE_CONNECTION_STRING=Host=postgres-db;Port=5432;Username=postgres;Password=YOUR_DB_PASSWORD;Database=postgres;Include Error Detail=false
			- S3_ENDPOINT=
			- S3_ACCESS_KEY=
			- S3_SECRET_KEY=
			- S3_USE_SSL=true
			- S3_REGION=
			- S3_BUCKET_NAME=
			- EMAIL_SMTP_HOST=
			- EMAIL_TLS_PORT=
			- EMAIL_ADDRESS=
			- EMAIL_PASSWORD=
			- EMAIL_FROM_NAME=
			- OwnerUser__Email=
			- OwnerUser__Password=
			- OwnerUser__FirstName=
			- OwnerUser__LastName=
			- DOCUMENT_INTELLIGENCE_CLIENT_ENDPOINT=
			- DOCUMENT_INTELLIGENCE_CLIENT_API_KEY=
			- CHAT_DEPLOYMENT_NAME=
			- AZURE_OPENAI_CLIENT_ENDPOINT=
			- AZURE_OPENAI_CLIENT_API_KEY=
			- EMBEDDINGS_MODEL_NAME=embed-v-4-0
			- EMBEDDINGS_CLIENT_ENDPOINT=
			- EMBEDDINGS_CLIENT_API_KEY=
			- HEADSCALE_URL=
			- HEADSCALE_API_KEY=
		healthcheck:
			test:
				- CMD
				- curl
				- -f
				- http://localhost:8080/status
			interval: 30s
			timeout: 10s
			retries: 3
			start_period: 60s
		restart: unless-stopped
		networks:
			- app-net
networks:
	app-net:
		external: true
```
*Note: The network name should be changed appropriately to the network created in the postgres compose*

Values to enter:

| Value Name                            | Value                                                                                                                                                                                                                                  |
| ------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| HOST_URL                              | This is the URL on where the frontend app will be accessed, so this is the same as the subdomain we created earlier (`app.example.com` in the example)                                                                                 |
| DATABASE_CONNECTION_STRING            | In this string it is needed to change the password part to the password you created before for the PostgreSQL compose.                                                                                                                 |
| S3_ENDPOINT                           | This value should be the URL to your S3 bucket, which can be found on the control panel of your S3 Storage provider.                                                                                                                   |
| S3_ACCESS_KEY                         | This value should be the access key of your S3 bucket, which can be found on the control panel of your S3 Storage provider.                                                                                                            |
| S3_SECRET_KEY                         | This value should be the secret key of your S3 bucket, which can be found on the control panel of your S3 Storage provider.                                                                                                            |
| S3_REGION                             | This should be the region of your S3 bucket, which can be found on the control panel of your S3 Storage provider. On Hetzner this is the part that comes before your-objectstorage.com in your URL (e.g., `nbg1`)                      |
| S3_BUCKET_NAME                        | This should be the name that you gave your S3 bucket, which can be found on the control panel of your S3 Storage provider.                                                                                                             |
| EMAIL_SMTP_HOST                       | This should be the SMTP endpoint of your email provider (e.g., `smtp.gmail.com`)                                                                                                                                                       |
| EMAIL_TLS_PORT                        | This should be the TLS port of your email provider (often `587`)                                                                                                                                                                       |
| EMAIL_ADDRESS                         | This should be the email address which will be used to send invitations to the platform.                                                                                                                                               |
| EMAIL_PASSWORD                        | This should be the password of the email address entered above.                                                                                                                                                                        |
| EMAIL_FROM_NAME                       | This name will displayed at the top of the email as the origin (e.g., `Knowledgebank Charge`)                                                                                                                                          |
| OwnerUser__Email                      | This will be the email address used to log in to the owner account of the platform. This email address does not necessarily need to exist. (*Note: The owner account cannot be used as a normal user, so don't choose your own email*) |
| OwnerUser__Password                   | This will be the password used to log into the owner account                                                                                                                                                                           |
| OwnerUser__FirstName                  | This is the first name that will be displayed on the owner account in the application                                                                                                                                                  |
| OwnerUser__LastName                   | This is the last name that will be displayed on the owner account in the application                                                                                                                                                   |
| DOCUMENT_INTELLIGENCE_CLIENT_ENDPOINT | This should be the URL to the Document Intelligence endpoint provided by Azure (e.g., `https://example.cognitiveservices.azure.com/`)                                                                                                  |
| DOCUMENT_INTELLIGENCE_CLIENT_API_KEY  | This should be the API key for Document Intelligence provided by Azure                                                                                                                                                                 |
| CHAT_DEPLOYMENT_NAME                  | This should be the name of the model that you want to use. You first have to configure the model in Azure. (e.g., `gpt-5-nano`)                                                                                                        |
| AZURE_OPENAI_CLIENT_ENDPOINT          | This should be the URL to the AI endpoint provided by Azure (e.g., `https://example-resource.cognitiveservices.azure.com/`)                                                                                                            |
| AZURE_OPENAI_API_KEY                  | This should be the API key for AI provided by Azure.                                                                                                                                                                                   |
| EMBEDDINGS_CLIENT_ENDPOINT            | This should be the URL to the embedding endpoint provided by Azure (e.g., `https://example-resource.services.ai.azure.com/models`)                                                                                                     |
| EMBEDDINGS_CLIENT_API_KEY             | This should be the API key for embedding provided by Azure                                                                                                                                                                             |
| HEADSCALE_URL                         | This should be the URL to the Headscale server we just set up. In the example this was `https://vpn.example.com`.                                                                                                                      |
| HEADSCALE_API_KEY                     | This should be the API key for your Headscale instance. This can be generated by running the following command on the server: `docker exec headscale headscale apikeys create`                                                         |

After filling in all environment values, deploy the compose. If everything went well, the command `docker logs backend | tail -10` should say "Backend started successfully" at the bottom.

### Application Frontend
The last part of this service is the frontend, which is the part the users will actually connect to. The compose for the frontend looks like this:
```yaml
services:
	frontend:
		image: ghcr.io/charge-research-platform-volt/kennisbank-frontend:latest
		container_name: frontend
		environment:
			- API_URL=http://backend:8080
			- HELP_URL=https://charge-volt.github.io/
			- NEXT_PUBLIC_TIMEZONE=Europe/Berlin # Change to your timezone
		restart: unless-stopped
		networks:
			- app-net
		ports:
			- 127.0.0.1:3001:3000
networks:
	app-net:
		external: true
```
*Note: The network name should be changed appropriately to the network created in the postgres compose*

Deploy the compose and check if everything is working correctly by running the command `docker logs frontend | tail -10`. It should say "Ready" at the bottom.

That was it! The application is now fully operational. The only thing left to do is inviting yourself to the platform so that you can connect to the VPN network and access the application. There is two ways to do this, the recommended way is using SSH tunneling to forward port 3001 directly to your PC, so that you can connect to `localhost:3001` to access the application. The second option is to create a VPN user and connect to the network (same as for the server user) and access it normally through `https://app.example.com`, however, it is then also best to remove this VPN user after for security reasons and also log out of the Headscale network before connecting normally using your app account.
Once you are connected to the application, log in using the Owner Account details you provided in the backend setup. After logging in, go to the Users tab and invite yourself through email. Log out of the owner account and follow the instructions in the email, this will get you connected to the Headscale network and lead you to the sign up page. After creating your account, log back into the owner account and make yourself an admin in the user page.

Done! Now you can use your own account to invite others. The owner account is only there for initial setup and fallback (this account cannot be deleted) but should not be used as it has some special configuration which can create some issues with normal use.