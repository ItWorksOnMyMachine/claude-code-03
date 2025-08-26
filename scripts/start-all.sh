#!/bin/bash

# Start All Services - Platform Stack
# Starts all services including databases, cache, auth service, BFF, and frontend
#
# Usage:
#   ./start-all.sh           # Start all services in foreground
#   ./start-all.sh -d        # Start all services in detached mode
#   ./start-all.sh -build    # Rebuild images before starting
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

echo -e "\n${YELLOW}Starting all services...${NC}"
echo -e "${GRAY}Services: postgres-platform, postgres-auth, redis, platform-bff, auth-service${NC}\n"

# Run docker-compose
if docker-compose $ARGS; then
    echo -e "\n${GREEN}✓ All services started successfully!${NC}"
    
    if [ "$DETACHED" = true ]; then
        echo -e "\n${CYAN}Service URLs:${NC}"
        echo -e "  ${WHITE}Platform BFF:    http://localhost:5000${NC}"
        echo -e "  ${WHITE}Auth Service:    http://localhost:5001${NC}"
        echo -e "  ${WHITE}Frontend:        http://localhost:3002${NC}"
        echo -e "  ${WHITE}PostgreSQL Platform: localhost:5432${NC}"
        echo -e "  ${WHITE}PostgreSQL Auth:     localhost:5433${NC}"
        echo -e "  ${WHITE}Redis:           localhost:6379${NC}"
        echo -e "\n${GRAY}Run './scripts/health-check.sh' to verify all services are healthy${NC}"
        echo -e "${GRAY}Run './scripts/logs.sh' to view logs${NC}"
        echo -e "${GRAY}Run 'docker-compose down' to stop all services${NC}"
    fi
else
    echo -e "${RED}✗ Failed to start services. Check the logs above for errors.${NC}"
    exit 1
fi