# 🚀 Development Setup Guide

## Quick Start

### 1. **One-Click Development** (Recommended)
```bash
npm run dev:setup
```
Then in VS Code: `Ctrl+Shift+P` → **Tasks: Run Task** → **Development Setup**

### 2. **Manual Setup**
```bash
# Terminal 1: Start infrastructure
npm run services

# Terminal 2: Start backend
npm run backend

# Terminal 3: Start frontend
npm run frontend
```

## 📝 **Available Scripts**

### Infrastructure
- `npm run services` - Start DB, Storage, Qdrant
- `npm run services:down` - Stop infrastructure
- `npm run services:logs` - View infrastructure logs

### Backend (.NET)
- `npm run backend` - Run with hot reload
- `npm run backend:build` - Build only
- `npm run backend:test` - Run tests

### Frontend (Next.js)
- `npm run frontend` - Run with hot reload
- `npm run frontend:build` - Build for production
- `npm run frontend:test` - Run tests
- `npm run frontend:lint` - Run linter

### Full Project
- `npm run test` - Run all tests
- `npm run build` - Build everything
- `npm run lint` - Lint everything
- `npm run reset` - Clean & restart infrastructure

### Production
- `npm run prod` - Full production deployment
- `npm run prod:down` - Stop production
- `npm run clean` - Clean all volumes and containers

## 🐛 **Debugging**

### VS Code Debugging
1. Start infrastructure: `npm run services`
2. Press `F5` or use "Launch Backend" configuration
3. Set breakpoints in your C# code

### Manual Debugging
- Backend: `http://localhost:8080/status`
- Frontend: `http://localhost:3000`
- Database: `localhost:5432` (postgres/postgres)
- Qdrant: `http://localhost:6333`
- Storage: `http://localhost:10000`

## 🔧 **Environment**
- Single `.env.local` file for both local development and Docker production testing
- Azure production will use Azure environment variables

## 🎯 **VS Code Tasks**

Press `Ctrl+Shift+P` → **Tasks: Run Task** to access:

### Development Tasks
- **Development Setup** - Start infrastructure + backend + frontend
- **Start Infrastructure** - Start DB, Storage, Qdrant only
- **Start Backend** - Run .NET with hot reload
- **Start Frontend** - Run Next.js with hot reload
- **Stop All Services** - Stop infrastructure

### Production Tasks
- **Production Setup** - Clean restart production environment
- **Start Production** - Deploy full production stack
- **Stop Production** - Stop production containers
- **Clean & Reset Production** - Remove all volumes and restart

### Other Tasks
- **Build Backend** / **Test Backend**
- **Test Frontend**

## 🎯 **VS Code Features**
- IntelliSense for C# and TypeScript
- Integrated debugging
- One-click task execution
- Docker support
- Tailwind CSS support