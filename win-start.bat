@echo off
echo Starting the development environment...

:: Start the backend
echo Starting backend...
start cmd /k "cd backend && dotnet run"

:: Start the frontend
echo Starting frontend...
start cmd /k "cd frontend && npm install && npm run dev"

:: Start the website
echo Starting website...
start cmd /k "cd website && npm install && npm run dev"