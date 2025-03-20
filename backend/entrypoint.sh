#!/bin/bash

# Remove Migrations folder if it exists
if [ -d "Migrations" ]; then
    echo "Removing existing Migrations folder..."
    rm -rf Migrations
fi

# Create a fresh initial migration
echo "Creating new initial migration..."
dotnet ef migrations add InitialCreate

# Start the application with hot reload
echo "Starting application with hot reload..."
dotnet watch --project backend.csproj run --urls http://+:8080