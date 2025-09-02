# Developer Setup Guide

Complete guide for setting up the Platform Host development environment.

## Table of Contents

1. [System Requirements](#system-requirements)
2. [Installation Steps](#installation-steps)
3. [Environment Configuration](#environment-configuration)
4. [Database Setup](#database-setup)
5. [Running the Platform](#running-the-platform)
6. [IDE Configuration](#ide-configuration)
7. [Verification Steps](#verification-steps)

## System Requirements

### Required Software

| Software | Minimum Version | Recommended Version | Notes |
|----------|----------------|---------------------|-------|
| Docker Desktop | 20.10+ | Latest | Required for containers |
| Node.js | 18.0.0 | 20.x LTS | Frontend development |
| npm | 9.0.0 | 10.x | Package management |
| .NET SDK | 9.0.100 | 9.0.x | Backend development |
| PowerShell | 7.0 | 7.4+ | Windows scripting |
| Git | 2.30 | Latest | Version control |

### Hardware Requirements

- **RAM**: Minimum 8GB, Recommended 16GB
- **Storage**: 10GB free space for Docker images
- **CPU**: 4 cores recommended

### Operating System

- Windows 10/11 (with WSL2 for Docker)
- macOS 11+ (Big Sur or later)
- Linux (Ubuntu 20.04+, Fedora 35+)

## Installation Steps

### 1. Install Docker Desktop

#### Windows
```powershell
# Download from Docker website
# https://www.docker.com/products/docker-desktop

# Verify installation
docker --version
docker-compose --version
```

#### macOS
```bash
# Using Homebrew
brew install --cask docker

# Or download from Docker website
```

#### Linux
```bash
# Ubuntu/Debian
sudo apt-get update
sudo apt-get install docker-ce docker-ce-cli containerd.io docker-compose-plugin

# Add user to docker group
sudo usermod -aG docker $USER
```

### 2. Install Node.js and npm

#### Using Node Version Manager (Recommended)

Windows (PowerShell):
```powershell
# Install nvm-windows
# Download from: https://github.com/coreybutler/nvm-windows

nvm install 20.11.0
nvm use 20.11.0
```

macOS/Linux:
```bash
# Install nvm
curl -o- https://raw.githubusercontent.com/nvm-sh/nvm/v0.39.0/install.sh | bash

# Install Node.js
nvm install 20.11.0
nvm use 20.11.0
```

### 3. Install .NET 9 SDK

```powershell
# Windows - Download installer
# https://dotnet.microsoft.com/download/dotnet/9.0

# macOS
brew install --cask dotnet-sdk

# Linux (Ubuntu)
sudo apt-get update
sudo apt-get install -y dotnet-sdk-9.0

# Verify installation
dotnet --version
```

### 4. Clone the Repository

```bash
git clone <repository-url>
cd platform-host-dev-setup

# Verify structure
ls -la
```

## Environment Configuration

### 1. Create Environment File

```bash
# Copy the template
cp .env.example .env

# Edit with your preferred editor
# Windows
notepad .env

# macOS/Linux
nano .env
```

### 2. Environment Variables

```env
# Database Configuration
POSTGRES_PLATFORM_HOST=localhost
POSTGRES_PLATFORM_PORT=5432
POSTGRES_PLATFORM_DB=platform_db
POSTGRES_PLATFORM_USER=platform_user
POSTGRES_PLATFORM_PASSWORD=platform_pass

POSTGRES_AUTH_HOST=localhost
POSTGRES_AUTH_PORT=5433
POSTGRES_AUTH_DB=auth_db
POSTGRES_AUTH_USER=auth_user
POSTGRES_AUTH_PASSWORD=auth_pass

# Redis Configuration
REDIS_HOST=localhost
REDIS_PORT=6379
REDIS_PASSWORD=DevRedisPass123!

# Application Ports
BFF_PORT=5000
AUTH_SERVICE_PORT=5001
FRONTEND_PORT=3002

# Development Settings
ENVIRONMENT=Development
DEBUG=true
```

### 3. SSL Certificates (Optional)

For HTTPS in development:

```powershell
# Generate dev certificate (.NET)
dotnet dev-certs https --trust

# For frontend (if needed)
cd platform-host/platform-host-frontend
npm run generate-cert
```

## Database Setup

### Initial Setup

```powershell
# Start database containers
.\scripts\start-deps.ps1 -d

# Wait for databases to be ready (automatic with health checks)
.\scripts\health-check.ps1
```

### Database Schema

The databases are automatically initialized with:

**Platform Database** (port 5432):
- Schemas: public, tenant, audit
- Tables: tenants, users, roles, permissions
- Seed data: 3 test tenants, 5 test users

**Auth Database** (port 5433):
- Duende IdentityServer tables
- OAuth clients configuration
- API resources and scopes

### Accessing Databases

```bash
# Platform database
psql -h localhost -p 5432 -U platform_user -d platform_db

# Auth database
psql -h localhost -p 5433 -U auth_user -d auth_db

# Using Docker exec
docker exec -it postgres-platform psql -U platform_user -d platform_db
```

## Running the Platform

### Full Stack (Recommended for First Run)

```powershell
# Start everything
.\scripts\start-all.ps1 -d

# Check status
.\scripts\health-check.ps1

# View logs
.\scripts\logs.ps1 -follow
```

### Development Mode (Hot Reload)

```powershell
# 1. Start only dependencies
.\scripts\start-deps.ps1 -d

# 2. Terminal 1 - Run BFF
cd platform-host\platform-host-bff
dotnet watch run

# 3. Terminal 2 - Run Auth Service
cd auth-service\AuthService
dotnet watch run

# 4. Terminal 3 - Run Frontend
cd platform-host\platform-host-frontend
npm run dev
```

### Service URLs

- Frontend: https://host-fe.platform.local:3002
- BFF API: https://host-bff.platform.local:5086
- Auth Service: https://login.platform.local:5214
- API Documentation: http://host-bff.platform.local:5086/swagger

## IDE Configuration

### Visual Studio Code

Recommended extensions:
```json
{
  "recommendations": [
    "ms-dotnettools.csharp",
    "ms-dotnettools.vscode-dotnet-runtime",
    "ms-azuretools.vscode-docker",
    "dbaeumer.vscode-eslint",
    "esbenp.prettier-vscode",
    "formulahendry.dotnet-test-explorer"
  ]
}
```

Settings (.vscode/settings.json):
```json
{
  "editor.formatOnSave": true,
  "editor.codeActionsOnSave": {
    "source.fixAll.eslint": true
  },
  "typescript.preferences.importModuleSpecifier": "relative",
  "dotnet-test-explorer.testProjectPath": "**/*Tests.csproj"
}
```

### Visual Studio 2022

1. Open `PlatformHost.sln`
2. Set multiple startup projects:
   - platform-host-bff
   - AuthService
3. Configure Docker support
4. Set environment to Development

### JetBrains Rider

1. Open solution file
2. Configure Run Configurations
3. Set environment variables
4. Enable Docker support

## Verification Steps

### 1. Check Docker Services

```powershell
# List running containers
docker-compose ps

# Expected output:
# NAME                    STATUS      PORTS
# postgres-platform       healthy     0.0.0.0:5432->5432/tcp
# postgres-auth          healthy     0.0.0.0:5433->5432/tcp
# redis                  healthy     0.0.0.0:6379->6379/tcp
```

### 2. Test Database Connections

```powershell
# Run health check
.\scripts\health-check.ps1 -v

# Expected: All services showing "Healthy"
```

### 3. Test API Endpoints

```powershell
# Health endpoint
curl https://login.platform.local:5086/health

# Auth endpoint
curl https://login.platform.local:5086/.well-known/openid-configuration
```

### 4. Test Frontend

1. Navigate to https://host-fe.platform.local:3002
2. Should see login page
3. Login with test credentials:
   - Email: `admin@platform.local`
   - Password: `Admin123!`

### 5. Run Tests

```bash
# All tests
npm test

# Backend tests
cd platform-host/platform-host-bff
dotnet test

# Frontend tests
cd platform-host/platform-host-frontend
npm test
```

## Troubleshooting Setup Issues

### Docker Issues

```powershell
# Reset Docker
docker system prune -a --volumes

# Restart Docker Desktop
# Windows: Restart from system tray
# macOS: Restart from menu bar
```

### Port Conflicts

```powershell
# Check port usage
netstat -an | findstr :5000

# Change ports in .env file if needed
```

### Database Connection Issues

```powershell
# Reset databases
.\scripts\reset-db.ps1 -force

# Check logs
docker logs postgres-platform
docker logs postgres-auth
```

### Permission Issues (Linux/Mac)

```bash
# Fix script permissions
chmod +x scripts/*.sh

# Fix Docker permissions
sudo usermod -aG docker $USER
newgrp docker
```

## Next Steps

- Review [ARCHITECTURE.md](./ARCHITECTURE.md) for system design
- Check [WORKFLOWS.md](./WORKFLOWS.md) for development workflows
- See [TROUBLESHOOTING.md](./TROUBLESHOOTING.md) for common issues

## Support

For setup assistance:
1. Check the troubleshooting guide
2. Review Docker and service logs
3. Contact the development team