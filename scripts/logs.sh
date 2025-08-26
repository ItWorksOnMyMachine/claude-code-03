#!/bin/bash

# View Logs - Tail logs from Docker containers
# Aggregates logs from all or specific containers with filtering options
#
# Parameters:
#   -service  : Specific service to show logs for (optional)
#   -tail     : Number of recent lines to show (default: 100)
#   -follow   : Follow log output in real-time
#   -since    : Show logs since timestamp (e.g., "2m" for last 2 minutes)
#
# Usage:
#   ./logs.sh                           # Show last 100 lines from all services
#   ./logs.sh -follow                   # Follow all logs in real-time
#   ./logs.sh -service platform-bff     # Show logs for specific service
#   ./logs.sh -tail 500 -since 5m       # Show last 500 lines from last 5 minutes
#   ./logs.sh -service redis -follow    # Follow Redis logs only
#
# Example:
#   ./scripts/logs.sh -service platform-bff -follow

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
WHITE='\033[1;37m'
GRAY='\033[0;90m'
DARK_GRAY='\033[1;30m'
NC='\033[0m' # No Color

# Default values
SERVICE=""
TAIL=100
FOLLOW=false
SINCE=""

# Parse arguments
while [[ "$#" -gt 0 ]]; do
    case $1 in
        -service|--service) SERVICE="$2"; shift ;;
        -tail|--tail) TAIL="$2"; shift ;;
        -follow|--follow|-f) FOLLOW=true ;;
        -since|--since) SINCE="$2"; shift ;;
        *) echo "Unknown parameter: $1"; exit 1 ;;
    esac
    shift
done

# Check if Docker is running
if ! docker version &>/dev/null; then
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

# Change to project root directory
cd "$PROJECT_ROOT"

# Build docker-compose command
ARGS="logs"

# Add tail parameter
if [ "$TAIL" -gt 0 ]; then
    ARGS="$ARGS --tail $TAIL"
fi

# Add follow parameter
if [ "$FOLLOW" = true ]; then
    ARGS="$ARGS -f"
fi

# Add since parameter
if [ -n "$SINCE" ]; then
    ARGS="$ARGS --since $SINCE"
fi

# Add service filter
if [ -n "$SERVICE" ]; then
    # Validate service exists
    VALID_SERVICES=("postgres-platform" "postgres-auth" "redis" "platform-bff" "auth-service")
    
    if [[ ! " ${VALID_SERVICES[@]} " =~ " ${SERVICE} " ]]; then
        echo -e "${RED}✗ Invalid service name: $SERVICE${NC}"
        echo -e "${YELLOW}Valid services:${NC}"
        for valid_service in "${VALID_SERVICES[@]}"; do
            echo -e "  ${GRAY}- $valid_service${NC}"
        done
        exit 1
    fi
    
    ARGS="$ARGS $SERVICE"
    
    if [ "$FOLLOW" = true ]; then
        echo -e "${CYAN}Following logs for $SERVICE (Ctrl+C to stop)...${NC}"
    else
        echo -e "${CYAN}Showing last $TAIL lines for $SERVICE...${NC}"
    fi
else
    if [ "$FOLLOW" = true ]; then
        echo -e "${CYAN}Following logs for all services (Ctrl+C to stop)...${NC}"
        echo -e "${GRAY}Services: postgres-platform, postgres-auth, redis, platform-bff, auth-service${NC}"
    else
        echo -e "${CYAN}Showing last $TAIL lines from all services...${NC}"
        echo -e "${GRAY}Services: postgres-platform, postgres-auth, redis, platform-bff, auth-service${NC}"
    fi
fi

if [ -n "$SINCE" ]; then
    echo -e "${GRAY}Filtering logs since: $SINCE${NC}"
fi

echo "" # Empty line before logs
echo -e "${DARK_GRAY}$(printf '─%.0s' {1..80})${NC}"

# Execute docker-compose logs
docker-compose $ARGS

if [ "$FOLLOW" = false ]; then
    echo -e "${DARK_GRAY}$(printf '─%.0s' {1..80})${NC}"
    echo -e "\n${CYAN}📝 Log Tips:${NC}"
    echo -e "  ${GRAY}• Use -follow to see real-time logs${NC}"
    echo -e "  ${GRAY}• Use -service [name] to filter by service${NC}"
    echo -e "  ${GRAY}• Use -tail [number] to see more/fewer lines${NC}"
    echo -e "  ${GRAY}• Use -since [time] for time-based filtering (e.g., '10m', '1h')${NC}"
    echo -e "\n${YELLOW}Examples:${NC}"
    echo -e "  ${GRAY}./scripts/logs.sh -follow${NC}"
    echo -e "  ${GRAY}./scripts/logs.sh -service postgres-platform -tail 200${NC}"
    echo -e "  ${GRAY}./scripts/logs.sh -since 5m -follow${NC}"
fi