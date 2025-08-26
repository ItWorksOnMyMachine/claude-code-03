#!/bin/bash

# Health Check - Verify all services are running and healthy
# Checks the health status of all platform services and dependencies
#
# Usage:
#   ./health-check.sh        # Check all services
#   ./health-check.sh -v     # Verbose output with response times
#   ./health-check.sh -json  # Output results as JSON
#
# Example:
#   ./scripts/health-check.sh

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
VERBOSE=false
JSON_OUTPUT=false

while [[ "$#" -gt 0 ]]; do
    case $1 in
        -v|--verbose) VERBOSE=true ;;
        -json|--json) JSON_OUTPUT=true ;;
        *) echo "Unknown parameter: $1"; exit 1 ;;
    esac
    shift
done

ALL_HEALTHY=true
declare -a SERVICES_JSON=()

# Function to check service health
check_service() {
    local name=$1
    local type=$2
    local url=$3
    local port=$4
    local container=$5
    
    local status="Unknown"
    local response_time=""
    local error=""
    local start_time=$(date +%s%N)
    
    case $type in
        "HTTP")
            if response=$(curl -s -o /dev/null -w "%{http_code}" --max-time 3 "$url" 2>/dev/null); then
                local end_time=$(date +%s%N)
                response_time=$((($end_time - $start_time) / 1000000))
                if [ "$response" = "200" ]; then
                    status="Healthy"
                else
                    status="Unhealthy"
                    error="Status code: $response"
                fi
            else
                status="Error"
                error="Connection failed"
            fi
            ;;
        "Docker")
            if [ -n "$container" ]; then
                if docker ps --filter "name=$container" --format "{{.Status}}" 2>/dev/null | grep -q "Up"; then
                    status="Running"
                else
                    status="Stopped"
                    error="Container not running"
                fi
            fi
            ;;
        "TCP")
            if timeout 3 bash -c "echo > /dev/tcp/localhost/$port" 2>/dev/null; then
                local end_time=$(date +%s%N)
                response_time=$((($end_time - $start_time) / 1000000))
                status="Available"
            else
                status="Unavailable"
                error="Cannot connect to port $port"
            fi
            ;;
    esac
    
    if [ "$status" != "Healthy" ] && [ "$status" != "Available" ] && [ "$status" != "Running" ]; then
        ALL_HEALTHY=false
    fi
    
    # Store result for JSON output
    local json_obj="{\"name\":\"$name\",\"type\":\"$type\",\"port\":$port,\"status\":\"$status\""
    [ -n "$response_time" ] && json_obj="$json_obj,\"responseTime\":$response_time"
    [ -n "$error" ] && json_obj="$json_obj,\"error\":\"$error\""
    json_obj="$json_obj}"
    SERVICES_JSON+=("$json_obj")
    
    # Display result
    if [ "$JSON_OUTPUT" = false ]; then
        local icon="❌"
        local color=$RED
        
        if [ "$status" = "Healthy" ] || [ "$status" = "Available" ] || [ "$status" = "Running" ]; then
            icon="✅"
            color=$GREEN
        fi
        
        local status_text="$icon $name (port $port): $status"
        if [ "$VERBOSE" = true ] && [ -n "$response_time" ]; then
            status_text="$status_text [${response_time}ms]"
        fi
        
        echo -e "${color}${status_text}${NC}"
        
        if [ -n "$error" ] && [ "$VERBOSE" = true ]; then
            echo -e "    ${YELLOW}Error: $error${NC}"
        fi
    fi
}

if [ "$JSON_OUTPUT" = false ]; then
    echo -e "\n${CYAN}🔍 Platform Health Check${NC}"
    echo -e "${CYAN}========================${NC}"
    echo -e "${YELLOW}Checking all services...${NC}"
fi

# Check Docker
if [ "$JSON_OUTPUT" = false ]; then
    echo -e "\n${GRAY}Checking Docker...${NC}"
fi

DOCKER_STATUS="Not Running"
if docker version &>/dev/null; then
    DOCKER_STATUS="Running"
else
    ALL_HEALTHY=false
fi

if [ "$JSON_OUTPUT" = false ]; then
    if [ "$DOCKER_STATUS" = "Running" ]; then
        echo -e "${GREEN}✅ Docker: $DOCKER_STATUS${NC}"
    else
        echo -e "${RED}❌ Docker: $DOCKER_STATUS${NC}"
    fi
    
    echo -e "\n${CYAN}🔌 Dependencies:${NC}"
fi

# Check Database Services
check_service "PostgreSQL Platform" "TCP" "" 5432 "postgres-platform"
check_service "PostgreSQL Auth" "TCP" "" 5433 "postgres-auth"
check_service "Redis Cache" "TCP" "" 6379 "redis"

if [ "$JSON_OUTPUT" = false ]; then
    echo -e "\n${CYAN}🚀 Applications:${NC}"
fi

# Check Application Services
check_service "Platform BFF" "HTTP" "http://localhost:5000/health" 5000 ""
check_service "Auth Service" "HTTP" "http://localhost:5001/health" 5001 ""
check_service "Frontend" "HTTP" "http://localhost:3002" 3002 ""

# Output results
if [ "$JSON_OUTPUT" = true ]; then
    # Build JSON output
    echo -n "{\"timestamp\":\"$(date -u +"%Y-%m-%dT%H:%M:%SZ")\","
    echo -n "\"docker\":\"$DOCKER_STATUS\","
    echo -n "\"services\":["
    
    # Join services array
    first=true
    for service in "${SERVICES_JSON[@]}"; do
        if [ "$first" = true ]; then
            first=false
        else
            echo -n ","
        fi
        echo -n "$service"
    done
    
    echo -n "],"
    if [ "$ALL_HEALTHY" = true ]; then
        echo -n "\"overall_health\":\"Healthy\""
    else
        echo -n "\"overall_health\":\"Unhealthy\""
    fi
    echo "}"
else
    # Console output summary
    echo -e "\n${CYAN}📈 Overall Status:${NC}"
    if [ "$ALL_HEALTHY" = true ]; then
        echo -e "${GREEN}✅ All services are healthy!${NC}"
        echo -e "\n${CYAN}Platform is ready at:${NC}"
        echo -e "  ${WHITE}Frontend: http://localhost:3002${NC}"
        echo -e "  ${WHITE}BFF API: http://localhost:5000${NC}"
        echo -e "  ${WHITE}Auth: http://localhost:5001${NC}"
    else
        echo -e "${YELLOW}⚠️ Some services are not healthy${NC}"
        echo -e "\n${CYAN}💡 Troubleshooting:${NC}"
        echo -e "  ${GRAY}1. Start all services: ./scripts/start-all.sh${NC}"
        echo -e "  ${GRAY}2. Check logs: ./scripts/logs.sh${NC}"
        echo -e "  ${GRAY}3. Reset if needed: ./scripts/reset-db.sh${NC}"
    fi
fi

# Exit with appropriate code
if [ "$ALL_HEALTHY" = true ]; then
    exit 0
else
    exit 1
fi