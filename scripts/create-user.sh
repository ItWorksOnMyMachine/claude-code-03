#!/bin/bash

# Create Test User - Creates a new user via the development API
# Creates a test user in the auth service and assigns them to a tenant
#
# Parameters:
#   -email     : User's email address (required)
#   -password  : User's password (optional, defaults to Test123!)
#   -tenant    : Tenant ID or name to assign user to (optional)
#   -role      : Role to assign (Admin/User, defaults to User)
#
# Usage:
#   ./create-user.sh -email user@example.com
#   ./create-user.sh -email admin@company.com -password SecurePass123! -role Admin
#   ./create-user.sh -email user@test.com -tenant "test-tenant" -role User
#
# Example:
#   ./scripts/create-user.sh -email developer@test.com

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
WHITE='\033[1;37m'
GRAY='\033[0;90m'
NC='\033[0m' # No Color

# Default values
EMAIL=""
PASSWORD="Test123!"
TENANT=""
ROLE="User"

# Parse arguments
while [[ "$#" -gt 0 ]]; do
    case $1 in
        -email|--email) EMAIL="$2"; shift ;;
        -password|--password) PASSWORD="$2"; shift ;;
        -tenant|--tenant) TENANT="$2"; shift ;;
        -role|--role) ROLE="$2"; shift ;;
        *) echo "Unknown parameter: $1"; exit 1 ;;
    esac
    shift
done

# Check required parameters
if [ -z "$EMAIL" ]; then
    echo -e "${RED}✗ Email is required${NC}"
    echo "Usage: $0 -email user@example.com [-password Pass123!] [-tenant tenant-name] [-role Admin|User]"
    exit 1
fi

# Validate email format
if ! echo "$EMAIL" | grep -qE '^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$'; then
    echo -e "${RED}✗ Invalid email format: $EMAIL${NC}"
    echo -e "${YELLOW}Please provide a valid email address (e.g., user@example.com)${NC}"
    exit 1
fi

# Validate password complexity
if [ ${#PASSWORD} -lt 8 ]; then
    echo -e "${RED}✗ Password must be at least 8 characters long${NC}"
    exit 1
fi

if ! echo "$PASSWORD" | grep -q '[A-Z]' || ! echo "$PASSWORD" | grep -q '[a-z]' || \
   ! echo "$PASSWORD" | grep -q '[0-9]' || ! echo "$PASSWORD" | grep -qE '[^a-zA-Z0-9]'; then
    echo -e "${RED}✗ Password must contain uppercase, lowercase, number, and special character${NC}"
    echo -e "${YELLOW}Example: Test123!${NC}"
    exit 1
fi

# Validate role
if [ "$ROLE" != "Admin" ] && [ "$ROLE" != "User" ]; then
    echo -e "${RED}✗ Role must be either 'Admin' or 'User'${NC}"
    exit 1
fi

echo -e "${YELLOW}Creating user account...${NC}"
echo -e "  ${GRAY}Email: $EMAIL${NC}"
echo -e "  ${GRAY}Role: $ROLE${NC}"
if [ -n "$TENANT" ]; then
    echo -e "  ${GRAY}Tenant: $TENANT${NC}"
fi

# Check if BFF service is running
BFF_URL="http://localhost:5000"
HEALTH_URL="$BFF_URL/health"

echo -e "\n${YELLOW}Checking BFF service availability...${NC}"
if curl -s -f -o /dev/null -w "%{http_code}" --max-time 2 "$HEALTH_URL" > /dev/null 2>&1; then
    echo -e "${GREEN}✓ BFF service is running${NC}"
else
    echo -e "${RED}✗ BFF service is not running at $BFF_URL${NC}"
    echo -e "${YELLOW}Please start the BFF service first:${NC}"
    echo -e "  ${GRAY}./scripts/start-all.sh${NC}"
    echo -e "  ${GRAY}OR${NC}"
    echo -e "  ${GRAY}cd platform-host/platform-host-bff && dotnet run${NC}"
    exit 1
fi

# Create JSON body
CREATE_USER_URL="$BFF_URL/api/dev/users/create"
JSON_BODY="{\"email\":\"$EMAIL\",\"password\":\"$PASSWORD\",\"role\":\"$ROLE\""
if [ -n "$TENANT" ]; then
    JSON_BODY="$JSON_BODY,\"tenantId\":\"$TENANT\""
fi
JSON_BODY="$JSON_BODY}"

echo -e "\n${YELLOW}Sending request to create user...${NC}"

# Send request
RESPONSE=$(curl -s -w "\n%{http_code}" -X POST "$CREATE_USER_URL" \
    -H "Content-Type: application/json" \
    -d "$JSON_BODY" 2>/dev/null)

HTTP_CODE=$(echo "$RESPONSE" | tail -n1)
RESPONSE_BODY=$(echo "$RESPONSE" | head -n-1)

if [ "$HTTP_CODE" = "200" ] || [ "$HTTP_CODE" = "201" ]; then
    echo -e "\n${GREEN}✅ User created successfully!${NC}"
    
    # Parse response if jq is available
    if command -v jq &> /dev/null; then
        USER_ID=$(echo "$RESPONSE_BODY" | jq -r '.userId // empty')
        USER_EMAIL=$(echo "$RESPONSE_BODY" | jq -r '.email // empty')
        USER_ROLE=$(echo "$RESPONSE_BODY" | jq -r '.role // empty')
        TENANT_NAME=$(echo "$RESPONSE_BODY" | jq -r '.tenantName // empty')
        
        echo -e "\n${CYAN}User Details:${NC}"
        [ -n "$USER_EMAIL" ] && echo -e "  ${WHITE}Email: $USER_EMAIL${NC}"
        [ -n "$USER_ID" ] && echo -e "  ${WHITE}User ID: $USER_ID${NC}"
        [ -n "$USER_ROLE" ] && echo -e "  ${WHITE}Role: $USER_ROLE${NC}"
        [ -n "$TENANT_NAME" ] && echo -e "  ${WHITE}Tenant: $TENANT_NAME${NC}"
    else
        echo "$RESPONSE_BODY"
    fi
    
    echo -e "\n${CYAN}Login credentials:${NC}"
    echo -e "  ${GRAY}Username: $EMAIL${NC}"
    echo -e "  ${GRAY}Password: $PASSWORD${NC}"
    
    echo -e "\n${CYAN}You can now:${NC}"
    echo -e "  ${GRAY}- Login at: http://localhost:3002/login${NC}"
    echo -e "  ${GRAY}- Use the API with these credentials${NC}"
    echo -e "  ${GRAY}- Assign to additional tenants if needed${NC}"
else
    echo -e "${RED}✗ Failed to create user${NC}"
    
    if [ -n "$RESPONSE_BODY" ]; then
        echo -e "${RED}Error: $RESPONSE_BODY${NC}"
        
        # Check for duplicate user
        if echo "$RESPONSE_BODY" | grep -qi "duplicate\|exists\|conflict"; then
            echo -e "\n${YELLOW}User with email '$EMAIL' may already exist${NC}"
            echo -e "${GRAY}Try a different email address or reset the database:${NC}"
            echo -e "  ${GRAY}./scripts/reset-db.sh${NC}"
        fi
    else
        echo -e "${RED}HTTP Status: $HTTP_CODE${NC}"
    fi
    
    exit 1
fi