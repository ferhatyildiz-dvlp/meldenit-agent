#!/bin/bash

# MeldenIT Agent Deployment Script
# Bash script for deploying the backend services

set -e

# Configuration
COMPOSE_FILE="docker-compose.yml"
ENV_FILE=".env"
BACKUP_DIR="./backups"
LOG_FILE="./deployment.log"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Logging function
log() {
    echo -e "${BLUE}[$(date +'%Y-%m-%d %H:%M:%S')]${NC} $1" | tee -a "$LOG_FILE"
}

error() {
    echo -e "${RED}[ERROR]${NC} $1" | tee -a "$LOG_FILE"
    exit 1
}

success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1" | tee -a "$LOG_FILE"
}

warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1" | tee -a "$LOG_FILE"
}

# Check prerequisites
check_prerequisites() {
    log "Checking prerequisites..."
    
    # Check Docker
    if ! command -v docker &> /dev/null; then
        error "Docker is not installed"
    fi
    
    # Check Docker Compose
    if ! command -v docker-compose &> /dev/null; then
        error "Docker Compose is not installed"
    fi
    
    # Check if .env file exists
    if [ ! -f "$ENV_FILE" ]; then
        warning ".env file not found. Creating from example..."
        if [ -f "env.example" ]; then
            cp env.example "$ENV_FILE"
            warning "Please edit $ENV_FILE with your configuration"
        else
            error "env.example not found"
        fi
    fi
    
    success "Prerequisites check completed"
}

# Create backup
create_backup() {
    log "Creating backup..."
    
    if [ ! -d "$BACKUP_DIR" ]; then
        mkdir -p "$BACKUP_DIR"
    fi
    
    local backup_name="backup_$(date +'%Y%m%d_%H%M%S')"
    local backup_path="$BACKUP_DIR/$backup_name"
    
    mkdir -p "$backup_path"
    
    # Backup database if running
    if docker-compose ps postgres | grep -q "Up"; then
        log "Backing up database..."
        docker-compose exec -T postgres pg_dump -U meldenit meldenit > "$backup_path/database.sql"
    fi
    
    # Backup volumes
    if [ -d "volumes" ]; then
        cp -r volumes "$backup_path/"
    fi
    
    success "Backup created: $backup_path"
}

# Pull latest images
pull_images() {
    log "Pulling latest images..."
    docker-compose pull
    success "Images pulled successfully"
}

# Build custom images
build_images() {
    log "Building custom images..."
    docker-compose build --no-cache
    success "Images built successfully"
}

# Start services
start_services() {
    log "Starting services..."
    
    # Start database first
    docker-compose up -d postgres redis
    
    # Wait for database to be ready
    log "Waiting for database to be ready..."
    sleep 10
    
    # Run database migrations
    log "Running database migrations..."
    docker-compose run --rm backend python scripts/init_db.py
    
    # Start all services
    docker-compose up -d
    
    success "Services started successfully"
}

# Check service health
check_health() {
    log "Checking service health..."
    
    # Wait for services to start
    sleep 30
    
    # Check backend health
    if curl -f http://localhost:8000/healthz > /dev/null 2>&1; then
        success "Backend is healthy"
    else
        error "Backend health check failed"
    fi
    
    # Check database
    if docker-compose exec -T postgres pg_isready -U meldenit > /dev/null 2>&1; then
        success "Database is healthy"
    else
        error "Database health check failed"
    fi
}

# Setup SSL certificates
setup_ssl() {
    log "Setting up SSL certificates..."
    
    # Check if domain is configured
    if [ -z "$DOMAIN_NAME" ]; then
        warning "DOMAIN_NAME not set, skipping SSL setup"
        return
    fi
    
    # Create initial certificate
    docker-compose run --rm certbot certonly --webroot --webroot-path=/var/www/certbot --email "$SSL_EMAIL" --agree-tos --no-eff-email -d "$DOMAIN_NAME"
    
    # Copy certificates to nginx
    docker-compose exec nginx sh -c "cp /etc/letsencrypt/live/$DOMAIN_NAME/fullchain.pem /etc/nginx/ssl/ && cp /etc/letsencrypt/live/$DOMAIN_NAME/privkey.pem /etc/nginx/ssl/"
    
    # Reload nginx
    docker-compose exec nginx nginx -s reload
    
    success "SSL certificates configured"
}

# Show service status
show_status() {
    log "Service Status:"
    docker-compose ps
    
    echo ""
    log "Service URLs:"
    echo "  Backend API: http://localhost:8000"
    echo "  API Documentation: http://localhost:8000/docs"
    echo "  Health Check: http://localhost:8000/healthz"
    
    if [ ! -z "$DOMAIN_NAME" ]; then
        echo "  Production URL: https://$DOMAIN_NAME"
    fi
}

# Stop services
stop_services() {
    log "Stopping services..."
    docker-compose down
    success "Services stopped"
}

# Clean up
cleanup() {
    log "Cleaning up..."
    docker-compose down -v
    docker system prune -f
    success "Cleanup completed"
}

# Main deployment function
deploy() {
    log "Starting MeldenIT Agent deployment..."
    
    check_prerequisites
    create_backup
    pull_images
    build_images
    start_services
    check_health
    
    # Setup SSL if domain is configured
    if [ ! -z "$DOMAIN_NAME" ]; then
        setup_ssl
    fi
    
    show_status
    
    success "Deployment completed successfully!"
}

# Parse command line arguments
case "${1:-deploy}" in
    deploy)
        deploy
        ;;
    start)
        start_services
        ;;
    stop)
        stop_services
        ;;
    restart)
        stop_services
        start_services
        ;;
    status)
        show_status
        ;;
    backup)
        create_backup
        ;;
    cleanup)
        cleanup
        ;;
    ssl)
        setup_ssl
        ;;
    *)
        echo "Usage: $0 {deploy|start|stop|restart|status|backup|cleanup|ssl}"
        exit 1
        ;;
esac
