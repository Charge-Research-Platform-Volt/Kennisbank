#!/bin/bash

echo "Starting the development environment..."

# Start the backend
echo "Starting backend..."
osascript -e 'tell app "Terminal" to do script "cd $(pwd)/backend && dotnet run"'

# Start the frontend
echo "Starting frontend..."
osascript -e 'tell app "Terminal" to do script "cd $(pwd)/frontend && npm install && npm run dev"'

# Start the website
echo "Starting website..."
osascript -e 'tell app "Terminal" to do script "cd $(pwd)/website && npm install && npm run dev"'

echo "All services are starting up..."
echo "Backend: http://localhost:5000 (or your configured port)"
echo "Frontend: http://localhost:5173 (default Vite port)"