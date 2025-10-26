# MeldenIT Agent - Deployment Dokümantasyonu

## Genel Bakış

Bu dokümantasyon, MeldenIT Agent sisteminin farklı ortamlarda nasıl deploy edileceğini açıklar.

## Gereksinimler

### Sistem Gereksinimleri

#### Backend Sunucu
- **OS**: Ubuntu 20.04+ / CentOS 8+ / Windows Server 2019+
- **CPU**: 2+ cores
- **RAM**: 4+ GB
- **Disk**: 50+ GB SSD
- **Network**: 100+ Mbps

#### Database
- **PostgreSQL**: 14+
- **Redis**: 6+ (opsiyonel)

#### Agent (Windows)
- **OS**: Windows 10+ / Windows Server 2016+
- **.NET**: 8.0 Runtime
- **RAM**: 1+ GB
- **Disk**: 1+ GB

### Yazılım Gereksinimleri

#### Backend
- **Python**: 3.11+
- **Docker**: 20.10+
- **Docker Compose**: 2.0+

#### Agent
- **.NET 8 Runtime**
- **Windows Service** (otomatik kurulum)

## Deployment Seçenekleri

### 1. Docker Compose (Önerilen)

#### Gereksinimler
```bash
# Docker ve Docker Compose kurulumu
curl -fsSL https://get.docker.com -o get-docker.sh
sh get-docker.sh

# Docker Compose kurulumu
sudo curl -L "https://github.com/docker/compose/releases/download/v2.20.0/docker-compose-$(uname -s)-$(uname -m)" -o /usr/local/bin/docker-compose
sudo chmod +x /usr/local/bin/docker-compose
```

#### Kurulum
```bash
# Repository'yi klonla
git clone https://github.com/meldenit/meldenit-agent.git
cd meldenit-agent

# Environment dosyasını oluştur
cp env.example .env
nano .env  # Gerekli ayarları yap

# Servisleri başlat
./scripts/deploy.sh deploy
```

#### Konfigürasyon
```bash
# .env dosyası örneği
POSTGRES_PASSWORD=secure_password_here
SECRET_KEY=your-secret-key-here
SNIPEIT_API_TOKEN=your-snipeit-token
DOMAIN_NAME=assit.meldencloud.com
SSL_EMAIL=admin@meldencloud.com
```

### 2. Manual Installation

#### Backend Kurulumu

##### 1. Python Environment
```bash
# Python 3.11 kurulumu
sudo apt update
sudo apt install python3.11 python3.11-venv python3.11-dev

# Virtual environment oluştur
python3.11 -m venv venv
source venv/bin/activate

# Dependencies kurulumu
cd backend
pip install -r requirements.txt
```

##### 2. Database Kurulumu
```bash
# PostgreSQL kurulumu
sudo apt install postgresql postgresql-contrib

# Database oluştur
sudo -u postgres psql
CREATE DATABASE meldenit;
CREATE USER meldenit WITH PASSWORD 'secure_password';
GRANT ALL PRIVILEGES ON DATABASE meldenit TO meldenit;
\q
```

##### 3. Redis Kurulumu (Opsiyonel)
```bash
# Redis kurulumu
sudo apt install redis-server
sudo systemctl enable redis-server
sudo systemctl start redis-server
```

##### 4. Application Başlatma
```bash
# Environment variables
export DATABASE_URL="postgresql://meldenit:secure_password@localhost/meldenit"
export SECRET_KEY="your-secret-key-here"
export SNIPEIT_API_TOKEN="your-snipeit-token"

# Database migration
python scripts/init_db.py

# Application başlat
uvicorn app.main:app --host 0.0.0.0 --port 8000
```

#### Nginx Kurulumu
```bash
# Nginx kurulumu
sudo apt install nginx

# Konfigürasyon
sudo nano /etc/nginx/sites-available/meldenit
```

```nginx
server {
    listen 80;
    server_name assit.meldencloud.com;
    
    location / {
        proxy_pass http://localhost:8000;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
```

```bash
# Site'ı aktifleştir
sudo ln -s /etc/nginx/sites-available/meldenit /etc/nginx/sites-enabled/
sudo nginx -t
sudo systemctl reload nginx
```

#### SSL Sertifikası
```bash
# Certbot kurulumu
sudo apt install certbot python3-certbot-nginx

# SSL sertifikası al
sudo certbot --nginx -d assit.meldencloud.com
```

### 3. Kubernetes Deployment

#### Namespace Oluştur
```yaml
apiVersion: v1
kind: Namespace
metadata:
  name: meldenit
```

#### ConfigMap
```yaml
apiVersion: v1
kind: ConfigMap
metadata:
  name: meldenit-config
  namespace: meldenit
data:
  DATABASE_URL: "postgresql://meldenit:password@postgres:5432/meldenit"
  SECRET_KEY: "your-secret-key"
  SNIPEIT_API_TOKEN: "your-snipeit-token"
```

#### PostgreSQL Deployment
```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: postgres
  namespace: meldenit
spec:
  replicas: 1
  selector:
    matchLabels:
      app: postgres
  template:
    metadata:
      labels:
        app: postgres
    spec:
      containers:
      - name: postgres
        image: postgres:15
        env:
        - name: POSTGRES_DB
          value: meldenit
        - name: POSTGRES_USER
          value: meldenit
        - name: POSTGRES_PASSWORD
          value: secure_password
        ports:
        - containerPort: 5432
        volumeMounts:
        - name: postgres-storage
          mountPath: /var/lib/postgresql/data
      volumes:
      - name: postgres-storage
        persistentVolumeClaim:
          claimName: postgres-pvc
---
apiVersion: v1
kind: Service
metadata:
  name: postgres
  namespace: meldenit
spec:
  selector:
    app: postgres
  ports:
  - port: 5432
    targetPort: 5432
```

#### Backend Deployment
```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: meldenit-backend
  namespace: meldenit
spec:
  replicas: 3
  selector:
    matchLabels:
      app: meldenit-backend
  template:
    metadata:
      labels:
        app: meldenit-backend
    spec:
      containers:
      - name: backend
        image: meldenit/backend:latest
        ports:
        - containerPort: 8000
        envFrom:
        - configMapRef:
            name: meldenit-config
---
apiVersion: v1
kind: Service
metadata:
  name: meldenit-backend
  namespace: meldenit
spec:
  selector:
    app: meldenit-backend
  ports:
  - port: 80
    targetPort: 8000
  type: LoadBalancer
```

## Agent Deployment

### 1. MSI Installation

#### Sessiz Kurulum
```powershell
# PowerShell ile sessiz kurulum
msiexec /i MeldenITAgent.msi /qn API_URL="https://assit.meldencloud.com" SNIPEIT_URL="https://assit.meldencloud.com" SITE_CODE="HQ"
```

#### Group Policy ile Kurulum
```xml
<!-- GPO XML örneği -->
<Computer>
  <Software>
    <Package>
      <Name>MeldenIT Agent</Name>
      <Version>1.0.0</Version>
      <Source>\\domain\software\MeldenITAgent.msi</Source>
      <Arguments>/qn API_URL="https://assit.meldencloud.com" SITE_CODE="HQ"</Arguments>
    </Package>
  </Software>
</Computer>
```

### 2. PowerShell Script ile Kurulum

```powershell
# Kurulum scripti
.\deployment\scripts\install.ps1 -ApiUrl "https://assit.meldencloud.com" -SiteCode "HQ" -Force
```

### 3. SCCM ile Kurulum

#### Application Oluştur
```xml
<Application>
  <Name>MeldenIT Agent</Name>
  <Version>1.0.0</Version>
  <DeploymentType>
    <Name>MSI Installation</Name>
    <Technology>MSI</Technology>
    <Content>
      <ContentLocation>\\sccm\packages\MeldenITAgent</ContentLocation>
    </Content>
    <InstallCommand>msiexec /i MeldenITAgent.msi /qn</InstallCommand>
    <UninstallCommand>msiexec /x {GUID} /qn</UninstallCommand>
  </DeploymentType>
</Application>
```

## Monitoring ve Logging

### 1. Health Checks

#### Backend Health Check
```bash
curl -f http://localhost:8000/healthz
```

#### Database Health Check
```bash
pg_isready -h localhost -p 5432 -U meldenit
```

### 2. Log Monitoring

#### Backend Logs
```bash
# Docker logs
docker-compose logs -f backend

# Application logs
tail -f /var/log/meldenit/backend.log
```

#### Agent Logs
```powershell
# Windows Event Log
Get-WinEvent -LogName Application -Source "MeldenIT Agent"

# File logs
Get-Content "C:\ProgramData\MeldenIT\Agent\logs\agent.log" -Tail 100
```

### 3. Metrics

#### Prometheus Metrics
```yaml
# prometheus.yml
scrape_configs:
  - job_name: 'meldenit-backend'
    static_configs:
      - targets: ['localhost:8000']
    metrics_path: '/metrics'
```

#### Grafana Dashboard
```json
{
  "dashboard": {
    "title": "MeldenIT Agent Dashboard",
    "panels": [
      {
        "title": "Agent Count",
        "type": "stat",
        "targets": [
          {
            "expr": "meldenit_agents_total"
          }
        ]
      }
    ]
  }
}
```

## Backup ve Recovery

### 1. Database Backup

#### Otomatik Backup
```bash
#!/bin/bash
# backup.sh
DATE=$(date +%Y%m%d_%H%M%S)
pg_dump -h localhost -U meldenit meldenit > /backup/meldenit_$DATE.sql
gzip /backup/meldenit_$DATE.sql
```

#### Cron Job
```bash
# Crontab
0 2 * * * /path/to/backup.sh
```

### 2. Configuration Backup

```bash
# Konfigürasyon yedekleme
tar -czf meldenit-config-$(date +%Y%m%d).tar.gz \
  .env \
  nginx/nginx.conf \
  docker-compose.yml
```

### 3. Disaster Recovery

#### Recovery Procedure
```bash
# 1. Database restore
psql -h localhost -U meldenit meldenit < backup.sql

# 2. Configuration restore
tar -xzf meldenit-config-YYYYMMDD.tar.gz

# 3. Service restart
docker-compose down
docker-compose up -d
```

## Security

### 1. SSL/TLS Configuration

#### Nginx SSL
```nginx
server {
    listen 443 ssl http2;
    ssl_certificate /etc/ssl/certs/meldenit.crt;
    ssl_certificate_key /etc/ssl/private/meldenit.key;
    ssl_protocols TLSv1.2 TLSv1.3;
    ssl_ciphers ECDHE-RSA-AES256-GCM-SHA512:DHE-RSA-AES256-GCM-SHA512;
    ssl_prefer_server_ciphers off;
}
```

### 2. Firewall Configuration

```bash
# UFW kurulumu
sudo ufw enable
sudo ufw allow 22/tcp
sudo ufw allow 80/tcp
sudo ufw allow 443/tcp
sudo ufw deny 5432/tcp  # PostgreSQL sadece local
```

### 3. Access Control

#### API Rate Limiting
```python
# backend/app/core/middleware.py
from slowapi import Limiter, _rate_limit_exceeded_handler
from slowapi.util import get_remote_address
from slowapi.errors import RateLimitExceeded

limiter = Limiter(key_func=get_remote_address)
app.state.limiter = limiter
app.add_exception_handler(RateLimitExceeded, _rate_limit_exceeded_handler)

@app.post("/api/v1/agents/heartbeat")
@limiter.limit("10/minute")
async def heartbeat(request: Request, ...):
    pass
```

## Troubleshooting

### 1. Common Issues

#### Agent Connection Issues
```powershell
# Network connectivity test
Test-NetConnection -ComputerName assit.meldencloud.com -Port 443

# DNS resolution
Resolve-DnsName assit.meldencloud.com
```

#### Database Connection Issues
```bash
# PostgreSQL connection test
psql -h localhost -p 5432 -U meldenit -d meldenit -c "SELECT 1;"
```

### 2. Log Analysis

#### Backend Error Analysis
```bash
# Error log filtering
grep "ERROR" /var/log/meldenit/backend.log | tail -20

# Specific error search
grep "Agent not found" /var/log/meldenit/backend.log
```

#### Agent Error Analysis
```powershell
# Event log filtering
Get-WinEvent -LogName Application -Source "MeldenIT Agent" | Where-Object {$_.LevelDisplayName -eq "Error"}
```

### 3. Performance Issues

#### Database Performance
```sql
-- Slow query analysis
SELECT query, mean_time, calls 
FROM pg_stat_statements 
ORDER BY mean_time DESC 
LIMIT 10;
```

#### Application Performance
```bash
# Resource usage monitoring
docker stats meldenit-backend

# Memory usage
free -h
df -h
```

## Maintenance

### 1. Regular Maintenance

#### Database Maintenance
```sql
-- Vacuum and analyze
VACUUM ANALYZE;

-- Index maintenance
REINDEX DATABASE meldenit;
```

#### Log Rotation
```bash
# Logrotate configuration
cat > /etc/logrotate.d/meldenit << EOF
/var/log/meldenit/*.log {
    daily
    rotate 30
    compress
    delaycompress
    missingok
    notifempty
    create 644 meldenit meldenit
}
EOF
```

### 2. Updates

#### Backend Updates
```bash
# Docker image update
docker-compose pull
docker-compose up -d
```

#### Agent Updates
```powershell
# Agent update check
$agent = Get-Service -Name "MeldenITAgentSvc"
if ($agent.Status -eq "Running") {
    # Update logic here
}
```

### 3. Scaling

#### Horizontal Scaling
```yaml
# docker-compose.yml
services:
  backend:
    deploy:
      replicas: 3
    environment:
      - DATABASE_URL=postgresql://meldenit:password@postgres:5432/meldenit
```

#### Load Balancer Configuration
```nginx
upstream backend {
    server backend1:8000;
    server backend2:8000;
    server backend3:8000;
}
```
