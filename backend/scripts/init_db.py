#!/usr/bin/env python3
"""
Database initialization script
"""
import asyncio
import sys
import os

# Add the app directory to the Python path
sys.path.append(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

from app.core.database import engine, Base
from app.core.config import settings
import logging

logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

async def init_db():
    """Initialize database tables"""
    try:
        logger.info("Creating database tables...")
        async with engine.begin() as conn:
            await conn.run_sync(Base.metadata.create_all)
        logger.info("Database tables created successfully")
    except Exception as e:
        logger.error(f"Error creating database tables: {e}")
        raise

async def main():
    """Main function"""
    logger.info("Initializing database...")
    logger.info(f"Database URL: {settings.DATABASE_URL}")
    await init_db()
    logger.info("Database initialization completed")

if __name__ == "__main__":
    asyncio.run(main())
