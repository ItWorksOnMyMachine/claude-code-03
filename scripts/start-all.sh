#!/bin/bash

# Start All Services - Platform Stack
# Starts all services including databases, cache, auth service, BFF, and frontend
# Since application services are not containerized, this script:
# 1. Starts infrastructure services in Docker (PostgreSQL, Redis)
# 2. Starts application services locally (BFF, Auth Service, Frontend)
#
# Usage:
#   ./start-all.sh           # Start infrastructure in foreground (manual app start required)
#   ./start-all.sh -d        # Start all services in detached/background mode
#   ./start-all.sh -build    # Rebuild Docker images before starting
#
# Example:
#   ./scripts/start-all.sh -d

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
WHITE='\033[1;37m'
GRAY='\033[0;90m'
NC='\033[0m' # No Color

# Parse arguments
DETACHED=false
BUILD=false
FORCE=false

while [[ "$#" -gt 0 ]]; do
    case $1 in
        -d|--detach) DETACHED=true ;;
        -build|--build) BUILD=true ;;
        -force|--force) FORCE=true ;;
        *) echo "Unknown parameter: $1"; exit 1 ;;
    esac
    shift
done

# Check if Docker is running
echo -e "${YELLOW}Checking Docker status...${NC}"
if docker version &>/dev/null; then
    echo -e "${GREEN}✓ Docker is running${NC}"
else
    echo -e "${RED}✗ Docker is not running. Please start Docker and try again.${NC}"
    exit 1
fi

# Get script directory and project root
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"

# Check if docker-compose.yml exists
COMPOSE_FILE="$PROJECT_ROOT/docker-compose.yml"
if [ ! -f "$COMPOSE_FILE" ]; then
    echo -e "${RED}✗ docker-compose.yml not found at $COMPOSE_FILE${NC}"
    exit 1
fi
echo -e "${GREEN}✓ Found docker-compose.yml${NC}"

# Build command arguments
ARGS="up"
if [ "$DETACHED" = true ]; then
    ARGS="$ARGS -d"
    echo -e "${CYAN}Starting services in detached mode...${NC}"
else
    echo -e "${CYAN}Starting services in foreground mode (Ctrl+C to stop)...${NC}"
fi

if [ "$BUILD" = true ]; then
    ARGS="$ARGS --build"
    echo -e "${CYAN}Rebuilding images...${NC}"
fi

if [ "$FORCE" = true ]; then
    ARGS="$ARGS --force-recreate"
    echo -e "${CYAN}Force recreating containers...${NC}"
fi

# Change to project root directory
cd "$PROJECT_ROOT"

echo -e "\n${YELLOW}Starting infrastructure services (Docker)...${NC}"
echo -e "${GRAY}Services: postgres-platform, postgres-auth, redis${NC}\n"

# Add service names to arguments for docker-compose
ARGS="$ARGS postgres-platform postgres-auth redis"

# Run docker-compose for infrastructure
if docker-compose $ARGS; then
    echo -e "\n${GREEN}✓ Infrastructure services started successfully!${NC}"
    
    if [ "$DETACHED" = true ]; then
        # Start application services in background
        echo -e "\n${YELLOW}Starting application services locally...${NC}"
        
        # Start Auth Service
        echo -e "${CYAN}Starting Auth Service...${NC}"
        cd "$PROJECT_ROOT/auth-service/AuthService"
        nohup dotnet run > "$PROJECT_ROOT/auth-service.log" 2>&1 &
        AUTH_PID=$!
        echo "Auth Service PID: $AUTH_PID"
        
        # Start Platform BFF
        echo -e "${CYAN}Starting Platform BFF...${NC}"
        cd "$PROJECT_ROOT/platform-host/platform-host-bff"
        nohup dotnet run > "$PROJECT_ROOT/platform-bff.log" 2>&1 &
        BFF_PID=$!
        echo "Platform BFF PID: $BFF_PID"
        
        # Start Frontend
        echo -e "${CYAN}Starting Frontend...${NC}"
        cd "$PROJECT_ROOT/platform-host/platform-host-frontend"
        nohup npm run dev > "$PROJECT_ROOT/frontend.log" 2>&1 &
        FRONTEND_PID=$!
        echo "Frontend PID: $FRONTEND_PID"
        
        # Save PIDs to file for later cleanup
        echo "$AUTH_PID" > "$PROJECT_ROOT/.app-pids"
        echo "$BFF_PID" >> "$PROJECT_ROOT/.app-pids"
        echo "$FRONTEND_PID" >> "$PROJECT_ROOT/.app-pids"
        
        echo -e "\n${YELLOW}Waiting for services to initialize...${NC}"
        sleep 5
        
        echo -e "\n${GREEN}✓ All services starting!${NC}"
        echo -e "\n${CYAN}Service URLs:${NC}"
        echo -e "  ${WHITE}Frontend:        http://localhost:3002${NC}"
        echo -e "  ${WHITE}Platform BFF:    http://localhost:5000${NC}"
        echo -e "  ${WHITE}Auth Service:    http://localhost:5001${NC}"
        echo -e "  ${WHITE}PostgreSQL Platform: localhost:5432${NC}"
        echo -e "  ${WHITE}PostgreSQL Auth:     localhost:5433${NC}"
        echo -e "  ${WHITE}Redis:           localhost:6379${NC}"
        
        echo -e "\n${YELLOW}Note: Application services are running in background${NC}"
        echo -e "${GRAY}Application logs are in: auth-service.log, platform-bff.log, frontend.log${NC}"
        echo -e "${GRAY}To stop all services, run: ./scripts/stop-all.sh${NC}"
        echo -e "${GRAY}To stop infrastructure only: docker-compose down${NC}"
        echo -e "\n${GRAY}Run './scripts/health-check.sh' to verify all services are healthy${NC}"
    else
        echo -e "\n${YELLOW}Infrastructure services are running in foreground mode.${NC}"
        echo -e "${GRAY}To start application services, run this script with -d flag or manually start:${NC}"
        echo -e "  ${GRAY}Auth Service: cd auth-service/AuthService && dotnet run${NC}"
        echo -e "  ${GRAY}BFF: cd platform-host/platform-host-bff && dotnet run${NC}"
        echo -e "  ${GRAY}Frontend: cd platform-host/platform-host-frontend && npm run dev${NC}"
    fi
else
    echo -e "${RED}✗ Failed to start infrastructure services. Check the logs above for errors.${NC}"
    exit 1
fi