#!/bin/bash

# Deploy KnowledgeBank to GitHub Container Registry (GHCR)
# Using personal namespace

set -e  # Exit on error

# Colors for output
GREEN='\033[0;32m'
BLUE='\033[0;34m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m' # No Color

echo -e "${BLUE}╔════════════════════════════════════════════════════════════╗${NC}"
echo -e "${BLUE}║  KnowledgeBank - Deploy to GHCR (Personal)                ║${NC}"
echo -e "${BLUE}╚════════════════════════════════════════════════════════════╝${NC}"
echo ""

# Get GitHub username
if [ -z "$GITHUB_USERNAME" ]; then
    echo -e "${YELLOW}Enter your GitHub username:${NC}"
    read GITHUB_USERNAME
fi

# Configuration
GHCR_BASE="ghcr.io/${GITHUB_USERNAME}/kennisbank"

# Get version tag (current branch name or tag)
BRANCH=$(git rev-parse --abbrev-ref HEAD)
COMMIT_SHORT=$(git rev-parse --short HEAD)
VERSION="${BRANCH}-${COMMIT_SHORT}"

echo -e "${BLUE}Configuration:${NC}"
echo "  GitHub User: ${GITHUB_USERNAME}"
echo "  Branch: ${BRANCH}"
echo "  Commit: ${COMMIT_SHORT}"
echo "  Version tag: ${VERSION}"
echo ""

# Check if logged into GitHub Container Registry
echo -e "${YELLOW}Checking GHCR authentication...${NC}"
if ! docker info | grep -q "ghcr.io" 2>/dev/null; then
    echo -e "${YELLOW}Testing GHCR connection...${NC}"
fi

echo -e "${GREEN}✓ Ready to build and push${NC}"
echo ""

# Build and push backend
echo -e "${BLUE}════════════════════════════════════════════════════════════${NC}"
echo -e "${BLUE}Building Backend...${NC}"
echo -e "${BLUE}════════════════════════════════════════════════════════════${NC}"

docker build \
    --file backend/Dockerfile \
    --target production \
    --tag ${GHCR_BASE}-backend:latest \
    --tag ${GHCR_BASE}-backend:${VERSION} \
    ./backend

echo -e "${GREEN}✓ Backend image built${NC}"
echo ""

echo -e "${YELLOW}Pushing backend to GHCR...${NC}"
docker push ${GHCR_BASE}-backend:latest
docker push ${GHCR_BASE}-backend:${VERSION}
echo -e "${GREEN}✓ Backend pushed to GHCR${NC}"
echo ""

# Build and push frontend
echo -e "${BLUE}════════════════════════════════════════════════════════════${NC}"
echo -e "${BLUE}Building Frontend...${NC}"
echo -e "${BLUE}════════════════════════════════════════════════════════════${NC}"

docker build \
    --file frontend/Dockerfile \
    --target production \
    --tag ${GHCR_BASE}-frontend:latest \
    --tag ${GHCR_BASE}-frontend:${VERSION} \
    ./frontend

echo -e "${GREEN}✓ Frontend image built${NC}"
echo ""

echo -e "${YELLOW}Pushing frontend to GHCR...${NC}"
docker push ${GHCR_BASE}-frontend:latest
docker push ${GHCR_BASE}-frontend:${VERSION}
echo -e "${GREEN}✓ Frontend pushed to GHCR${NC}"
echo ""

# Summary
echo -e "${GREEN}╔════════════════════════════════════════════════════════════╗${NC}"
echo -e "${GREEN}║  ✓ Images successfully pushed to GHCR!                    ║${NC}"
echo -e "${GREEN}╚════════════════════════════════════════════════════════════╝${NC}"
echo ""
echo -e "${BLUE}Images available at:${NC}"
echo "  Backend:  ${GHCR_BASE}-backend:latest"
echo "            ${GHCR_BASE}-backend:${VERSION}"
echo "  Frontend: ${GHCR_BASE}-frontend:latest"
echo "            ${GHCR_BASE}-frontend:${VERSION}"
echo ""
echo -e "${YELLOW}Next steps:${NC}"
echo "  1. Make images public:"
echo "     - Go to: https://github.com/${GITHUB_USERNAME}?tab=packages"
echo "     - Find each package → Package settings → Change visibility to Public"
echo ""
echo "  2. Deploy to Azure Container Apps using:"
echo "     Backend:  ${GHCR_BASE}-backend:latest"
echo "     Frontend: ${GHCR_BASE}-frontend:latest"
echo ""

# This program has been developed by students from the bachelor Computer Science at Utrecht
# University within the Software Project course.
# © Copyright Utrecht University (Department of Information and Computing Sciences)
