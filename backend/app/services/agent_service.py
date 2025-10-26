from sqlalchemy.ext.asyncio import AsyncSession
from sqlalchemy import select, update
from app.models.agent import Agent, Heartbeat, Inventory, Job, AuditLog
from app.schemas.agent import (
    AgentRegistrationRequest, AgentRegistrationResponse,
    HeartbeatRequest, HeartbeatResponse,
    InventorySyncRequest, InventorySyncResponse,
    UpdateCheckRequest, UpdateCheckResponse,
    AgentConfigResponse
)
from app.core.config import settings
from datetime import datetime, timedelta
import logging
import secrets

logger = logging.getLogger(__name__)

class AgentService:
    def __init__(self, db: AsyncSession):
        self.db = db

    async def register_agent(self, request: AgentRegistrationRequest) -> AgentRegistrationResponse:
        """Register a new agent"""
        logger.info(f"Registering agent {request.agent_guid}")
        
        # Check if agent already exists
        result = await self.db.execute(
            select(Agent).where(Agent.agent_guid == request.agent_guid)
        )
        existing_agent = result.scalar_one_or_none()
        
        if existing_agent:
            # Update existing agent
            existing_agent.hostname = request.hostname
            existing_agent.serial_number = request.serial
            existing_agent.domain = request.domain
            existing_agent.site_code = request.site_code
            existing_agent.version = request.version
            existing_agent.updated_at = datetime.utcnow()
            agent = existing_agent
        else:
            # Create new agent
            device_token = secrets.token_urlsafe(32)
            agent = Agent(
                agent_guid=request.agent_guid,
                hostname=request.hostname,
                serial_number=request.serial,
                domain=request.domain,
                site_code=request.site_code,
                device_token=device_token,
                version=request.version,
                status="active",
                is_online=True
            )
            self.db.add(agent)
        
        await self.db.commit()
        await self.db.refresh(agent)
        
        # Log audit
        await self._log_audit(
            agent_id=agent.id,
            agent_guid=agent.agent_guid,
            action="agent_registered",
            resource_type="agent",
            resource_id=str(agent.id)
        )
        
        # Create policy response
        policy = {
            "heartbeat_interval": settings.AGENT_HEARTBEAT_INTERVAL,
            "delta_sync_interval": settings.AGENT_DELTA_SYNC_INTERVAL,
            "full_sync_time": settings.AGENT_FULL_SYNC_TIME,
            "max_retry_attempts": settings.AGENT_MAX_RETRY_ATTEMPTS,
            "retry_delay_seconds": settings.AGENT_RETRY_DELAY_SECONDS
        }
        
        return AgentRegistrationResponse(
            device_token=agent.device_token,
            policy=policy
        )

    async def send_heartbeat(self, request: HeartbeatRequest) -> HeartbeatResponse:
        """Process agent heartbeat"""
        logger.debug(f"Processing heartbeat for agent {request.agent_guid}")
        
        # Get agent
        result = await self.db.execute(
            select(Agent).where(Agent.agent_guid == request.agent_guid)
        )
        agent = result.scalar_one_or_none()
        
        if not agent:
            logger.warning(f"Agent {request.agent_guid} not found")
            return HeartbeatResponse(
                status="error",
                message="Agent not found"
            )
        
        # Update agent status
        agent.last_heartbeat = datetime.utcnow()
        agent.is_online = True
        agent.status = request.status
        agent.version = request.version
        
        # Create heartbeat record
        heartbeat = Heartbeat(
            agent_id=agent.id,
            agent_guid=agent.agent_guid,
            status=request.status,
            version=request.version,
            last_sync=request.last_sync
        )
        self.db.add(heartbeat)
        
        await self.db.commit()
        
        return HeartbeatResponse(
            status="success",
            message="Heartbeat received",
            config_updated=False
        )

    async def sync_inventory(self, request: InventorySyncRequest) -> InventorySyncResponse:
        """Process inventory sync"""
        logger.info(f"Processing {request.sync_type} inventory sync for agent {request.agent_guid}")
        
        # Get agent
        result = await self.db.execute(
            select(Agent).where(Agent.agent_guid == request.agent_guid)
        )
        agent = result.scalar_one_or_none()
        
        if not agent:
            logger.warning(f"Agent {request.agent_guid} not found")
            return InventorySyncResponse(
                status="error",
                message="Agent not found"
            )
        
        # Create inventory record
        inventory = Inventory(
            agent_id=agent.id,
            agent_guid=agent.agent_guid,
            sync_type=request.sync_type,
            inventory_data=request.inventory,
            synced_at=datetime.utcnow()
        )
        self.db.add(inventory)
        
        # Update agent last sync
        agent.last_sync = datetime.utcnow()
        
        await self.db.commit()
        
        # TODO: Integrate with Snipe-IT
        snipeit_updated = await self._sync_to_snipeit(agent, request.inventory)
        inventory.snipeit_updated = snipeit_updated
        await self.db.commit()
        
        # Log audit
        await self._log_audit(
            agent_id=agent.id,
            agent_guid=agent.agent_guid,
            action="inventory_synced",
            resource_type="inventory",
            resource_id=str(inventory.id),
            details={"sync_type": request.sync_type, "snipeit_updated": snipeit_updated}
        )
        
        return InventorySyncResponse(
            status="success",
            message="Inventory synced successfully",
            snipeit_updated=snipeit_updated,
            next_sync=datetime.utcnow() + timedelta(minutes=settings.AGENT_DELTA_SYNC_INTERVAL)
        )

    async def check_for_updates(self, request: UpdateCheckRequest) -> UpdateCheckResponse:
        """Check for agent updates"""
        logger.debug(f"Checking for updates for agent {request.agent_guid}")
        
        if not settings.UPDATE_CHECK_ENABLED:
            return UpdateCheckResponse(update_available=False)
        
        current_version = request.current_version
        latest_version = settings.LATEST_VERSION
        
        # Simple version comparison (in production, use proper semver)
        update_available = current_version != latest_version
        
        return UpdateCheckResponse(
            update_available=update_available,
            latest_version=latest_version if update_available else None,
            download_url=settings.UPDATE_DOWNLOAD_URL if update_available else None,
            release_notes="Bug fixes and improvements" if update_available else None,
            force_update=False
        )

    async def get_agent_config(self, agent_guid: str) -> AgentConfigResponse:
        """Get agent configuration"""
        return AgentConfigResponse(
            heartbeat_interval=settings.AGENT_HEARTBEAT_INTERVAL,
            delta_sync_interval=settings.AGENT_DELTA_SYNC_INTERVAL,
            full_sync_time=settings.AGENT_FULL_SYNC_TIME,
            max_retry_attempts=settings.AGENT_MAX_RETRY_ATTEMPTS,
            retry_delay_seconds=settings.AGENT_RETRY_DELAY_SECONDS
        )

    async def _sync_to_snipeit(self, agent: Agent, inventory_data: dict) -> bool:
        """Sync inventory data to Snipe-IT"""
        try:
            from app.services.snipeit_service import SnipeItService
            
            snipeit_service = SnipeItService()
            return await snipeit_service.sync_inventory_to_snipeit(agent.agent_guid, inventory_data)
            
        except Exception as e:
            logger.error(f"Error syncing to Snipe-IT: {e}")
            return False

    async def _log_audit(self, agent_id: int, agent_guid: str, action: str, 
                        resource_type: str, resource_id: str = None, 
                        details: dict = None):
        """Log audit event"""
        audit_log = AuditLog(
            agent_id=agent_id,
            agent_guid=agent_guid,
            action=action,
            resource_type=resource_type,
            resource_id=resource_id,
            details=details
        )
        self.db.add(audit_log)
        await self.db.commit()
