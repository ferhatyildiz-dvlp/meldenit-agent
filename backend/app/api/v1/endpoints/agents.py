from fastapi import APIRouter, Depends, HTTPException, status
from sqlalchemy.ext.asyncio import AsyncSession
from app.core.database import get_db
from app.services.agent_service import AgentService
from app.schemas.agent import (
    AgentRegistrationRequest, AgentRegistrationResponse,
    HeartbeatRequest, HeartbeatResponse,
    AgentConfigResponse
)

router = APIRouter()

@router.post("/register", response_model=AgentRegistrationResponse)
async def register_agent(
    request: AgentRegistrationRequest,
    db: AsyncSession = Depends(get_db)
):
    """Register a new agent"""
    service = AgentService(db)
    return await service.register_agent(request)

@router.post("/heartbeat", response_model=HeartbeatResponse)
async def send_heartbeat(
    request: HeartbeatRequest,
    db: AsyncSession = Depends(get_db)
):
    """Send agent heartbeat"""
    service = AgentService(db)
    return await service.send_heartbeat(request)

@router.get("/{agent_guid}/config", response_model=AgentConfigResponse)
async def get_agent_config(
    agent_guid: str,
    db: AsyncSession = Depends(get_db)
):
    """Get agent configuration"""
    service = AgentService(db)
    return await service.get_agent_config(agent_guid)
