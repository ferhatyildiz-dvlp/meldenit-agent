import pytest
import httpx
from fastapi.testclient import TestClient
from app.main import app
from app.core.database import get_db
from app.models.agent import Agent, Inventory, Heartbeat
from sqlalchemy.ext.asyncio import create_async_engine, AsyncSession
from sqlalchemy.orm import sessionmaker
import asyncio

# Test database URL
TEST_DATABASE_URL = "sqlite+aiosqlite:///./test.db"

# Create test engine
test_engine = create_async_engine(TEST_DATABASE_URL, echo=True)
TestSessionLocal = async_sessionmaker(test_engine, class_=AsyncSession, expire_on_commit=False)

# Override database dependency
async def override_get_db():
    async with TestSessionLocal() as session:
        try:
            yield session
        finally:
            await session.close()

app.dependency_overrides[get_db] = override_get_db

@pytest.fixture(scope="session")
def event_loop():
    """Create an instance of the default event loop for the test session."""
    loop = asyncio.get_event_loop_policy().new_event_loop()
    yield loop
    loop.close()

@pytest.fixture(scope="session")
async def setup_database():
    """Setup test database"""
    from app.core.database import Base
    async with test_engine.begin() as conn:
        await conn.run_sync(Base.metadata.create_all)
    yield
    async with test_engine.begin() as conn:
        await conn.run_sync(Base.metadata.drop_all)

@pytest.fixture
async def client(setup_database):
    """Create test client"""
    with TestClient(app) as client:
        yield client

@pytest.fixture
async def sample_agent():
    """Create sample agent for testing"""
    async with TestSessionLocal() as session:
        agent = Agent(
            agent_guid="test-guid-123",
            hostname="TEST-PC",
            serial_number="ABC123456",
            domain="test.local",
            site_code="TEST",
            device_token="test-token-123",
            version="1.0.0",
            status="active",
            is_online=True
        )
        session.add(agent)
        await session.commit()
        await session.refresh(agent)
        return agent

class TestAPIIntegration:
    
    def test_health_check(self, client):
        """Test health check endpoint"""
        response = client.get("/healthz")
        assert response.status_code == 200
        assert response.json()["status"] == "healthy"
    
    def test_root_endpoint(self, client):
        """Test root endpoint"""
        response = client.get("/")
        assert response.status_code == 200
        assert "MeldenIT Backend API" in response.json()["message"]
    
    def test_agent_registration(self, client):
        """Test agent registration endpoint"""
        registration_data = {
            "agent_guid": "test-guid-456",
            "hostname": "NEW-PC",
            "serial": "XYZ789012",
            "domain": "test.local",
            "version": "1.0.0",
            "site_code": "TEST"
        }
        
        response = client.post("/api/v1/agents/register", json=registration_data)
        assert response.status_code == 200
        
        data = response.json()
        assert "device_token" in data
        assert "policy" in data
        assert data["policy"]["heartbeat_interval"] == 15
    
    def test_agent_heartbeat(self, client, sample_agent):
        """Test agent heartbeat endpoint"""
        heartbeat_data = {
            "agent_guid": sample_agent.agent_guid,
            "version": "1.0.0",
            "last_sync": "2024-01-01T00:00:00Z",
            "status": "healthy"
        }
        
        response = client.post("/api/v1/agents/heartbeat", json=heartbeat_data)
        assert response.status_code == 200
        
        data = response.json()
        assert data["status"] == "success"
        assert data["message"] == "Heartbeat received"
    
    def test_inventory_sync_delta(self, client, sample_agent):
        """Test delta inventory sync"""
        inventory_data = {
            "agent_guid": sample_agent.agent_guid,
            "sync_type": "delta",
            "inventory": {
                "device_identity": {
                    "hostname": "TEST-PC",
                    "serial_number": "ABC123456"
                },
                "hardware": {
                    "manufacturer": "Dell Inc.",
                    "model": "OptiPlex 7090"
                }
            },
            "last_sync": "2024-01-01T00:00:00Z"
        }
        
        response = client.post("/api/v1/inventory/delta", json=inventory_data)
        assert response.status_code == 200
        
        data = response.json()
        assert data["status"] == "success"
        assert data["message"] == "Inventory synced successfully"
    
    def test_inventory_sync_full(self, client, sample_agent):
        """Test full inventory sync"""
        inventory_data = {
            "agent_guid": sample_agent.agent_guid,
            "sync_type": "full",
            "inventory": {
                "device_identity": {
                    "hostname": "TEST-PC",
                    "serial_number": "ABC123456"
                },
                "hardware": {
                    "manufacturer": "Dell Inc.",
                    "model": "OptiPlex 7090"
                }
            },
            "last_sync": "2024-01-01T00:00:00Z"
        }
        
        response = client.post("/api/v1/inventory/full", json=inventory_data)
        assert response.status_code == 200
        
        data = response.json()
        assert data["status"] == "success"
        assert data["message"] == "Inventory synced successfully"
    
    def test_update_check(self, client, sample_agent):
        """Test update check endpoint"""
        update_data = {
            "agent_guid": sample_agent.agent_guid,
            "current_version": "1.0.0"
        }
        
        response = client.post("/api/v1/update/check", json=update_data)
        assert response.status_code == 200
        
        data = response.json()
        assert "update_available" in data
        assert "latest_version" in data
    
    def test_agent_config(self, client, sample_agent):
        """Test agent configuration endpoint"""
        response = client.get(f"/api/v1/agents/{sample_agent.agent_guid}/config")
        assert response.status_code == 200
        
        data = response.json()
        assert "heartbeat_interval" in data
        assert "delta_sync_interval" in data
        assert "full_sync_time" in data
    
    def test_invalid_sync_type(self, client, sample_agent):
        """Test invalid sync type"""
        inventory_data = {
            "agent_guid": sample_agent.agent_guid,
            "sync_type": "invalid",
            "inventory": {},
            "last_sync": "2024-01-01T00:00:00Z"
        }
        
        response = client.post("/api/v1/inventory/delta", json=inventory_data)
        assert response.status_code == 400
        assert "Sync type must be 'delta'" in response.json()["detail"]
    
    def test_agent_not_found_heartbeat(self, client):
        """Test heartbeat with non-existent agent"""
        heartbeat_data = {
            "agent_guid": "non-existent-guid",
            "version": "1.0.0",
            "status": "healthy"
        }
        
        response = client.post("/api/v1/agents/heartbeat", json=heartbeat_data)
        assert response.status_code == 200  # Should return error status in response
        
        data = response.json()
        assert data["status"] == "error"
        assert "Agent not found" in data["message"]
    
    def test_agent_not_found_sync(self, client):
        """Test inventory sync with non-existent agent"""
        inventory_data = {
            "agent_guid": "non-existent-guid",
            "sync_type": "delta",
            "inventory": {},
            "last_sync": "2024-01-01T00:00:00Z"
        }
        
        response = client.post("/api/v1/inventory/delta", json=inventory_data)
        assert response.status_code == 200  # Should return error status in response
        
        data = response.json()
        assert data["status"] == "error"
        assert "Agent not found" in data["message"]
