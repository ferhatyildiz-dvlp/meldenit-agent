from pydantic import BaseModel, Field
from typing import Optional, Dict, Any
from datetime import datetime

class AgentRegistrationRequest(BaseModel):
    agent_guid: str = Field(..., description="Agent GUID")
    hostname: str = Field(..., description="Hostname")
    serial: str = Field(..., description="Serial number")
    domain: str = Field(..., description="Domain")
    version: str = Field(..., description="Agent version")
    site_code: str = Field(..., description="Site code")

class AgentRegistrationResponse(BaseModel):
    device_token: str = Field(..., description="Device authentication token")
    policy: Dict[str, Any] = Field(..., description="Agent policy configuration")

class HeartbeatRequest(BaseModel):
    agent_guid: str = Field(..., description="Agent GUID")
    version: str = Field(..., description="Agent version")
    last_sync: Optional[datetime] = Field(None, description="Last sync time")
    status: str = Field(..., description="Agent status")

class HeartbeatResponse(BaseModel):
    status: str = Field(..., description="Response status")
    message: Optional[str] = Field(None, description="Response message")
    config_updated: bool = Field(False, description="Whether config was updated")

class InventorySyncRequest(BaseModel):
    agent_guid: str = Field(..., description="Agent GUID")
    sync_type: str = Field(..., description="Sync type (delta or full)")
    inventory: Dict[str, Any] = Field(..., description="Inventory data")
    last_sync: Optional[datetime] = Field(None, description="Last sync time")

class InventorySyncResponse(BaseModel):
    status: str = Field(..., description="Response status")
    message: Optional[str] = Field(None, description="Response message")
    snipeit_updated: bool = Field(False, description="Whether Snipe-IT was updated")
    next_sync: Optional[datetime] = Field(None, description="Next sync time")

class UpdateCheckRequest(BaseModel):
    agent_guid: str = Field(..., description="Agent GUID")
    current_version: str = Field(..., description="Current agent version")

class UpdateCheckResponse(BaseModel):
    update_available: bool = Field(False, description="Whether update is available")
    latest_version: Optional[str] = Field(None, description="Latest version")
    download_url: Optional[str] = Field(None, description="Download URL")
    release_notes: Optional[str] = Field(None, description="Release notes")
    force_update: bool = Field(False, description="Whether update is forced")

class AgentConfigResponse(BaseModel):
    heartbeat_interval: int = Field(15, description="Heartbeat interval in minutes")
    delta_sync_interval: int = Field(360, description="Delta sync interval in minutes")
    full_sync_time: str = Field("03:00", description="Full sync time")
    max_retry_attempts: int = Field(3, description="Max retry attempts")
    retry_delay_seconds: int = Field(30, description="Retry delay in seconds")

class AgentInfo(BaseModel):
    id: int
    agent_guid: str
    hostname: str
    serial_number: str
    domain: Optional[str]
    site_code: str
    version: str
    status: str
    last_heartbeat: Optional[datetime]
    last_sync: Optional[datetime]
    is_online: bool
    created_at: datetime
    updated_at: datetime

    class Config:
        from_attributes = True
