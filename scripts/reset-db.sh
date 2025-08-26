#!/bin/bash

# Reset Databases - Drop and Recreate with Seed Data
# WARNING: This will destroy all data in the databases!
# Stops containers, removes volumes, and restarts with fresh databases
#
# Usage:
#   ./reset-db.sh            # Interactive mode (prompts for confirmation)
#   ./reset-db.sh -force     # Skip confirmation prompt
#   ./reset-db.sh -keepRedis # Reset databases but preserve Redis cache
#
# Example:
#   ./scripts/reset-db.sh

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
FORCE=false
KEEP_REDIS=false

while [[ "$#" -gt 0 ]]; do
    case $1 in
        -force|--force) FORCE=true ;;
        -keepRedis|--keep-redis) KEEP_REDIS=true ;;
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

# Confirmation prompt
if [ "$FORCE" = false ]; then
    echo -e "\n${RED}⚠️  WARNING: This will destroy all data in the databases!${NC}"
    echo -e "${YELLOW}This action cannot be undone.${NC}"
    
    read -p "Are you sure you want to reset the databases? Type 'yes' to confirm: " confirmation
    if [ "$confirmation" != "yes" ]; then
        echo -e "${YELLOW}Operation cancelled.${NC}"
        exit 0
    fi
fi

# Change to project root directory
cd "$PROJECT_ROOT"

echo -e "\n${YELLOW}Stopping all containers...${NC}"
if docker-compose down; then
    echo -e "${GREEN}✓ Containers stopped${NC}"
fi

# Remove volumes
if [ "$KEEP_REDIS" = true ]; then
    echo -e "\n${YELLOW}Removing database volumes (keeping Redis)...${NC}"
    docker volume rm claude-code-03_postgres_platform_data 2>/dev/null || true
    docker volume rm claude-code-03_postgres_auth_data 2>/dev/null || true
else
    echo -e "\n${YELLOW}Removing all volumes...${NC}"
    docker-compose down -v
fi

if [ $? -eq 0 ]; then
    echo -e "${GREEN}✓ Volumes removed${NC}"
fi

echo -e "\n${YELLOW}Starting fresh database containers...${NC}"
if docker-compose up -d postgres-platform postgres-auth redis; then
    echo -e "${GREEN}✓ Database containers started${NC}"
else
    echo -e "${RED}✗ Failed to start database containers${NC}"
    exit 1
fi

# Wait for databases to be ready
echo -e "\n${YELLOW}Waiting for databases to initialize...${NC}"
MAX_ATTEMPTS=30
ATTEMPT=0
PLATFORM_READY=false
AUTH_READY=false

while [ $ATTEMPT -lt $MAX_ATTEMPTS ] && { [ "$PLATFORM_READY" = false ] || [ "$AUTH_READY" = false ]; }; do
    ATTEMPT=$((ATTEMPT + 1))
    echo -e "  ${GRAY}Checking databases... (attempt $ATTEMPT/$MAX_ATTEMPTS)${NC}"
    
    # Check platform database
    if [ "$PLATFORM_READY" = false ]; then
        if docker exec postgres-platform pg_isready -U platform_user -d platform_db &>/dev/null; then
            PLATFORM_READY=true
            echo -e "    ${GREEN}✓ Platform database ready${NC}"
        fi
    fi
    
    # Check auth database
    if [ "$AUTH_READY" = false ]; then
        if docker exec postgres-auth pg_isready -U auth_user -d auth_db &>/dev/null; then
            AUTH_READY=true
            echo -e "    ${GREEN}✓ Auth database ready${NC}"
        fi
    fi
    
    if [ "$PLATFORM_READY" = false ] || [ "$AUTH_READY" = false ]; then
        sleep 2
    fi
done

if [ "$PLATFORM_READY" = true ] && [ "$AUTH_READY" = true ]; then
    echo -e "\n${GREEN}✓ Databases initialized and ready!${NC}"
    
    # Run migrations if BFF is available
    echo -e "\n${YELLOW}Attempting to run Entity Framework migrations...${NC}"
    BFF_PATH="$PROJECT_ROOT/platform-host/platform-host-bff"
    if [ -d "$BFF_PATH" ]; then
        cd "$BFF_PATH"
        if dotnet ef database update &>/dev/null; then
            echo -e "${GREEN}✓ Migrations applied successfully${NC}"
        else
            echo -e "${YELLOW}⚠ Could not apply migrations automatically. Run manually when BFF starts.${NC}"
        fi
        cd "$PROJECT_ROOT"
    fi
    
    echo -e "\n${GREEN}✅ Database reset complete!${NC}"
    echo -e "\n${CYAN}Databases have been reset with seed data:${NC}"
    echo -e "  ${WHITE}Platform DB: Contains platform tenant and test data${NC}"
    echo -e "  ${WHITE}Auth DB: Contains Duende IdentityServer schema and OAuth clients${NC}"
    
    echo -e "\n${CYAN}Default test credentials:${NC}"
    echo -e "  ${GRAY}Platform Admin: admin@platform.com / Admin123!${NC}"
    echo -e "  ${GRAY}Test User: user@test.com / Test123!${NC}"
    
    echo -e "\n${CYAN}You can now:${NC}"
    echo -e "  ${GRAY}- Start application services: ./scripts/start-all.sh${NC}"
    echo -e "  ${GRAY}- Create additional users: ./scripts/create-user.sh${NC}"
    echo -e "  ${GRAY}- Check service health: ./scripts/health-check.sh${NC}"
else
    echo -e "${RED}✗ Databases failed to initialize within timeout period${NC}"
    echo -e "${YELLOW}Check docker logs for more information:${NC}"
    echo -e "  ${GRAY}docker logs postgres-platform${NC}"
    echo -e "  ${GRAY}docker logs postgres-auth${NC}"
    exit 1
fi