#!/bin/bash

# Stop All Services - Platform Stack
# Stops all running services including local application services and Docker infrastructure
#
# Usage:
#   ./stop-all.sh           # Stop all services
#   ./stop-all.sh -force    # Force stop all services and remove volumes
#
# Example:
#   ./scripts/stop-all.sh

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
GRAY='\033[0;90m'
NC='\033[0m' # No Color

# Parse arguments
FORCE=false

while [[ "$#" -gt 0 ]]; do
    case $1 in
        -force|--force) FORCE=true ;;
        *) echo "Unknown parameter: $1"; exit 1 ;;
    esac
    shift
done

echo -e "${YELLOW}Stopping all platform services...${NC}"

# Get script directory and project root
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"

# Stop application services if PIDs file exists
echo -e "\n${CYAN}Stopping application services...${NC}"

if [ -f "$PROJECT_ROOT/.app-pids" ]; then
    while IFS= read -r pid; do
        if kill -0 "$pid" 2>/dev/null; then
            echo -e "  ${GRAY}Stopping process $pid...${NC}"
            kill "$pid" 2>/dev/null || true
        fi
    done < "$PROJECT_ROOT/.app-pids"
    rm "$PROJECT_ROOT/.app-pids"
    echo -e "${GREEN}✓ Application services stopped${NC}"
else
    # Try to find and stop services by name
    echo -e "  ${GRAY}Looking for running application services...${NC}"
    
    # Stop dotnet processes
    pkill -f "dotnet.*platform-host-bff" 2>/dev/null || true
    pkill -f "dotnet.*AuthService" 2>/dev/null || true
    
    # Stop Node.js frontend
    pkill -f "node.*platform-host-frontend" 2>/dev/null || true
    
    echo -e "${GREEN}✓ Application services stopped${NC}"
fi

# Clean up log files
if [ -f "$PROJECT_ROOT/auth-service.log" ]; then
    rm "$PROJECT_ROOT/auth-service.log"
fi
if [ -f "$PROJECT_ROOT/platform-bff.log" ]; then
    rm "$PROJECT_ROOT/platform-bff.log"
fi
if [ -f "$PROJECT_ROOT/frontend.log" ]; then
    rm "$PROJECT_ROOT/frontend.log"
fi

# Stop Docker infrastructure services
echo -e "\n${CYAN}Stopping Docker infrastructure services...${NC}"

# Change to project root directory
cd "$PROJECT_ROOT"

# Check if Docker is running
if docker version &>/dev/null; then
    # Stop docker-compose services
    if [ "$FORCE" = true ]; then
        docker-compose down -v
        echo -e "${GREEN}✓ Docker services stopped and volumes removed${NC}"
    else
        docker-compose down
        echo -e "${GREEN}✓ Docker services stopped${NC}"
    fi
else
    echo -e "${YELLOW}⚠ Docker is not running or services already stopped${NC}"
fi

echo -e "\n${GREEN}✓ All services stopped successfully!${NC}"