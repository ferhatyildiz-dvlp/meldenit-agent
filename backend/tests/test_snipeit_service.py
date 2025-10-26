import pytest
from unittest.mock import AsyncMock, patch
import httpx
from app.services.snipeit_service import SnipeItService

@pytest.fixture
def snipeit_service():
    with patch('app.services.snipeit_service.settings') as mock_settings:
        mock_settings.SNIPEIT_BASE_URL = "https://test.snipeit.com"
        mock_settings.SNIPEIT_API_TOKEN = "test-token"
        yield SnipeItService()

@pytest.fixture
def sample_inventory_data():
    return {
        "device_identity": {
            "hostname": "TEST-PC",
            "serial_number": "ABC123456",
            "domain": "test.local",
            "ou": "OU=Computers,DC=test,DC=local"
        },
        "hardware": {
            "manufacturer": "Dell Inc.",
            "model": "OptiPlex 7090",
            "cpu": {
                "name": "Intel Core i7-11700",
                "cores": 8,
                "logical_processors": 16
            },
            "memory": {
                "total_gb": 16.0,
                "slots": [
                    {"capacity_gb": 8.0, "speed_mhz": 3200},
                    {"capacity_gb": 8.0, "speed_mhz": 3200}
                ]
            },
            "disks": [
                {
                    "model": "Samsung SSD 980 PRO",
                    "capacity_gb": 500.0,
                    "type": "SSD"
                }
            ]
        },
        "software": {
            "os_name": "Microsoft Windows 11 Pro",
            "os_version": "10.0.22621",
            "os_build": "22621"
        },
        "network": {
            "adapters": [
                {
                    "name": "Ethernet",
                    "mac_address": "00:11:22:33:44:55",
                    "is_connected": True
                }
            ]
        },
        "bios": {
            "name": "Dell Inc.",
            "version": "1.0.0",
            "tpm_enabled": True,
            "secure_boot": True
        },
        "usage": {
            "uptime_hours": 72.5
        },
        "tagging": {
            "site_code": "TEST",
            "location": "Office Building A"
        }
    }

class TestSnipeItService:
    
    @pytest.mark.asyncio
    async def test_search_hardware_by_serial(self, snipeit_service):
        """Test searching hardware by serial number"""
        mock_response = {
            "total": 1,
            "rows": [{
                "id": 123,
                "name": "TEST-PC",
                "serial": "ABC123456",
                "asset_tag": "TEST-1234"
            }]
        }
        
        with patch('httpx.AsyncClient') as mock_client:
            mock_client.return_value.__aenter__.return_value.get.return_value.json.return_value = mock_response
            mock_client.return_value.__aenter__.return_value.get.return_value.raise_for_status.return_value = None
            
            result = await snipeit_service.search_hardware(serial="ABC123456")
            
            assert result is not None
            assert result["id"] == 123
            assert result["serial"] == "ABC123456"
    
    @pytest.mark.asyncio
    async def test_search_hardware_by_hostname(self, snipeit_service):
        """Test searching hardware by hostname"""
        mock_response = {
            "total": 1,
            "rows": [{
                "id": 123,
                "name": "TEST-PC",
                "serial": "ABC123456"
            }]
        }
        
        with patch('httpx.AsyncClient') as mock_client:
            mock_client.return_value.__aenter__.return_value.get.return_value.json.return_value = mock_response
            mock_client.return_value.__aenter__.return_value.get.return_value.raise_for_status.return_value = None
            
            result = await snipeit_service.search_hardware(hostname="TEST-PC")
            
            assert result is not None
            assert result["id"] == 123
    
    @pytest.mark.asyncio
    async def test_search_hardware_not_found(self, snipeit_service):
        """Test searching hardware when not found"""
        mock_response = {
            "total": 0,
            "rows": []
        }
        
        with patch('httpx.AsyncClient') as mock_client:
            mock_client.return_value.__aenter__.return_value.get.return_value.json.return_value = mock_response
            mock_client.return_value.__aenter__.return_value.get.return_value.raise_for_status.return_value = None
            
            result = await snipeit_service.search_hardware(serial="NOTFOUND")
            
            assert result is None
    
    @pytest.mark.asyncio
    async def test_create_hardware(self, snipeit_service):
        """Test creating new hardware"""
        hardware_data = {
            "name": "TEST-PC",
            "serial": "ABC123456",
            "asset_tag": "TEST-1234"
        }
        
        mock_response = {
            "id": 123,
            "name": "TEST-PC",
            "serial": "ABC123456"
        }
        
        with patch('httpx.AsyncClient') as mock_client:
            mock_client.return_value.__aenter__.return_value.post.return_value.json.return_value = mock_response
            mock_client.return_value.__aenter__.return_value.post.return_value.raise_for_status.return_value = None
            
            result = await snipeit_service.create_hardware(hardware_data)
            
            assert result is not None
            assert result["id"] == 123
    
    @pytest.mark.asyncio
    async def test_update_hardware(self, snipeit_service):
        """Test updating existing hardware"""
        hardware_data = {
            "name": "TEST-PC-UPDATED",
            "serial": "ABC123456"
        }
        
        mock_response = {
            "id": 123,
            "name": "TEST-PC-UPDATED",
            "serial": "ABC123456"
        }
        
        with patch('httpx.AsyncClient') as mock_client:
            mock_client.return_value.__aenter__.return_value.patch.return_value.json.return_value = mock_response
            mock_client.return_value.__aenter__.return_value.patch.return_value.raise_for_status.return_value = None
            
            result = await snipeit_service.update_hardware(123, hardware_data)
            
            assert result is not None
            assert result["id"] == 123
    
    def test_map_inventory_to_snipeit(self, snipeit_service, sample_inventory_data):
        """Test mapping inventory data to Snipe-IT format"""
        result = snipeit_service.map_inventory_to_snipeit(sample_inventory_data)
        
        assert result["name"] == "TEST-PC"
        assert result["serial"] == "ABC123456"
        assert result["asset_tag"].startswith("TEST-")
        assert "Managed by MeldenIT Agent" in result["notes"]
        assert result["status_id"] == 1
        assert result["model_id"] == 1
        assert result["category_id"] == 1
        
        # Check custom fields
        custom_fields = result["custom_fields"]
        assert custom_fields["cpu"] == "Intel Core i7-11700"
        assert custom_fields["ram"] == "16.0 GB"
        assert "500.0 GB" in custom_fields["disk"]
        assert "Microsoft Windows 11 Pro" in custom_fields["os_build"]
        assert custom_fields["mac_address"] == "00:11:22:33:44:55"
        assert custom_fields["uptime_hours"] == 72.5
        assert custom_fields["location"] == "Office Building A"
        assert custom_fields["tpm_enabled"] is True
        assert custom_fields["secure_boot"] is True
    
    def test_generate_asset_tag(self, snipeit_service):
        """Test asset tag generation"""
        asset_tag = snipeit_service._generate_asset_tag("TEST")
        
        assert asset_tag.startswith("TEST-")
        assert len(asset_tag) == 9  # TEST- + 4 chars
    
    def test_get_disk_info(self, snipeit_service):
        """Test disk information summary"""
        disks = [
            {"capacity_gb": 500.0, "type": "SSD"},
            {"capacity_gb": 1000.0, "type": "HDD"}
        ]
        
        result = snipeit_service._get_disk_info(disks)
        
        assert "1500.0 GB" in result
        assert "SSD" in result
        assert "HDD" in result
    
    def test_get_primary_mac(self, snipeit_service):
        """Test primary MAC address selection"""
        adapters = [
            {"mac_address": "00:11:22:33:44:55", "is_connected": True},
            {"mac_address": "00:11:22:33:44:66", "is_connected": False}
        ]
        
        result = snipeit_service._get_primary_mac(adapters)
        
        assert result == "00:11:22:33:44:55"
    
    @pytest.mark.asyncio
    async def test_sync_inventory_to_snipeit_create_new(self, snipeit_service, sample_inventory_data):
        """Test syncing inventory when creating new hardware"""
        # Mock search to return no existing hardware
        snipeit_service.search_hardware = AsyncMock(return_value=None)
        snipeit_service.create_hardware = AsyncMock(return_value={"id": 123})
        snipeit_service.map_inventory_to_snipeit = Mock(return_value={"name": "TEST-PC"})
        
        result = await snipeit_service.sync_inventory_to_snipeit("test-guid", sample_inventory_data)
        
        assert result is True
        snipeit_service.search_hardware.assert_called_once()
        snipeit_service.create_hardware.assert_called_once()
    
    @pytest.mark.asyncio
    async def test_sync_inventory_to_snipeit_update_existing(self, snipeit_service, sample_inventory_data):
        """Test syncing inventory when updating existing hardware"""
        # Mock search to return existing hardware
        existing_hardware = {"id": 123, "name": "TEST-PC"}
        snipeit_service.search_hardware = AsyncMock(return_value=existing_hardware)
        snipeit_service.update_hardware = AsyncMock(return_value={"id": 123})
        snipeit_service.map_inventory_to_snipeit = Mock(return_value={"name": "TEST-PC"})
        
        result = await snipeit_service.sync_inventory_to_snipeit("test-guid", sample_inventory_data)
        
        assert result is True
        snipeit_service.search_hardware.assert_called_once()
        snipeit_service.update_hardware.assert_called_once_with(123, {"name": "TEST-PC"})
    
    @pytest.mark.asyncio
    async def test_sync_inventory_to_snipeit_error(self, snipeit_service, sample_inventory_data):
        """Test syncing inventory when error occurs"""
        # Mock search to raise exception
        snipeit_service.search_hardware = AsyncMock(side_effect=Exception("API Error"))
        
        result = await snipeit_service.sync_inventory_to_snipeit("test-guid", sample_inventory_data)
        
        assert result is False
