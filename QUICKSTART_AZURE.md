# Azure Deployment - Quick Start

**Fast track guide to deploy KnowledgeBank to Azure Container Apps**

## Prerequisites Check

```bash
# Verify tools are installed
docker --version
az --version
git --version
```

## 1️⃣ Setup GitHub Container Registry (One-time)

### Create GitHub PAT (Personal Access Token)
1. Go to: https://github.com/settings/tokens?type=beta
2. Click **"Generate new token"** (classic)
3. Name: `GHCR Deploy`
4. Scopes: ✅ `write:packages` ✅ `read:packages`
5. Copy the token

### Login to GHCR
```bash
export CR_PAT=YOUR_TOKEN_HERE
echo $CR_PAT | docker login ghcr.io -u YOUR_GITHUB_USERNAME --password-stdin
```

## 2️⃣ Build & Push Images

```bash
./deploy-to-ghcr.sh
```

**Make images public:**
- Visit: https://github.com/orgs/Charge-Research-Platform-Volt/packages
- For each package (backend, frontend):
  - Package settings → Change visibility → Public

## 3️⃣ Deploy to Azure

### Login & Setup
```bash
az login
az account set --subscription "YOUR_SUBSCRIPTION"

# Variables
RESOURCE_GROUP="knowledgebank-rg"
LOCATION="westeurope"
ENVIRONMENT="knowledgebank-env"
```

### Create Environment
```bash
# Resource group
az group create --name $RESOURCE_GROUP --location $LOCATION

# Container Apps environment
az containerapp env create \
  --name $ENVIRONMENT \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION
```

### Deploy Services

**Option A: Minimal (Cheapest) - Everything as Containers**

```bash
# PostgreSQL (as container with volume)
az containerapp create \
  --name database \
  --resource-group $RESOURCE_GROUP \
  --environment $ENVIRONMENT \
  --image postgres:latest \
  --target-port 5432 \
  --ingress internal \
  --min-replicas 1 \
  --max-replicas 1 \
  --cpu 0.5 \
  --memory 1Gi \
  --env-vars \
    "POSTGRES_USER=postgres" \
    "POSTGRES_PASSWORD=YourSecurePassword123" \
    "POSTGRES_DB=postgres"

# Azurite (Storage Emulator)
az containerapp create \
  --name storage \
  --resource-group $RESOURCE_GROUP \
  --environment $ENVIRONMENT \
  --image mcr.microsoft.com/azure-storage/azurite \
  --target-port 10000 \
  --ingress internal \
  --min-replicas 1 \
  --max-replicas 1 \
  --cpu 0.25 \
  --memory 0.5Gi

# Qdrant
az containerapp create \
  --name qdrant \
  --resource-group $RESOURCE_GROUP \
  --environment $ENVIRONMENT \
  --image qdrant/qdrant:latest \
  --target-port 6333 \
  --ingress internal \
  --min-replicas 1 \
  --max-replicas 1 \
  --cpu 0.5 \
  --memory 1Gi
```

### Deploy Backend

```bash
az containerapp create \
  --name backend \
  --resource-group $RESOURCE_GROUP \
  --environment $ENVIRONMENT \
  --image ghcr.io/charge-research-platform-volt/kennisbank-backend:latest \
  --target-port 8080 \
  --ingress external \
  --min-replicas 1 \
  --max-replicas 2 \
  --cpu 0.5 \
  --memory 1Gi \
  --env-vars \
    "ASPNETCORE_ENVIRONMENT=Production" \
    "DATABASE_CONNECTION_STRING=Host=database;Port=5432;Username=postgres;Password=YourSecurePassword123;Database=postgres;Include Error Detail=false" \
    "STORAGE_CONNECTION_STRING=DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://storage:10000/devstoreaccount1;" \
    "QDRANT_HOST=qdrant" \
    "QDRANT_HTTPS=false" \
    "QDRANT_API_KEY=null" \
    "HOST_URL=https://FRONTEND_URL" \
    "AZURE_OPENAI_CLIENT_ENDPOINT=YOUR_ENDPOINT" \
    "AZURE_OPENAI_CLIENT_API_KEY=YOUR_KEY" \
    "EMBEDDINGS_CLIENT_ENDPOINT=YOUR_ENDPOINT" \
    "EMBEDDINGS_CLIENT_API_KEY=YOUR_KEY" \
    "DOCUMENT_INTELLIGENCE_CLIENT_ENDPOINT=YOUR_ENDPOINT" \
    "DOCUMENT_INTELLIGENCE_CLIENT_API_KEY=YOUR_KEY" \
    "OwnerUser__Email=admin@example.com" \
    "OwnerUser__Password=ChangeThis123"

# Get backend URL
BACKEND_URL=$(az containerapp show --name backend --resource-group $RESOURCE_GROUP --query properties.configuration.ingress.fqdn -o tsv)
echo "Backend: https://${BACKEND_URL}"
```

### Deploy Frontend

```bash
az containerapp create \
  --name frontend \
  --resource-group $RESOURCE_GROUP \
  --environment $ENVIRONMENT \
  --image ghcr.io/charge-research-platform-volt/kennisbank-frontend:latest \
  --target-port 3000 \
  --ingress external \
  --min-replicas 1 \
  --max-replicas 2 \
  --cpu 0.25 \
  --memory 0.5Gi \
  --env-vars \
    "API_URL=https://${BACKEND_URL}" \
    "NEXT_PUBLIC_TIMEZONE=Europe/Amsterdam"

# Get frontend URL
FRONTEND_URL=$(az containerapp show --name frontend --resource-group $RESOURCE_GROUP --query properties.configuration.ingress.fqdn -o tsv)
echo "Frontend: https://${FRONTEND_URL}"
```

### Update Backend with Frontend URL

```bash
az containerapp update \
  --name backend \
  --resource-group $RESOURCE_GROUP \
  --set-env-vars "HOST_URL=https://${FRONTEND_URL}"
```

## 4️⃣ Verify Deployment

```bash
# Check status
az containerapp list --resource-group $RESOURCE_GROUP --output table

# View logs
az containerapp logs show --name backend --resource-group $RESOURCE_GROUP --tail 50
az containerapp logs show --name frontend --resource-group $RESOURCE_GROUP --tail 50
```

## 🔄 Update After Code Changes

```bash
# 1. Rebuild and push images
./deploy-to-ghcr.sh

# 2. Update Azure containers
az containerapp update --name backend --resource-group $RESOURCE_GROUP \
  --image ghcr.io/charge-research-platform-volt/kennisbank-backend:latest

az containerapp update --name frontend --resource-group $RESOURCE_GROUP \
  --image ghcr.io/charge-research-platform-volt/kennisbank-frontend:latest
```

## 💰 Estimated Costs

**Minimal setup (all containers):** ~$20-30/month
- Container Apps: ~$15-25/month
- Networking: ~$5/month

**With managed services:** ~$40-60/month
- PostgreSQL Flexible Server: +$12/month
- Azure Blob Storage: +$2-5/month

**Free tier:** May stay free for very light usage!

## 🗑️ Clean Up

```bash
# Delete everything
az group delete --name $RESOURCE_GROUP --yes
```

## 📚 Need More Details?

See `AZURE_DEPLOY.md` for:
- Managed Azure services (PostgreSQL, Blob Storage)
- Advanced configuration
- Troubleshooting
- Security best practices

---

**You don't need repository settings access** - as a contributor with write access, you can push to GHCR! 🎉

© Copyright Utrecht University (Department of Information and Computing Sciences)
