#!/bin/bash
# Test script for chunked file upload

# Configuration
API_URL="http://localhost:8080"
FILE_PATH="$1"
EMAIL="${2:-admin@admin.nl}"
PASSWORD="${3:-Admin123!}"

if [ -z "$FILE_PATH" ]; then
    echo "Usage: ./test-upload.sh <file-path> [email] [password]"
    echo "Example: ./test-upload.sh test.pdf"
    echo "         ./test-upload.sh test.pdf user@example.com MyPassword"
    exit 1
fi

if [ ! -f "$FILE_PATH" ]; then
    echo "Error: File '$FILE_PATH' not found"
    exit 1
fi

FILE_NAME=$(basename "$FILE_PATH")
FILE_SIZE=$(stat -c%s "$FILE_PATH" 2>/dev/null || stat -f%z "$FILE_PATH" 2>/dev/null)
CHUNK_SIZE=$((4 * 1024 * 1024)) # 4MB chunks
COOKIE_FILE=$(mktemp)

echo "================================================"
echo "Testing Chunked File Upload"
echo "================================================"
echo "File: $FILE_NAME"
echo "Size: $FILE_SIZE bytes"
echo "Chunk size: $CHUNK_SIZE bytes"
echo ""

# Step 0: Login
echo "Step 0: Authenticating..."
LOGIN_RESPONSE=$(curl -s -c "$COOKIE_FILE" -X POST "$API_URL/Auth/login?useCookies=true&useSessionCookies=true" \
    -H "Content-Type: application/json" \
    -d "{\"email\": \"$EMAIL\", \"password\": \"$PASSWORD\"}")

if echo "$LOGIN_RESPONSE" | grep -q "error\|Error"; then
    echo "Login failed: $LOGIN_RESPONSE"
    rm -f "$COOKIE_FILE"
    exit 1
fi

echo "✓ Authenticated as $EMAIL"
echo ""

# Step 1: Initialize upload
echo "Step 1: Initializing upload..."
INIT_RESPONSE=$(curl -s -b "$COOKIE_FILE" -X POST "$API_URL/files/upload/init" \
    -H "Content-Type: application/json" \
    -d "{\"fileName\": \"$FILE_NAME\", \"fileSize\": $FILE_SIZE}")

echo "Response: $INIT_RESPONSE"

# Extract GUID from response
GUID=$(echo $INIT_RESPONSE | grep -oP '"guid":"[^"]*"' | cut -d'"' -f4)

if [ -z "$GUID" ]; then
    echo "Error: Failed to get GUID from init response"
    exit 1
fi

echo "✓ Got GUID: $GUID"
echo ""

# Step 2: Upload chunks
echo "Step 2: Uploading chunks..."
OFFSET=0
BLOCK_INDEX=0
BLOCK_IDS=()

while [ $OFFSET -lt $FILE_SIZE ]; do
    # Calculate chunk size for this iteration
    REMAINING=$((FILE_SIZE - OFFSET))
    CURRENT_CHUNK_SIZE=$CHUNK_SIZE
    if [ $REMAINING -lt $CHUNK_SIZE ]; then
        CURRENT_CHUNK_SIZE=$REMAINING
    fi

    # Generate block ID (64-character hex string)
    BLOCK_ID=$(printf "%064x" $BLOCK_INDEX)
    BLOCK_IDS+=($BLOCK_ID)

    echo "  Uploading chunk $((BLOCK_INDEX + 1)): offset=$OFFSET, size=$CURRENT_CHUNK_SIZE"

    # Extract chunk and upload
    dd if="$FILE_PATH" bs=1 skip=$OFFSET count=$CURRENT_CHUNK_SIZE 2>/dev/null | \
    curl -s -b "$COOKIE_FILE" -X POST "$API_URL/files/upload/chunk/$GUID/$BLOCK_ID" \
        -H "Content-Type: application/octet-stream" \
        --data-binary @- > /dev/null

    if [ $? -ne 0 ]; then
        echo "Error: Failed to upload chunk $BLOCK_INDEX"
        rm -f "$COOKIE_FILE"
        exit 1
    fi

    OFFSET=$((OFFSET + CURRENT_CHUNK_SIZE))
    BLOCK_INDEX=$((BLOCK_INDEX + 1))
done

echo "✓ Uploaded $BLOCK_INDEX chunks"
echo ""

# Step 3: Finalize upload
echo "Step 3: Finalizing upload..."

# Build block IDs JSON array
BLOCK_IDS_JSON=$(printf ',"%s"' "${BLOCK_IDS[@]}")
BLOCK_IDS_JSON="[${BLOCK_IDS_JSON:1}]"

FINALIZE_RESPONSE=$(curl -s -b "$COOKIE_FILE" -X POST "$API_URL/files/upload/finalize" \
    -H "Content-Type: application/json" \
    -d "{\"guid\": \"$GUID\", \"fileName\": \"$FILE_NAME\", \"blockIds\": $BLOCK_IDS_JSON}")

echo "Response: $FINALIZE_RESPONSE"
echo ""

# Check if successful
if echo "$FINALIZE_RESPONSE" | grep -q '"success":true'; then
    echo "================================================"
    echo "✓ Upload completed successfully!"
    echo "GUID: $GUID"
    echo "================================================"
    echo ""
    echo "File uploaded to blob storage with GUID: $GUID"
    echo ""
    echo "Note: Download requires authentication."
    echo "You can test download from your browser or frontend."

    # Cleanup
    rm -f "$COOKIE_FILE"
else
    echo "================================================"
    echo "✗ Upload failed"
    echo "================================================"
    rm -f "$COOKIE_FILE"
    exit 1
fi
