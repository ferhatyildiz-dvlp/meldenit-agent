import pytest
import asyncio
import httpx
from unittest.mock import AsyncMock, patch
from datetime import datetime
import json

# This is an end-to-end test that simulates the full agent-backend interaction
# In a real scenario, this would test against a running backend instance

class TestAgentBackendIntegration:
    
    @pytest.fixture
    async def backend_client(self):
        """Create HTTP client for backend API"""
        async with httpx.AsyncClient(base_url="http://localhost:8000") as client:
            yield client
    
    @pytest.fixture
    def sample_inventory_data(self):
        """Sample inventory data that would be collected by the agent"""
        return {
            "device_identity": {
                "hostname": "E2E-TEST-PC",
                "domain": "test.local",
                "ou": "OU=Computers,DC=test,DC=local",
                "sid": "S-1-5-21-1234567890-1234567890-1234567890-1001",
                "uuid": "12345678-1234-1234-1234-123456789012",
                "serial_number": "E2E123456789",
                "asset_tag": None,
                "logged_in_user": "testuser"
            },
            "hardware": {
                "manufacturer": "Dell Inc.",
                "model": "OptiPlex 7090",
                "cpu": {
                    "name": "Intel Core i7-11700",
                    "cores": 8,
                    "logical_processors": 16,
                    "max_clock_speed": 3200
                },
                "memory": {
                    "total_gb": 32.0,
                    "slots": [
                        {
                            "capacity_gb": 16.0,
                            "speed_mhz": 3200,
                            "manufacturer": "Corsair",
                            "part_number": "CMK32GX4M2B3200C16"
                        },
                        {
                            "capacity_gb": 16.0,
                            "speed_mhz": 3200,
                            "manufacturer": "Corsair",
                            "part_number": "CMK32GX4M2B3200C16"
                        }
                    ]
                },
                "disks": [
                    {
                        "model": "Samsung SSD 980 PRO",
                        "capacity_gb": 1000.0,
                        "type": "SSD",
                        "partitions": [
                            {
                                "drive_letter": "C:",
                                "size_gb": 900.0,
                                "free_space_gb": 450.0,
                                "file_system": "NTFS"
                            }
                        ]
                    }
                ],
                "gpu": {
                    "name": "NVIDIA GeForce RTX 3060",
                    "memory_mb": 12288,
                    "driver_version": "31.0.15.3640"
                }
            },
            "software": {
                "os_name": "Microsoft Windows 11 Pro",
                "os_version": "10.0.22621",
                "os_build": "22621",
                "installed_software": [
                    {
                        "name": "Microsoft Office 365",
                        "version": "16.0.14326.20404",
                        "publisher": "Microsoft Corporation",
                        "install_date": "2024-01-15T00:00:00Z"
                    },
                    {
                        "name": "Google Chrome",
                        "version": "120.0.6099.109",
                        "publisher": "Google LLC",
                        "install_date": "2024-01-10T00:00:00Z"
                    }
                ],
                "dotnet_versions": ["v4.0.30319", "v8.0.0"]
            },
            "network": {
                "adapters": [
                    {
                        "name": "Intel(R) Ethernet Connection (7) I219-LM",
                        "mac_address": "00:11:22:33:44:55",
                        "connection_type": "Ethernet",
                        "is_connected": True
                    },
                    {
                        "name": "Intel(R) Wi-Fi 6 AX201 160MHz",
                        "mac_address": "00:11:22:33:44:66",
                        "connection_type": "WiFi",
                        "is_connected": False
                    }
                ],
                "ipv4_addresses": ["192.168.1.100"],
                "ipv6_addresses": ["2001:db8::1"],
                "gateway": "192.168.1.1",
                "dns_servers": ["8.8.8.8", "8.8.4.4"],
                "wifi_ssid": "TestNetwork"
            },
            "bios": {
                "name": "Dell Inc.",
                "version": "1.0.0",
                "release_date": "2023-01-01T00:00:00Z",
                "tpm_enabled": True,
                "secure_boot": True
            },
            "usage": {
                "uptime_hours": 168.5,
                "last_reboot": "2024-01-01T00:00:00Z",
                "cpu_usage_avg": 25.5,
                "memory_usage_avg": 60.2,
                "disk_io_avg": 15.3
            },
            "tagging": {
                "location": "Office Building A, Floor 2",
                "site_code": "E2E-TEST",
                "department": "IT",
                "cost_center": "IT-001"
            },
            "collected_at": "2024-01-15T10:30:00Z"
        }
    
    @pytest.mark.asyncio
    async def test_full_agent_lifecycle(self, backend_client, sample_inventory_data):
        """Test complete agent lifecycle: registration -> heartbeat -> sync -> update check"""
        
        # Step 1: Agent Registration
        registration_data = {
            "agent_guid": "e2e-test-guid-123",
            "hostname": "E2E-TEST-PC",
            "serial": "E2E123456789",
            "domain": "test.local",
            "version": "1.0.0",
            "site_code": "E2E-TEST"
        }
        
        response = await backend_client.post("/api/v1/agents/register", json=registration_data)
        assert response.status_code == 200
        
        registration_result = response.json()
        assert "device_token" in registration_result
        assert "policy" in registration_result
        
        device_token = registration_result["device_token"]
        
        # Step 2: Send Heartbeat
        heartbeat_data = {
            "agent_guid": "e2e-test-guid-123",
            "version": "1.0.0",
            "last_sync": None,
            "status": "healthy"
        }
        
        response = await backend_client.post("/api/v1/agents/heartbeat", json=heartbeat_data)
        assert response.status_code == 200
        
        heartbeat_result = response.json()
        assert heartbeat_result["status"] == "success"
        
        # Step 3: Full Inventory Sync
        sync_data = {
            "agent_guid": "e2e-test-guid-123",
            "sync_type": "full",
            "inventory": sample_inventory_data,
            "last_sync": None
        }
        
        response = await backend_client.post("/api/v1/inventory/full", json=sync_data)
        assert response.status_code == 200
        
        sync_result = response.json()
        assert sync_result["status"] == "success"
        assert sync_result["message"] == "Inventory synced successfully"
        
        # Step 4: Delta Inventory Sync
        delta_sync_data = {
            "agent_guid": "e2e-test-guid-123",
            "sync_type": "delta",
            "inventory": sample_inventory_data,
            "last_sync": "2024-01-15T10:30:00Z"
        }
        
        response = await backend_client.post("/api/v1/inventory/delta", json=delta_sync_data)
        assert response.status_code == 200
        
        delta_sync_result = response.json()
        assert delta_sync_result["status"] == "success"
        
        # Step 5: Update Check
        update_data = {
            "agent_guid": "e2e-test-guid-123",
            "current_version": "1.0.0"
        }
        
        response = await backend_client.post("/api/v1/update/check", json=update_data)
        assert response.status_code == 200
        
        update_result = response.json()
        assert "update_available" in update_result
        assert "latest_version" in update_result
        
        # Step 6: Get Agent Configuration
        response = await backend_client.get("/api/v1/agents/e2e-test-guid-123/config")
        assert response.status_code == 200
        
        config_result = response.json()
        assert "heartbeat_interval" in config_result
        assert "delta_sync_interval" in config_result
        assert "full_sync_time" in config_result
    
    @pytest.mark.asyncio
    async def test_agent_error_handling(self, backend_client):
        """Test agent error handling scenarios"""
        
        # Test registration with invalid data
        invalid_registration = {
            "agent_guid": "",  # Empty GUID
            "hostname": "TEST-PC",
            "serial": "ABC123",
            "domain": "test.local",
            "version": "1.0.0",
            "site_code": "TEST"
        }
        
        response = await backend_client.post("/api/v1/agents/register", json=invalid_registration)
        # Should still work but with empty GUID
        
        # Test heartbeat with non-existent agent
        heartbeat_data = {
            "agent_guid": "non-existent-guid",
            "version": "1.0.0",
            "status": "healthy"
        }
        
        response = await backend_client.post("/api/v1/agents/heartbeat", json=heartbeat_data)
        assert response.status_code == 200
        
        result = response.json()
        assert result["status"] == "error"
        assert "Agent not found" in result["message"]
        
        # Test inventory sync with non-existent agent
        sync_data = {
            "agent_guid": "non-existent-guid",
            "sync_type": "delta",
            "inventory": {},
            "last_sync": None
        }
        
        response = await backend_client.post("/api/v1/inventory/delta", json=sync_data)
        assert response.status_code == 200
        
        result = response.json()
        assert result["status"] == "error"
        assert "Agent not found" in result["message"]
    
    @pytest.mark.asyncio
    async def test_concurrent_agents(self, backend_client):
        """Test multiple agents registering and syncing concurrently"""
        
        # Register multiple agents
        agents = []
        for i in range(5):
            registration_data = {
                "agent_guid": f"concurrent-test-guid-{i}",
                "hostname": f"CONCURRENT-PC-{i}",
                "serial": f"CONC{i:06d}",
                "domain": "test.local",
                "version": "1.0.0",
                "site_code": "CONCURRENT-TEST"
            }
            
            response = await backend_client.post("/api/v1/agents/register", json=registration_data)
            assert response.status_code == 200
            
            result = response.json()
            agents.append({
                "guid": f"concurrent-test-guid-{i}",
                "token": result["device_token"]
            })
        
        # Send heartbeats from all agents concurrently
        heartbeat_tasks = []
        for agent in agents:
            heartbeat_data = {
                "agent_guid": agent["guid"],
                "version": "1.0.0",
                "status": "healthy"
            }
            task = backend_client.post("/api/v1/agents/heartbeat", json=heartbeat_data)
            heartbeat_tasks.append(task)
        
        responses = await asyncio.gather(*heartbeat_tasks)
        
        for response in responses:
            assert response.status_code == 200
            result = response.json()
            assert result["status"] == "success"
    
    @pytest.mark.asyncio
    async def test_large_inventory_data(self, backend_client):
        """Test handling of large inventory data"""
        
        # Create large inventory data
        large_inventory = {
            "device_identity": {
                "hostname": "LARGE-DATA-PC",
                "serial_number": "LARGE123456"
            },
            "hardware": {
                "manufacturer": "Dell Inc.",
                "model": "PowerEdge R740"
            },
            "software": {
                "installed_software": [
                    {
                        "name": f"Software {i}",
                        "version": "1.0.0",
                        "publisher": f"Publisher {i}"
                    }
                    for i in range(1000)  # 1000 software entries
                ]
            }
        }
        
        # Register agent
        registration_data = {
            "agent_guid": "large-data-guid",
            "hostname": "LARGE-DATA-PC",
            "serial": "LARGE123456",
            "domain": "test.local",
            "version": "1.0.0",
            "site_code": "LARGE-TEST"
        }
        
        response = await backend_client.post("/api/v1/agents/register", json=registration_data)
        assert response.status_code == 200
        
        # Sync large inventory
        sync_data = {
            "agent_guid": "large-data-guid",
            "sync_type": "full",
            "inventory": large_inventory,
            "last_sync": None
        }
        
        response = await backend_client.post("/api/v1/inventory/full", json=sync_data)
        assert response.status_code == 200
        
        result = response.json()
        assert result["status"] == "success"
