from fastapi import APIRouter, Depends
from sqlalchemy.ext.asyncio import AsyncSession
from app.core.database import get_db
from app.services.agent_service import AgentService
from app.schemas.agent import (
    UpdateCheckRequest, UpdateCheckResponse
)

router = APIRouter()

@router.post("/check", response_model=UpdateCheckResponse)
async def check_for_updates(
    request: UpdateCheckRequest,
    db: AsyncSession = Depends(get_db)
):
    """Check for agent updates"""
    service = AgentService(db)
    return await service.check_for_updates(request)
