from fastapi import APIRouter, Depends, HTTPException, status
from sqlalchemy.ext.asyncio import AsyncSession
from app.core.database import get_db
from app.services.agent_service import AgentService
from app.schemas.agent import (
    InventorySyncRequest, InventorySyncResponse
)

router = APIRouter()

@router.post("/delta", response_model=InventorySyncResponse)
async def sync_delta_inventory(
    request: InventorySyncRequest,
    db: AsyncSession = Depends(get_db)
):
    """Sync delta inventory changes"""
    if request.sync_type != "delta":
        raise HTTPException(
            status_code=status.HTTP_400_BAD_REQUEST,
            detail="Sync type must be 'delta'"
        )
    
    service = AgentService(db)
    return await service.sync_inventory(request)

@router.post("/full", response_model=InventorySyncResponse)
async def sync_full_inventory(
    request: InventorySyncRequest,
    db: AsyncSession = Depends(get_db)
):
    """Sync full inventory"""
    if request.sync_type != "full":
        raise HTTPException(
            status_code=status.HTTP_400_BAD_REQUEST,
            detail="Sync type must be 'full'"
        )
    
    service = AgentService(db)
    return await service.sync_inventory(request)
