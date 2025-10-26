from fastapi import FastAPI, HTTPException, Depends, status
from fastapi.middleware.cors import CORSMiddleware
from fastapi.security import HTTPBearer, HTTPAuthorizationCredentials
from contextlib import asynccontextmanager
import logging
import os
from dotenv import load_dotenv

from app.core.config import settings
from app.core.database import engine, Base
from app.api.v1.api import api_router
from app.core.logging import setup_logging

# Load environment variables
load_dotenv()

# Setup logging
setup_logging()
logger = logging.getLogger(__name__)

@asynccontextmanager
async def lifespan(app: FastAPI):
    # Startup
    logger.info("Starting MeldenIT Backend API")
    
    # Create database tables
    async with engine.begin() as conn:
        await conn.run_sync(Base.metadata.create_all)
    
    logger.info("Database tables created/verified")
    yield
    
    # Shutdown
    logger.info("Shutting down MeldenIT Backend API")

# Create FastAPI app
app = FastAPI(
    title="MeldenIT Backend API",
    description="Merkez sunucu API'si - Snipe-IT entegrasyonlu envanter yönetimi",
    version="1.0.0",
    lifespan=lifespan
)

# Add CORS middleware
app.add_middleware(
    CORSMiddleware,
    allow_origins=settings.ALLOWED_ORIGINS,
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

# Include API router
app.include_router(api_router, prefix="/api/v1")

@app.get("/healthz")
async def health_check():
    """Health check endpoint"""
    return {"status": "healthy", "service": "MeldenIT Backend API"}

@app.get("/")
async def root():
    """Root endpoint"""
    return {
        "message": "MeldenIT Backend API",
        "version": "1.0.0",
        "docs": "/docs"
    }

if __name__ == "__main__":
    import uvicorn
    uvicorn.run(
        "app.main:app",
        host="0.0.0.0",
        port=8000,
        reload=True,
        log_level="info"
    )
