#!/bin/bash

# Deploy KnowledgeBank to GitHub Container Registry (GHCR)
# Repository: Charge-Research-Platform-Volt/Kennisbank

set -e  # Exit on error

# Colors for output
GREEN='\033[0;32m'
BLUE='\033[0;34m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m' # No Color

echo -e "${BLUE}╔════════════════════════════════════════════════════════════╗${NC}"
echo -e "${BLUE}║  KnowledgeBank - Deploy to GitHub Container Registry      ║${NC}"
echo -e "${BLUE}╚════════════════════════════════════════════════════════════╝${NC}"
echo ""

# Configuration
REPO_OWNER="charge-research-platform-volt"
REPO_NAME="kennisbank"
GHCR_BASE="ghcr.io/${REPO_OWNER}/${REPO_NAME}"
CREDENTIALS_FILE="${HOME}/.ghcr-credentials"

# Get version tag (current branch name or tag)
BRANCH=$(git rev-parse --abbrev-ref HEAD)
COMMIT_SHORT=$(git rev-parse --short HEAD)
VERSION="${BRANCH}-${COMMIT_SHORT}"

echo -e "${BLUE}Configuration:${NC}"
echo "  Repository: ${REPO_OWNER}/${REPO_NAME}"
echo "  Branch: ${BRANCH}"
echo "  Commit: ${COMMIT_SHORT}"
echo "  Version tag: ${VERSION}"
echo ""

# Check if logged into GitHub Container Registry
echo -e "${YELLOW}Checking GHCR authentication...${NC}"

# Check if we have stored credentials - this is the source of truth
if [ -f "$CREDENTIALS_FILE" ]; then
    source "$CREDENTIALS_FILE"
    if [ -n "$GHCR_TOKEN" ] && [ -n "$GHCR_USER" ]; then
        # Re-login to ensure Docker has current credentials
        echo "$GHCR_TOKEN" | docker login ghcr.io -u "$GHCR_USER" --password-stdin >/dev/null 2>&1
        if [ $? -eq 0 ]; then
            echo -e "${GREEN}✓ Authenticated with GHCR (using saved credentials)${NC}"
        else
            echo -e "${RED}Saved credentials invalid, clearing...${NC}"
            rm -f "$CREDENTIALS_FILE"
            GHCR_TOKEN=""
            GHCR_USER=""
        fi
    fi
fi

# If no valid credentials, prompt for them
if [ -z "$GHCR_TOKEN" ] || [ -z "$GHCR_USER" ]; then
    echo -e "${YELLOW}Not logged into GHCR. Starting one-time setup...${NC}"
    echo ""
    echo -e "${YELLOW}To authenticate, you need a GitHub Personal Access Token (PAT):${NC}"
    echo "  1. Go to: https://github.com/settings/tokens (classic token)"
    echo "  2. Give it a name: 'GHCR Deploy'"
    echo "  3. Select scopes: 'write:packages' and 'read:packages'"
    echo "  4. Set expiration to 'No expiration' (or your preference)"
    echo "  5. Generate token and copy it"
    echo "  6. If using an org repo: Configure SSO → Authorize for the org"
    echo ""

    read -p "Enter your GitHub username: " GHCR_USER
    read -sp "Enter your GitHub PAT token: " GHCR_TOKEN
    echo ""

    # Save credentials locally (not in repo)
    echo "# GHCR credentials - DO NOT COMMIT THIS FILE" > "$CREDENTIALS_FILE"
    echo "GHCR_USER=\"$GHCR_USER\"" >> "$CREDENTIALS_FILE"
    echo "GHCR_TOKEN=\"$GHCR_TOKEN\"" >> "$CREDENTIALS_FILE"
    chmod 600 "$CREDENTIALS_FILE"

    echo -e "${GREEN}✓ Credentials saved to ${CREDENTIALS_FILE}${NC}"

    # Login to GHCR
    echo "$GHCR_TOKEN" | docker login ghcr.io -u "$GHCR_USER" --password-stdin

    if [ $? -eq 0 ]; then
        echo -e "${GREEN}✓ Authenticated with GHCR${NC}"
    else
        echo -e "${RED}❌ Authentication failed${NC}"
        rm -f "$CREDENTIALS_FILE"
        exit 1
    fi
fi

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
if ! docker push ${GHCR_BASE}-backend:latest || ! docker push ${GHCR_BASE}-backend:${VERSION}; then
    echo -e "${RED}❌ Push failed. Clearing credentials for next run.${NC}"
    rm -f "$CREDENTIALS_FILE"
    docker logout ghcr.io 2>/dev/null || true
    echo -e "${YELLOW}Run the script again to re-authenticate.${NC}"
    exit 1
fi
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
if ! docker push ${GHCR_BASE}-frontend:latest || ! docker push ${GHCR_BASE}-frontend:${VERSION}; then
    echo -e "${RED}❌ Push failed. Clearing credentials for next run.${NC}"
    rm -f "$CREDENTIALS_FILE"
    docker logout ghcr.io 2>/dev/null || true
    echo -e "${YELLOW}Run the script again to re-authenticate.${NC}"
    exit 1
fi
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
echo "  1. On your server, pull and restart in Dockge"
echo "  2. Or run: docker pull ${GHCR_BASE}-backend:latest"
echo ""

# This program has been developed by students from the bachelor Computer Science at Utrecht
# University within the Software Project course.
# © Copyright Utrecht University (Department of Information and Computing Sciences)