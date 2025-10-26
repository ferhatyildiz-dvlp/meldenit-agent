import pytest
from unittest.mock import AsyncMock, Mock, patch
from sqlalchemy.ext.asyncio import AsyncSession
from app.services.agent_service import AgentService
from app.schemas.agent import (
    AgentRegistrationRequest, HeartbeatRequest,
    InventorySyncRequest, UpdateCheckRequest
)
from app.models.agent import Agent
from datetime import datetime

@pytest.fixture
def mock_db():
    return AsyncMock(spec=AsyncSession)

@pytest.fixture
def agent_service(mock_db):
    return AgentService(mock_db)

@pytest.fixture
def sample_registration_request():
    return AgentRegistrationRequest(
        agent_guid="test-guid-123",
        hostname="TEST-PC",
        serial="ABC123456",
        domain="test.local",
        version="1.0.0",
        site_code="TEST"
    )

@pytest.fixture
def sample_heartbeat_request():
    return HeartbeatRequest(
        agent_guid="test-guid-123",
        version="1.0.0",
        last_sync=datetime.utcnow(),
        status="healthy"
    )

@pytest.fixture
def sample_inventory_sync_request():
    return InventorySyncRequest(
        agent_guid="test-guid-123",
        sync_type="delta",
        inventory={
            "device_identity": {
                "hostname": "TEST-PC",
                "serial_number": "ABC123456"
            }
        },
        last_sync=datetime.utcnow()
    )

@pytest.fixture
def sample_update_check_request():
    return UpdateCheckRequest(
        agent_guid="test-guid-123",
        current_version="1.0.0"
    )

class TestAgentService:
    
    @pytest.mark.asyncio
    async def test_register_agent_new(self, agent_service, sample_registration_request):
        """Test registering a new agent"""
        # Mock database query to return no existing agent
        agent_service.db.execute.return_value.scalar_one_or_none.return_value = None
        
        # Mock database operations
        agent_service.db.add = Mock()
        agent_service.db.commit = AsyncMock()
        agent_service.db.refresh = AsyncMock()
        agent_service._log_audit = AsyncMock()
        
        result = await agent_service.register_agent(sample_registration_request)
        
        assert result.device_token is not None
        assert result.policy is not None
        assert result.policy["heartbeat_interval"] == 15
        
        # Verify database operations
        agent_service.db.add.assert_called_once()
        agent_service.db.commit.assert_called_once()
        agent_service._log_audit.assert_called_once()
    
    @pytest.mark.asyncio
    async def test_register_agent_existing(self, agent_service, sample_registration_request):
        """Test updating an existing agent"""
        # Mock existing agent
        existing_agent = Mock()
        existing_agent.hostname = "OLD-PC"
        existing_agent.serial_number = "OLD123"
        
        agent_service.db.execute.return_value.scalar_one_or_none.return_value = existing_agent
        agent_service.db.commit = AsyncMock()
        agent_service.db.refresh = AsyncMock()
        agent_service._log_audit = AsyncMock()
        
        result = await agent_service.register_agent(sample_registration_request)
        
        assert result.device_token is not None
        assert existing_agent.hostname == sample_registration_request.hostname
        assert existing_agent.serial_number == sample_registration_request.serial
        
        agent_service.db.commit.assert_called_once()
        agent_service._log_audit.assert_called_once()
    
    @pytest.mark.asyncio
    async def test_send_heartbeat_success(self, agent_service, sample_heartbeat_request):
        """Test successful heartbeat"""
        # Mock existing agent
        agent = Mock()
        agent.id = 1
        agent.agent_guid = sample_heartbeat_request.agent_guid
        
        agent_service.db.execute.return_value.scalar_one_or_none.return_value = agent
        agent_service.db.commit = AsyncMock()
        
        result = await agent_service.send_heartbeat(sample_heartbeat_request)
        
        assert result.status == "success"
        assert result.message == "Heartbeat received"
        assert result.config_updated is False
        
        agent_service.db.commit.assert_called_once()
    
    @pytest.mark.asyncio
    async def test_send_heartbeat_agent_not_found(self, agent_service, sample_heartbeat_request):
        """Test heartbeat when agent not found"""
        agent_service.db.execute.return_value.scalar_one_or_none.return_value = None
        
        result = await agent_service.send_heartbeat(sample_heartbeat_request)
        
        assert result.status == "error"
        assert result.message == "Agent not found"
    
    @pytest.mark.asyncio
    async def test_sync_inventory_success(self, agent_service, sample_inventory_sync_request):
        """Test successful inventory sync"""
        # Mock existing agent
        agent = Mock()
        agent.id = 1
        agent.agent_guid = sample_inventory_sync_request.agent_guid
        
        agent_service.db.execute.return_value.scalar_one_or_none.return_value = agent
        agent_service.db.commit = AsyncMock()
        agent_service._sync_to_snipeit = AsyncMock(return_value=True)
        agent_service._log_audit = AsyncMock()
        
        result = await agent_service.sync_inventory(sample_inventory_sync_request)
        
        assert result.status == "success"
        assert result.message == "Inventory synced successfully"
        assert result.snipeit_updated is True
        
        agent_service.db.commit.assert_called()
        agent_service._sync_to_snipeit.assert_called_once()
        agent_service._log_audit.assert_called_once()
    
    @pytest.mark.asyncio
    async def test_sync_inventory_agent_not_found(self, agent_service, sample_inventory_sync_request):
        """Test inventory sync when agent not found"""
        agent_service.db.execute.return_value.scalar_one_or_none.return_value = None
        
        result = await agent_service.sync_inventory(sample_inventory_sync_request)
        
        assert result.status == "error"
        assert result.message == "Agent not found"
    
    @pytest.mark.asyncio
    async def test_check_for_updates_available(self, agent_service, sample_update_check_request):
        """Test update check when update is available"""
        with patch('app.services.agent_service.settings') as mock_settings:
            mock_settings.UPDATE_CHECK_ENABLED = True
            mock_settings.LATEST_VERSION = "1.1.0"
            mock_settings.UPDATE_DOWNLOAD_URL = "https://example.com/download"
            
            result = await agent_service.check_for_updates(sample_update_check_request)
            
            assert result.update_available is True
            assert result.latest_version == "1.1.0"
            assert result.download_url == "https://example.com/download"
    
    @pytest.mark.asyncio
    async def test_check_for_updates_not_available(self, agent_service, sample_update_check_request):
        """Test update check when no update is available"""
        with patch('app.services.agent_service.settings') as mock_settings:
            mock_settings.UPDATE_CHECK_ENABLED = True
            mock_settings.LATEST_VERSION = "1.0.0"
            
            result = await agent_service.check_for_updates(sample_update_check_request)
            
            assert result.update_available is False
            assert result.latest_version is None
    
    @pytest.mark.asyncio
    async def test_check_for_updates_disabled(self, agent_service, sample_update_check_request):
        """Test update check when disabled"""
        with patch('app.services.agent_service.settings') as mock_settings:
            mock_settings.UPDATE_CHECK_ENABLED = False
            
            result = await agent_service.check_for_updates(sample_update_check_request)
            
            assert result.update_available is False
    
    @pytest.mark.asyncio
    async def test_get_agent_config(self, agent_service):
        """Test getting agent configuration"""
        with patch('app.services.agent_service.settings') as mock_settings:
            mock_settings.AGENT_HEARTBEAT_INTERVAL = 15
            mock_settings.AGENT_DELTA_SYNC_INTERVAL = 360
            mock_settings.AGENT_FULL_SYNC_TIME = "03:00"
            mock_settings.AGENT_MAX_RETRY_ATTEMPTS = 3
            mock_settings.AGENT_RETRY_DELAY_SECONDS = 30
            
            result = await agent_service.get_agent_config("test-guid")
            
            assert result.heartbeat_interval == 15
            assert result.delta_sync_interval == 360
            assert result.full_sync_time == "03:00"
            assert result.max_retry_attempts == 3
            assert result.retry_delay_seconds == 30
