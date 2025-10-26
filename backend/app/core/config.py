from pydantic_settings import BaseSettings
from typing import List, Optional
import os

class Settings(BaseSettings):
    # Database
    DATABASE_URL: str = "postgresql://user:password@localhost/meldenit"
    
    # Security
    SECRET_KEY: str = "your-secret-key-change-this-in-production"
    ALGORITHM: str = "HS256"
    ACCESS_TOKEN_EXPIRE_MINUTES: int = 30
    
    # CORS
    ALLOWED_ORIGINS: List[str] = ["*"]
    
    # Snipe-IT Integration
    SNIPEIT_BASE_URL: str = "https://assit.meldencloud.com"
    SNIPEIT_API_TOKEN: str = "your-snipeit-api-token"
    
    # Agent Configuration
    AGENT_HEARTBEAT_INTERVAL: int = 15  # minutes
    AGENT_DELTA_SYNC_INTERVAL: int = 360  # minutes
    AGENT_FULL_SYNC_TIME: str = "03:00"
    AGENT_MAX_RETRY_ATTEMPTS: int = 3
    AGENT_RETRY_DELAY_SECONDS: int = 30
    
    # Logging
    LOG_LEVEL: str = "INFO"
    LOG_FORMAT: str = "%(asctime)s - %(name)s - %(levelname)s - %(message)s"
    
    # Update Management
    UPDATE_CHECK_ENABLED: bool = True
    LATEST_VERSION: str = "1.0.0"
    UPDATE_DOWNLOAD_URL: str = "https://assit.meldencloud.com/downloads/"
    
    class Config:
        env_file = ".env"
        case_sensitive = True

# Create settings instance
settings = Settings()
