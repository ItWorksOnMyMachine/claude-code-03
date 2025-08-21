# Authentication Service Deployment Guide

## Overview

This guide provides comprehensive instructions for deploying the Authentication Service to production environments. The service is designed to run as a standalone identity provider at `auth.platform.com`.

## Prerequisites

- Docker 20.10+ and Docker Compose 2.0+
- PostgreSQL 17+ database
- Redis 7+ for session caching
- SSL certificate for HTTPS
- Domain configured (auth.platform.com)
- Kubernetes cluster (optional, for K8s deployment)

## Environment Configuration

### Required Environment Variables

```bash
# Database Configuration
DB_HOST=postgres.platform.com
DB_PORT=5432
DB_NAME=authservice_prod
DB_USER=authservice
DB_PASSWORD=<secure_password>

# Redis Configuration
REDIS_CONNECTION=redis.platform.com:6379,password=<redis_password>,ssl=true

# Certificate Configuration
CERT_PASSWORD=<certificate_password>

# Email Service (SendGrid)
SENDGRID_API_KEY=<sendgrid_api_key>

# Monitoring (OpenTelemetry)
OTEL_EXPORTER_OTLP_ENDPOINT=https://otel-collector.platform.com:4317

# Application Secrets
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=https://+:443;http://+:80
```

## Deployment Methods

### 1. Docker Deployment

#### Build the Docker Image

```bash
# Navigate to auth-service directory
cd auth-service

# Build the production image
docker build -t platform/authservice:latest -f Dockerfile .

# Tag for registry
docker tag platform/authservice:latest registry.platform.com/authservice:latest

# Push to registry
docker push registry.platform.com/authservice:latest
```

#### Run with Docker Compose

Create `docker-compose.production.yml`:

```yaml
version: '3.8'

services:
  authservice:
    image: registry.platform.com/authservice:latest
    container_name: authservice
    ports:
      - "443:443"
      - "80:80"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ASPNETCORE_URLS=https://+:443;http://+:80
      - DB_HOST=${DB_HOST}
      - DB_PORT=${DB_PORT}
      - DB_NAME=${DB_NAME}
      - DB_USER=${DB_USER}
      - DB_PASSWORD=${DB_PASSWORD}
      - REDIS_CONNECTION=${REDIS_CONNECTION}
      - CERT_PASSWORD=${CERT_PASSWORD}
      - SENDGRID_API_KEY=${SENDGRID_API_KEY}
      - OTEL_EXPORTER_OTLP_ENDPOINT=${OTEL_EXPORTER_OTLP_ENDPOINT}
    volumes:
      - ./certs:/app/certs:ro
      - ./logs:/var/log/authservice
    networks:
      - platform-network
    restart: unless-stopped
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost/health"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 40s

networks:
  platform-network:
    external: true
```

Deploy:

```bash
# Load environment variables
export $(cat .env.production | xargs)

# Deploy
docker-compose -f docker-compose.production.yml up -d

# Check logs
docker logs -f authservice
```

### 2. Kubernetes Deployment

#### Create Kubernetes Resources

Create `k8s/deployment.yaml`:

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: authservice
  namespace: platform
spec:
  replicas: 3
  selector:
    matchLabels:
      app: authservice
  template:
    metadata:
      labels:
        app: authservice
    spec:
      containers:
      - name: authservice
        image: registry.platform.com/authservice:latest
        ports:
        - containerPort: 443
          name: https
        - containerPort: 80
          name: http
        env:
        - name: ASPNETCORE_ENVIRONMENT
          value: "Production"
        - name: DB_HOST
          valueFrom:
            secretKeyRef:
              name: authservice-secrets
              key: db-host
        - name: DB_PASSWORD
          valueFrom:
            secretKeyRef:
              name: authservice-secrets
              key: db-password
        # ... other environment variables
        volumeMounts:
        - name: certificates
          mountPath: /app/certs
          readOnly: true
        - name: logs
          mountPath: /var/log/authservice
        livenessProbe:
          httpGet:
            path: /health
            port: 80
          initialDelaySeconds: 30
          periodSeconds: 30
        readinessProbe:
          httpGet:
            path: /health/ready
            port: 80
          initialDelaySeconds: 10
          periodSeconds: 10
        resources:
          requests:
            memory: "256Mi"
            cpu: "250m"
          limits:
            memory: "512Mi"
            cpu: "500m"
      volumes:
      - name: certificates
        secret:
          secretName: authservice-certificates
      - name: logs
        persistentVolumeClaim:
          claimName: authservice-logs-pvc
---
apiVersion: v1
kind: Service
metadata:
  name: authservice
  namespace: platform
spec:
  selector:
    app: authservice
  ports:
  - port: 443
    targetPort: 443
    name: https
  - port: 80
    targetPort: 80
    name: http
  type: LoadBalancer
---
apiVersion: networking.k8s.io/v1
kind: Ingress
metadata:
  name: authservice-ingress
  namespace: platform
  annotations:
    cert-manager.io/cluster-issuer: letsencrypt-prod
    nginx.ingress.kubernetes.io/ssl-redirect: "true"
spec:
  tls:
  - hosts:
    - auth.platform.com
    secretName: authservice-tls
  rules:
  - host: auth.platform.com
    http:
      paths:
      - path: /
        pathType: Prefix
        backend:
          service:
            name: authservice
            port:
              number: 443
```

Deploy to Kubernetes:

```bash
# Create namespace
kubectl create namespace platform

# Create secrets
kubectl create secret generic authservice-secrets \
  --from-literal=db-host=$DB_HOST \
  --from-literal=db-password=$DB_PASSWORD \
  --from-literal=redis-connection=$REDIS_CONNECTION \
  -n platform

# Create certificate secret
kubectl create secret generic authservice-certificates \
  --from-file=signing-certificate.pfx=./certs/signing-certificate.pfx \
  -n platform

# Apply deployment
kubectl apply -f k8s/deployment.yaml

# Check status
kubectl get pods -n platform
kubectl logs -f deployment/authservice -n platform
```

## Database Migration

### Initial Setup

```bash
# Connect to production database
psql -h $DB_HOST -U $DB_USER -d postgres

# Create database
CREATE DATABASE authservice_prod;

# Run migrations using Entity Framework
dotnet ef database update --project AuthService --connection "Host=$DB_HOST;Port=$DB_PORT;Database=$DB_NAME;Username=$DB_USER;Password=$DB_PASSWORD"
```

### Migration Strategy

1. **Blue-Green Deployment**: Run migrations before switching traffic
2. **Rolling Update**: Ensure backward compatibility in migrations
3. **Backup**: Always backup database before migrations

```bash
# Backup database
pg_dump -h $DB_HOST -U $DB_USER authservice_prod > backup_$(date +%Y%m%d_%H%M%S).sql

# Run migration
dotnet ef database update

# Verify migration
psql -h $DB_HOST -U $DB_USER -d authservice_prod -c "SELECT * FROM __EFMigrationsHistory;"
```

## SSL Certificate Setup

### Generate Production Certificate

```powershell
# Using provided PowerShell script
.\scripts\generate-certificate.ps1 `
  -SubjectName "CN=auth.platform.com" `
  -OutputPath "./certs" `
  -Password "StrongPassword123!" `
  -ValidityDays 365
```

### Using Let's Encrypt (Recommended)

```bash
# Install certbot
apt-get update && apt-get install certbot

# Generate certificate
certbot certonly --standalone -d auth.platform.com --email admin@platform.com --agree-tos

# Convert to PFX
openssl pkcs12 -export -out signing-certificate.pfx \
  -inkey /etc/letsencrypt/live/auth.platform.com/privkey.pem \
  -in /etc/letsencrypt/live/auth.platform.com/fullchain.pem
```

## Health Checks and Monitoring

### Health Check Endpoints

- `/health` - Basic health check
- `/health/ready` - Detailed readiness check with dependencies
- `/metrics` - Prometheus metrics endpoint

### Monitoring Setup

1. **Configure Prometheus scraping**:

```yaml
scrape_configs:
  - job_name: 'authservice'
    static_configs:
      - targets: ['auth.platform.com:443']
    scheme: https
    metrics_path: /metrics
```

2. **Configure alerts**:

```yaml
groups:
- name: authservice
  rules:
  - alert: AuthServiceDown
    expr: up{job="authservice"} == 0
    for: 5m
    annotations:
      summary: "Authentication Service is down"
  
  - alert: HighErrorRate
    expr: rate(http_requests_total{job="authservice",status=~"5.."}[5m]) > 0.05
    for: 5m
    annotations:
      summary: "High error rate detected"
  
  - alert: DatabaseConnectionFailure
    expr: authservice_database_connections_failed > 0
    for: 1m
    annotations:
      summary: "Database connection failures detected"
```

## Security Hardening

### 1. Network Security

```bash
# Configure firewall rules
ufw allow 443/tcp
ufw allow 80/tcp
ufw deny 5432/tcp  # Block direct database access
```

### 2. Container Security

```dockerfile
# Run as non-root user
USER app:app

# Security scanning
docker scan platform/authservice:latest
```

### 3. Secrets Management

Use Azure Key Vault or AWS Secrets Manager:

```bash
# Azure Key Vault
az keyvault secret set --vault-name platform-vault --name db-password --value $DB_PASSWORD

# AWS Secrets Manager
aws secretsmanager create-secret --name authservice/db-password --secret-string $DB_PASSWORD
```

## Backup and Recovery

### Database Backup

```bash
# Automated backup script
#!/bin/bash
BACKUP_DIR="/backups/postgres"
TIMESTAMP=$(date +%Y%m%d_%H%M%S)
BACKUP_FILE="$BACKUP_DIR/authservice_$TIMESTAMP.sql.gz"

pg_dump -h $DB_HOST -U $DB_USER authservice_prod | gzip > $BACKUP_FILE

# Keep only last 30 days
find $BACKUP_DIR -name "*.sql.gz" -mtime +30 -delete
```

### Session Cache Backup

```bash
# Redis backup
redis-cli -h redis.platform.com BGSAVE
```

## Performance Tuning

### 1. Database Optimization

```sql
-- Create indexes for frequently queried columns
CREATE INDEX idx_users_email ON "AspNetUsers" (email);
CREATE INDEX idx_audit_logs_timestamp ON "AuthenticationAuditLogs" (timestamp);
CREATE INDEX idx_sessions_user_id ON "PersistedGrants" (subject_id);

-- Analyze tables
ANALYZE "AspNetUsers";
ANALYZE "AuthenticationAuditLogs";
```

### 2. Application Settings

```json
{
  "Kestrel": {
    "Limits": {
      "MaxConcurrentConnections": 10000,
      "MaxConcurrentUpgradedConnections": 10000,
      "MaxRequestBodySize": 10485760,
      "Http2": {
        "MaxStreamsPerConnection": 100
      }
    }
  }
}
```

### 3. Redis Configuration

```conf
# redis.conf
maxmemory 2gb
maxmemory-policy allkeys-lru
timeout 300
tcp-keepalive 60
```

## Rollback Procedures

### Application Rollback

```bash
# Kubernetes rollback
kubectl rollout undo deployment/authservice -n platform

# Docker rollback
docker-compose -f docker-compose.production.yml down
docker pull registry.platform.com/authservice:previous-version
docker-compose -f docker-compose.production.yml up -d
```

### Database Rollback

```bash
# Restore from backup
psql -h $DB_HOST -U $DB_USER -d postgres -c "DROP DATABASE authservice_prod;"
psql -h $DB_HOST -U $DB_USER -d postgres -c "CREATE DATABASE authservice_prod;"
gunzip < backup_20240115_120000.sql.gz | psql -h $DB_HOST -U $DB_USER authservice_prod
```

## Troubleshooting

### Common Issues

1. **Certificate errors**:
```bash
# Check certificate validity
openssl x509 -in /app/certs/signing-certificate.pfx -text -noout

# Test HTTPS endpoint
curl -v https://auth.platform.com/.well-known/openid-configuration
```

2. **Database connection issues**:
```bash
# Test database connectivity
psql -h $DB_HOST -U $DB_USER -d authservice_prod -c "SELECT 1;"

# Check connection pool
SELECT count(*) FROM pg_stat_activity WHERE datname = 'authservice_prod';
```

3. **Redis connection issues**:
```bash
# Test Redis connectivity
redis-cli -h redis.platform.com ping
```

### Log Analysis

```bash
# View application logs
docker logs authservice --tail 100 -f

# Search for errors
grep ERROR /var/log/authservice/auth-*.log

# Analyze with jq
cat /var/log/authservice/auth-*.log | jq '. | select(.Level == "Error")'
```

## Maintenance Windows

### Planned Maintenance Checklist

- [ ] Notify users 24 hours in advance
- [ ] Backup database and Redis
- [ ] Prepare rollback plan
- [ ] Test deployment in staging
- [ ] Monitor health checks post-deployment
- [ ] Verify all integrations working
- [ ] Update documentation

## Support and Contact

- **On-call Engineer**: Use PagerDuty rotation
- **Escalation**: Platform Team Lead
- **Documentation**: https://docs.platform.com/auth-service
- **Monitoring Dashboard**: https://grafana.platform.com/d/authservice