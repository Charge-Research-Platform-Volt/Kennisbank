#!/bin/bash

# Check if we're running a custom command (like tests in CI)
if [ $# -gt 0 ]; then
    # Execute the passed command instead of the default behavior
    exec "$@"
else
    # Normal development behavior
    # Remove Migrations folder if it exists
    if [ -d "Migrations" ]; then
        echo "Removing existing Migrations folder..."
        rm -rf Migrations
    fi

    # Create a fresh initial migration
    echo "Creating new initial migration..."
    if [ -f "backend.csproj" ]; then
        dotnet ef migrations add InitialCreate
    else
        echo "Error: backend.csproj not found"
        exit 1
    fi

    # Start the application with hot reload
    echo "Starting application with hot reload..."
    dotnet watch --project backend.csproj run --urls http://+:8080
fi