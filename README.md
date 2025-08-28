# Platform Host - Multi-Tenant SaaS Platform

A modern multi-tenant SaaS platform built with React Module Federation frontend and .NET 9 BFF (Backend for Frontend) architecture.

## Quick Start

### Prerequisites

- Docker Desktop (Windows/Mac/Linux)
- Node.js 18+ and npm 9+
- .NET 9 SDK
- PowerShell 7+ (Windows) or Bash (Linux/Mac)

### 1. Clone and Setup

```bash
git clone <repository-url>
cd platform-host-dev-setup

# Copy environment template
cp .env.example .env
```

### 2. Start Services

#### Option A: Dependencies Only (Recommended for Development)

```powershell
# Start just the databases and Redis
.\scripts\start-deps.ps1 -d

# Then in separate terminals:
# Terminal 1 - Backend BFF
cd platform-host\platform-host-bff
dotnet run

# Terminal 2 - Frontend
cd platform-host\platform-host-frontend
npm install  # First time only
npm run dev
```

#### Option B: All Docker Containers

```powershell
# Start all containers (databases, Redis only for now)
.\scripts\start-all.ps1 -d
```

**Note:** The application services (BFF, Auth, Frontend) are currently commented out in docker-compose.yml and need to be run locally.

### 3. Access the Platform

Open your browser to: **http://localhost:3002**

**Test Credentials:**
- Email: `admin@platform.local`
- Password: `Admin123!`

## Development Commands

### Managing Services

```powershell
# Start all services
.\scripts\start-all.ps1 -d

# Start only dependencies (for local development)
.\scripts\start-deps.ps1 -d

# Check health status
.\scripts\health-check.ps1

# View logs
.\scripts\logs.ps1 -follow
.\scripts\logs.ps1 -service platform-bff

# Stop everything
docker-compose down
```

### Database Management

```powershell
# Reset databases (WARNING: Deletes all data!)
.\scripts\reset-db.ps1

# Create a test user
.\scripts\create-user.ps1 -email "test@example.com" -password "Test123!" -tenant "default"
```

### Local Development

For hot-reload development:

```powershell
# 1. Start dependencies only
.\scripts\start-deps.ps1 -d

# 2. Run backend services locally
cd platform-host\platform-host-bff
dotnet run

# 3. Run frontend with hot-reload
cd platform-host\platform-host-frontend
npm run dev
```

## Project Structure

```
platform-host-dev-setup/
├── docker-compose.yml          # Container orchestration
├── .env.example                 # Environment template
├── scripts/                     # Developer utilities
│   ├── start-all.ps1/sh        # Start full stack
│   ├── start-deps.ps1/sh       # Start dependencies only
│   ├── reset-db.ps1/sh         # Reset databases
│   ├── health-check.ps1/sh     # Check service health
│   └── logs.ps1/sh             # View container logs
├── docker/
│   └── sql/                    # Database initialization
│       ├── platform/           # Platform DB scripts
│       └── auth/              # Auth DB scripts
├── platform-host/
│   ├── platform-host-frontend/ # React Module Federation
│   └── platform-host-bff/      # .NET 9 BFF API
└── test/                       # Test suites
```

## Available Services

| Service | Port | Description |
|---------|------|-------------|
| Frontend | 3002 | React Module Federation host |
| Platform BFF | 5000 | .NET 9 Backend for Frontend |
| Auth Service | 5001 | Duende IdentityServer |
| PostgreSQL Platform | 5432 | Platform database |
| PostgreSQL Auth | 5433 | Authentication database |
| Redis | 6379 | Session cache |

## Documentation

- [Developer Setup Guide](./documentation/DEVELOPER_SETUP.md) - Detailed setup instructions
- [Architecture Overview](./documentation/ARCHITECTURE.md) - System design and patterns
- [Development Workflows](./documentation/WORKFLOWS.md) - Common development tasks
- [Troubleshooting Guide](./documentation/TROUBLESHOOTING.md) - Solutions to common issues

## Testing

```bash
# Run all tests
npm test

# Run specific test suites
npm test docker-compose
npm test database-init
npm test scripts

# Backend tests
cd platform-host/platform-host-bff
dotnet test

# Frontend tests
cd platform-host/platform-host-frontend
npm test
```

## Common Issues

### Docker not running
```
Error: Docker is not running
Solution: Start Docker Desktop
```

### Port already in use
```
Error: Port 5000 is already allocated
Solution: Stop conflicting service or change port in .env
```

### Database connection failed
```
Solution: Run .\scripts\health-check.ps1 to diagnose
         Then .\scripts\reset-db.ps1 if needed
```

See [TROUBLESHOOTING.md](./TROUBLESHOOTING.md) for more solutions.

## Contributing

1. Create a feature branch
2. Make your changes
3. Run tests: `npm test`
4. Submit a pull request

## License

[License Type] - See LICENSE file for details