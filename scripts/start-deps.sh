#!/bin/bash

# Start Dependencies Only - PostgreSQL and Redis
# Starts only the dependency services without the application services
# Useful for local development when running BFF and Auth services from IDE
#
# Usage:
#   ./start-deps.sh          # Start dependencies in foreground
#   ./start-deps.sh -d       # Start dependencies in detached mode
#   ./start-deps.sh -fresh   # Remove volumes and start fresh
#
# Example:
#   ./scripts/start-deps.sh -d

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
FRESH=false

while [[ "$#" -gt 0 ]]; do
    case $1 in
        -d|--detach) DETACHED=true ;;
        -fresh|--fresh) FRESH=true ;;
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

# Change to project root directory
cd "$PROJECT_ROOT"

# If fresh start requested, remove volumes
if [ "$FRESH" = true ]; then
    echo -e "\n${YELLOW}Removing existing volumes for fresh start...${NC}"
    if docker-compose down -v; then
        echo -e "${GREEN}✓ Volumes removed${NC}"
    fi
fi

# Build command arguments
ARGS="up"
if [ "$DETACHED" = true ]; then
    ARGS="$ARGS -d"
    echo -e "\n${CYAN}Starting dependencies in detached mode...${NC}"
else
    echo -e "\n${CYAN}Starting dependencies in foreground mode (Ctrl+C to stop)...${NC}"
fi

# Only start dependency services
ARGS="$ARGS postgres-platform postgres-auth redis"

echo -e "${GRAY}Services: postgres-platform, postgres-auth, redis${NC}\n"

# Run docker-compose
if docker-compose $ARGS; then
    echo -e "\n${GREEN}✓ Dependencies started successfully!${NC}"
    
    if [ "$DETACHED" = true ]; then
        echo -e "\n${CYAN}Dependency Service Ports:${NC}"
        echo -e "  ${WHITE}PostgreSQL Platform DB: localhost:5432${NC}"
        echo -e "    ${GRAY}Database: platform_db${NC}"
        echo -e "    ${GRAY}Username: platform_user${NC}"
        echo -e "    ${GRAY}Password: platform_pass${NC}"
        
        echo -e "\n  ${WHITE}PostgreSQL Auth DB: localhost:5433${NC}"
        echo -e "    ${GRAY}Database: auth_db${NC}"
        echo -e "    ${GRAY}Username: auth_user${NC}"
        echo -e "    ${GRAY}Password: auth_pass${NC}"
        
        echo -e "\n  ${WHITE}Redis: localhost:6379${NC}"
        echo -e "    ${GRAY}No authentication required for local dev${NC}"
        
        echo -e "\n${CYAN}Application services can now be started locally:${NC}"
        echo -e "  ${GRAY}BFF: cd platform-host/platform-host-bff && dotnet run${NC}"
        echo -e "  ${GRAY}Auth: cd auth-service/AuthService && dotnet run${NC}"
        echo -e "  ${GRAY}Frontend: cd platform-host/platform-host-frontend && npm run dev${NC}"
        
        echo -e "\n${GRAY}Run 'docker-compose ps' to see running containers${NC}"
        echo -e "${GRAY}Run 'docker-compose down' to stop dependencies${NC}"
    fi
else
    echo -e "${RED}✗ Failed to start dependencies. Check the logs above for errors.${NC}"
    exit 1
fi