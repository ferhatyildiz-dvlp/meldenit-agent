from fastapi import APIRouter
from app.api.v1.endpoints import agents, inventory, updates

api_router = APIRouter()

api_router.include_router(agents.router, prefix="/agents", tags=["agents"])
api_router.include_router(inventory.router, prefix="/inventory", tags=["inventory"])
api_router.include_router(updates.router, prefix="/update", tags=["updates"])
