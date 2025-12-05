# Azure Container Apps Deployment Guide

This guide walks you through deploying KnowledgeBank to Azure Container Apps using GitHub Container Registry (GHCR).

## Prerequisites

- Azure CLI installed: `az --version`
- Docker installed: `docker --version`
- GitHub account with contributor access to the repository
- Azure subscription

## Step 1: Authenticate with GitHub Container Registry (GHCR)

### 1.1 Create GitHub Personal Access Token (PAT)

1. Go to: https://github.com/settings/tokens?type=beta
2. Click **"Generate new token"** (classic)
3. Give it a name: `GHCR Deploy`
4. Select scopes:
   - ✅ `write:packages`
   - ✅ `read:packages`
5. Click **"Generate token"**
6. **Copy the token** (you won't see it again!)

### 1.2 Login to GHCR

```bash
# Set your token as environment variable
export CR_PAT=YOUR_TOKEN_HERE

# Login to GitHub Container Registry
echo $CR_PAT | docker login ghcr.io -u YOUR_GITHUB_USERNAME --password-stdin
```

## Step 2: Build and Push Images to GHCR

```bash
# Make the deploy script executable
chmod +x deploy-to-ghcr.sh

# Run the deployment script
./deploy-to-ghcr.sh
```

This will:
- Build backend and frontend Docker images
- Tag them with `latest` and your branch+commit
- Push to `ghcr.io/charge-research-platform-volt/kennisbank-backend:latest`
- Push to `ghcr.io/charge-research-platform-volt/kennisbank-frontend:latest`

### 2.1 Make Images Public (Important!)

For Azure to pull images without authentication, make them public:

1. Go to: https://github.com/orgs/Charge-Research-Platform-Volt/packages
2. Click on `kennisbank-backend`
3. Go to **Package settings** (bottom right)
4. Under **Danger Zone** → **Change visibility**
5. Select **Public** and confirm
6. Repeat for `kennisbank-frontend`

**Alternative:** If you keep images private, you'll need to configure Azure with GHCR credentials (see Step 5).

## Step 3: Setup Azure Container Apps Environment

### 3.1 Login to Azure

```bash
# Login to Azure
az login

# Set your subscription (if you have multiple)
az account set --subscription "YOUR_SUBSCRIPTION_NAME_OR_ID"
```

### 3.2 Create Resource Group

```bash
# Choose a region (e.g., westeurope, eastus, etc.)
LOCATION="westeurope"
RESOURCE_GROUP="knowledgebank-rg"

az group create \
  --name $RESOURCE_GROUP \
  --location $LOCATION
```

### 3.3 Create Container Apps Environment

```bash
ENVIRONMENT_NAME="knowledgebank-env"

az containerapp env create \
  --name $ENVIRONMENT_NAME \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION
```

This creates a managed environment for your containers (~5 minutes).

## Step 4: Deploy Infrastructure Services

You need PostgreSQL, Storage, and Qdrant. Choose one approach:

### Option A: Azure Managed Services (Recommended for Production)

#### PostgreSQL Flexible Server
```bash
DB_SERVER="knowledgebank-db"
DB_ADMIN="kbadmin"
DB_PASSWORD="ChangeThisSecurePassword123!"  # Change this!

az postgres flexible-server create \
  --resource-group $RESOURCE_GROUP \
  --name $DB_SERVER \
  --location $LOCATION \
  --admin-user $DB_ADMIN \
  --admin-password $DB_PASSWORD \
  --sku-name Standard_B1ms \
  --tier Burstable \
  --storage-size 32 \
  --version 14

# Allow Azure services to connect
az postgres flexible-server firewall-rule create \
  --resource-group $RESOURCE_GROUP \
  --name $DB_SERVER \
  --rule-name AllowAzureServices \
  --start-ip-address 0.0.0.0 \
  --end-ip-address 0.0.0.0

# Get connection string
DB_CONNECTION_STRING="Host=${DB_SERVER}.postgres.database.azure.com;Port=5432;Username=${DB_ADMIN};Password=${DB_PASSWORD};Database=postgres;SslMode=Require"
```

#### Azure Blob Storage
```bash
STORAGE_ACCOUNT="knowledgebankstore"  # Must be globally unique, lowercase, no hyphens

az storage account create \
  --name $STORAGE_ACCOUNT \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION \
  --sku Standard_LRS

# Get connection string
STORAGE_CONNECTION_STRING=$(az storage account show-connection-string \
  --name $STORAGE_ACCOUNT \
  --resource-group $RESOURCE_GROUP \
  --output tsv)
```

#### Qdrant (as Container App)
```bash
az containerapp create \
  --name qdrant \
  --resource-group $RESOURCE_GROUP \
  --environment $ENVIRONMENT_NAME \
  --image qdrant/qdrant:latest \
  --target-port 6333 \
  --ingress internal \
  --min-replicas 1 \
  --max-replicas 1 \
  --cpu 0.5 \
  --memory 1Gi
```

### Option B: All Containers (Cheaper for Testing)

Deploy PostgreSQL, Azurite, and Qdrant as container apps with persistent storage.

**Note:** This is simpler and cheaper but less reliable for production.

## Step 5: Deploy Backend

```bash
az containerapp create \
  --name backend \
  --resource-group $RESOURCE_GROUP \
  --environment $ENVIRONMENT_NAME \
  --image ghcr.io/charge-research-platform-volt/kennisbank-backend:latest \
  --target-port 8080 \
  --ingress external \
  --min-replicas 1 \
  --max-replicas 3 \
  --cpu 0.5 \
  --memory 1Gi \
  --env-vars \
    "ASPNETCORE_ENVIRONMENT=Production" \
    "DATABASE_CONNECTION_STRING=${DB_CONNECTION_STRING}" \
    "STORAGE_CONNECTION_STRING=${STORAGE_CONNECTION_STRING}" \
    "QDRANT_HOST=qdrant" \
    "QDRANT_HTTPS=false" \
    "QDRANT_API_KEY=null" \
    "HOST_URL=https://YOUR_FRONTEND_URL" \
    "AZURE_OPENAI_CLIENT_ENDPOINT=YOUR_OPENAI_ENDPOINT" \
    "AZURE_OPENAI_CLIENT_API_KEY=YOUR_OPENAI_KEY" \
    "EMBEDDINGS_CLIENT_ENDPOINT=YOUR_EMBEDDINGS_ENDPOINT" \
    "EMBEDDINGS_CLIENT_API_KEY=YOUR_EMBEDDINGS_KEY" \
    "DOCUMENT_INTELLIGENCE_CLIENT_ENDPOINT=YOUR_DOC_INTEL_ENDPOINT" \
    "DOCUMENT_INTELLIGENCE_CLIENT_API_KEY=YOUR_DOC_INTEL_KEY" \
    "OwnerUser__Email=admin@example.com" \
    "OwnerUser__Password=ChangeThis123!" \
    "EMAIL_SMTP_HOST=smtp.gmail.com" \
    "EMAIL_TLS_PORT=587" \
    "EMAIL_ADDRESS=your-email@gmail.com" \
    "EMAIL_PASSWORD=your-app-password"

# Get backend URL
BACKEND_URL=$(az containerapp show \
  --name backend \
  --resource-group $RESOURCE_GROUP \
  --query properties.configuration.ingress.fqdn \
  --output tsv)

echo "Backend URL: https://${BACKEND_URL}"
```

## Step 6: Deploy Frontend

```bash
az containerapp create \
  --name frontend \
  --resource-group $RESOURCE_GROUP \
  --environment $ENVIRONMENT_NAME \
  --image ghcr.io/charge-research-platform-volt/kennisbank-frontend:latest \
  --target-port 3000 \
  --ingress external \
  --min-replicas 1 \
  --max-replicas 3 \
  --cpu 0.25 \
  --memory 0.5Gi \
  --env-vars \
    "API_URL=https://${BACKEND_URL}" \
    "NEXT_PUBLIC_TIMEZONE=Europe/Amsterdam"

# Get frontend URL
FRONTEND_URL=$(az containerapp show \
  --name frontend \
  --resource-group $RESOURCE_GROUP \
  --query properties.configuration.ingress.fqdn \
  --output tsv)

echo ""
echo "✓ Deployment complete!"
echo "Frontend: https://${FRONTEND_URL}"
echo "Backend:  https://${BACKEND_URL}"
```

## Step 7: Update Backend with Frontend URL

After creating the frontend, update the backend with the correct HOST_URL:

```bash
az containerapp update \
  --name backend \
  --resource-group $RESOURCE_GROUP \
  --set-env-vars "HOST_URL=https://${FRONTEND_URL}"
```

## Step 8: View Logs and Monitor

```bash
# View backend logs
az containerapp logs show \
  --name backend \
  --resource-group $RESOURCE_GROUP \
  --follow

# View frontend logs
az containerapp logs show \
  --name frontend \
  --resource-group $RESOURCE_GROUP \
  --follow

# Check application status
az containerapp list \
  --resource-group $RESOURCE_GROUP \
  --output table
```

## Updating Your Deployment

When you make changes to your code:

```bash
# 1. Build and push new images
./deploy-to-ghcr.sh

# 2. Create new revisions in Azure Container Apps
az containerapp update \
  --name backend \
  --resource-group $RESOURCE_GROUP \
  --image ghcr.io/charge-research-platform-volt/kennisbank-backend:latest

az containerapp update \
  --name frontend \
  --resource-group $RESOURCE_GROUP \
  --image ghcr.io/charge-research-platform-volt/kennisbank-frontend:latest
```

## Cost Optimization

### Free Tier Eligibility
Azure Container Apps has a free tier:
- 180,000 vCPU-seconds
- 360,000 GiB-seconds of memory
- 2 million HTTP requests

For minimal usage, you might stay within free tier!

### Cost Estimates (Pay-as-you-go)
- **Container Apps**: ~$15-30/month (with auto-scaling)
- **PostgreSQL Flexible Server (Burstable B1ms)**: ~$12/month
- **Blob Storage**: ~$1-5/month (depending on usage)
- **Total**: ~$30-50/month for light usage

### Reduce Costs
- Use `--min-replicas 0` for non-critical environments (scales to zero when idle)
- Use Burstable tier for PostgreSQL
- Delete resources when not in use

## Troubleshooting

### Container won't start
```bash
# Check container logs
az containerapp logs show --name backend --resource-group $RESOURCE_GROUP --tail 100

# Check replica status
az containerapp replica list --name backend --resource-group $RESOURCE_GROUP
```

### Database connection issues
- Ensure firewall rules allow Azure services
- Check connection string format (SslMode=Require for Azure PostgreSQL)
- Verify database exists: `az postgres flexible-server db list`

### Images not pulling
- Make GHCR packages public, OR
- Add registry credentials to Container App:
```bash
az containerapp registry set \
  --name backend \
  --resource-group $RESOURCE_GROUP \
  --server ghcr.io \
  --username YOUR_GITHUB_USERNAME \
  --password $CR_PAT
```

## Clean Up

To delete all resources:

```bash
az group delete --name $RESOURCE_GROUP --yes --no-wait
```

---

© Copyright Utrecht University (Department of Information and Computing Sciences)
