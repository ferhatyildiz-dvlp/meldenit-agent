from sqlalchemy import Column, String, DateTime, Boolean, Text, Integer, JSON
from sqlalchemy.sql import func
from app.core.database import Base
from datetime import datetime
from typing import Optional

class Agent(Base):
    __tablename__ = "agents"
    
    id = Column(Integer, primary_key=True, index=True)
    agent_guid = Column(String(36), unique=True, index=True, nullable=False)
    hostname = Column(String(255), nullable=False)
    serial_number = Column(String(255), nullable=False)
    domain = Column(String(255), nullable=True)
    site_code = Column(String(50), nullable=False)
    device_token = Column(String(255), unique=True, nullable=False)
    version = Column(String(50), nullable=False)
    status = Column(String(50), default="active")
    last_heartbeat = Column(DateTime, nullable=True)
    last_sync = Column(DateTime, nullable=True)
    created_at = Column(DateTime, default=func.now())
    updated_at = Column(DateTime, default=func.now(), onupdate=func.now())
    is_online = Column(Boolean, default=True)
    metadata = Column(JSON, nullable=True)

class Inventory(Base):
    __tablename__ = "inventories"
    
    id = Column(Integer, primary_key=True, index=True)
    agent_id = Column(Integer, nullable=False, index=True)
    agent_guid = Column(String(36), nullable=False, index=True)
    sync_type = Column(String(20), nullable=False)  # "delta" or "full"
    inventory_data = Column(JSON, nullable=False)
    collected_at = Column(DateTime, default=func.now())
    synced_at = Column(DateTime, nullable=True)
    snipeit_updated = Column(Boolean, default=False)
    created_at = Column(DateTime, default=func.now())

class Heartbeat(Base):
    __tablename__ = "heartbeats"
    
    id = Column(Integer, primary_key=True, index=True)
    agent_id = Column(Integer, nullable=False, index=True)
    agent_guid = Column(String(36), nullable=False, index=True)
    status = Column(String(50), nullable=False)
    version = Column(String(50), nullable=False)
    last_sync = Column(DateTime, nullable=True)
    received_at = Column(DateTime, default=func.now())

class Job(Base):
    __tablename__ = "jobs"
    
    id = Column(Integer, primary_key=True, index=True)
    agent_id = Column(Integer, nullable=False, index=True)
    agent_guid = Column(String(36), nullable=False, index=True)
    job_type = Column(String(50), nullable=False)  # "sync", "update", "config"
    status = Column(String(50), default="pending")  # "pending", "running", "completed", "failed"
    payload = Column(JSON, nullable=True)
    result = Column(JSON, nullable=True)
    error_message = Column(Text, nullable=True)
    created_at = Column(DateTime, default=func.now())
    started_at = Column(DateTime, nullable=True)
    completed_at = Column(DateTime, nullable=True)

class AuditLog(Base):
    __tablename__ = "audit_logs"
    
    id = Column(Integer, primary_key=True, index=True)
    agent_id = Column(Integer, nullable=True, index=True)
    agent_guid = Column(String(36), nullable=True, index=True)
    action = Column(String(100), nullable=False)
    resource_type = Column(String(50), nullable=False)
    resource_id = Column(String(100), nullable=True)
    details = Column(JSON, nullable=True)
    ip_address = Column(String(45), nullable=True)
    user_agent = Column(String(500), nullable=True)
    created_at = Column(DateTime, default=func.now())
