import httpx
import logging
from typing import Dict, Any, Optional, List
from app.core.config import settings

logger = logging.getLogger(__name__)

class SnipeItService:
    def __init__(self):
        self.base_url = settings.SNIPEIT_BASE_URL
        self.api_token = settings.SNIPEIT_API_TOKEN
        self.headers = {
            "Authorization": f"Bearer {self.api_token}",
            "Accept": "application/json",
            "Content-Type": "application/json"
        }

    async def search_hardware(self, serial: str = None, hostname: str = None) -> Optional[Dict[str, Any]]:
        """Search for existing hardware in Snipe-IT"""
        try:
            async with httpx.AsyncClient() as client:
                params = {}
                if serial:
                    params["search"] = serial
                elif hostname:
                    params["search"] = hostname
                
                response = await client.get(
                    f"{self.base_url}/api/v1/hardware",
                    headers=self.headers,
                    params=params,
                    timeout=30.0
                )
                response.raise_for_status()
                
                data = response.json()
                if data.get("total") > 0:
                    return data["rows"][0]  # Return first match
                return None
                
        except Exception as e:
            logger.error(f"Error searching hardware in Snipe-IT: {e}")
            return None

    async def create_hardware(self, hardware_data: Dict[str, Any]) -> Optional[Dict[str, Any]]:
        """Create new hardware in Snipe-IT"""
        try:
            async with httpx.AsyncClient() as client:
                response = await client.post(
                    f"{self.base_url}/api/v1/hardware",
                    headers=self.headers,
                    json=hardware_data,
                    timeout=30.0
                )
                response.raise_for_status()
                
                return response.json()
                
        except Exception as e:
            logger.error(f"Error creating hardware in Snipe-IT: {e}")
            return None

    async def update_hardware(self, hardware_id: int, hardware_data: Dict[str, Any]) -> Optional[Dict[str, Any]]:
        """Update existing hardware in Snipe-IT"""
        try:
            async with httpx.AsyncClient() as client:
                response = await client.patch(
                    f"{self.base_url}/api/v1/hardware/{hardware_id}",
                    headers=self.headers,
                    json=hardware_data,
                    timeout=30.0
                )
                response.raise_for_status()
                
                return response.json()
                
        except Exception as e:
            logger.error(f"Error updating hardware in Snipe-IT: {e}")
            return None

    async def get_models(self) -> List[Dict[str, Any]]:
        """Get available models from Snipe-IT"""
        try:
            async with httpx.AsyncClient() as client:
                response = await client.get(
                    f"{self.base_url}/api/v1/models",
                    headers=self.headers,
                    timeout=30.0
                )
                response.raise_for_status()
                
                data = response.json()
                return data.get("rows", [])
                
        except Exception as e:
            logger.error(f"Error getting models from Snipe-IT: {e}")
            return []

    async def get_categories(self) -> List[Dict[str, Any]]:
        """Get available categories from Snipe-IT"""
        try:
            async with httpx.AsyncClient() as client:
                response = await client.get(
                    f"{self.base_url}/api/v1/categories",
                    headers=self.headers,
                    timeout=30.0
                )
                response.raise_for_status()
                
                data = response.json()
                return data.get("rows", [])
                
        except Exception as e:
            logger.error(f"Error getting categories from Snipe-IT: {e}")
            return []

    async def get_status_labels(self) -> List[Dict[str, Any]]:
        """Get available status labels from Snipe-IT"""
        try:
            async with httpx.AsyncClient() as client:
                response = await client.get(
                    f"{self.base_url}/api/v1/statuslabels",
                    headers=self.headers,
                    timeout=30.0
                )
                response.raise_for_status()
                
                data = response.json()
                return data.get("rows", [])
                
        except Exception as e:
            logger.error(f"Error getting status labels from Snipe-IT: {e}")
            return []

    def map_inventory_to_snipeit(self, inventory_data: Dict[str, Any]) -> Dict[str, Any]:
        """Map inventory data to Snipe-IT hardware format"""
        try:
            device_identity = inventory_data.get("device_identity", {})
            hardware = inventory_data.get("hardware", {})
            software = inventory_data.get("software", {})
            network = inventory_data.get("network", {})
            bios = inventory_data.get("bios", {})
            usage = inventory_data.get("usage", {})
            tagging = inventory_data.get("tagging", {})

            # Basic hardware mapping
            snipeit_data = {
                "name": device_identity.get("hostname", "Unknown"),
                "serial": device_identity.get("serial_number", ""),
                "asset_tag": self._generate_asset_tag(tagging.get("site_code", "UNKNOWN")),
                "notes": f"Managed by MeldenIT Agent\nLast Sync: {inventory_data.get('collected_at', 'Unknown')}",
                "status_id": 1,  # Ready to Deploy (default)
                "model_id": 1,   # Generic Windows Workstation (default)
                "category_id": 1,  # Computer (default)
                "custom_fields": {
                    "cpu": hardware.get("cpu", {}).get("name", ""),
                    "ram": f"{hardware.get('memory', {}).get('total_gb', 0):.1f} GB",
                    "disk": self._get_disk_info(hardware.get("disks", [])),
                    "os_build": f"{software.get('os_name', '')} {software.get('os_version', '')}",
                    "mac_address": self._get_primary_mac(network.get("adapters", [])),
                    "uptime_hours": usage.get("uptime_hours", 0),
                    "location": tagging.get("location", ""),
                    "ou": device_identity.get("ou", ""),
                    "logged_in_user": device_identity.get("logged_in_user", ""),
                    "bios_version": bios.get("version", ""),
                    "tpm_enabled": bios.get("tpm_enabled", False),
                    "secure_boot": bios.get("secure_boot", False)
                }
            }

            return snipeit_data

        except Exception as e:
            logger.error(f"Error mapping inventory to Snipe-IT format: {e}")
            return {}

    def _generate_asset_tag(self, site_code: str) -> str:
        """Generate asset tag in format: SITE-CODE + short GUID"""
        import uuid
        short_guid = str(uuid.uuid4())[:4].upper()
        return f"{site_code}-{short_guid}"

    def _get_disk_info(self, disks: List[Dict[str, Any]]) -> str:
        """Get disk information summary"""
        if not disks:
            return "Unknown"
        
        total_capacity = sum(disk.get("capacity_gb", 0) for disk in disks)
        disk_types = [disk.get("type", "Unknown") for disk in disks]
        
        return f"{total_capacity:.1f} GB ({', '.join(set(disk_types))})"

    def _get_primary_mac(self, adapters: List[Dict[str, Any]]) -> str:
        """Get primary MAC address"""
        for adapter in adapters:
            if adapter.get("is_connected", False):
                return adapter.get("mac_address", "")
        
        # Return first MAC if no connected adapter found
        if adapters:
            return adapters[0].get("mac_address", "")
        
        return ""

    async def sync_inventory_to_snipeit(self, agent_guid: str, inventory_data: Dict[str, Any]) -> bool:
        """Main method to sync inventory data to Snipe-IT"""
        try:
            logger.info(f"Syncing inventory to Snipe-IT for agent {agent_guid}")
            
            device_identity = inventory_data.get("device_identity", {})
            serial = device_identity.get("serial_number", "")
            hostname = device_identity.get("hostname", "")
            
            # Search for existing hardware
            existing_hardware = None
            if serial:
                existing_hardware = await self.search_hardware(serial=serial)
            
            if not existing_hardware and hostname:
                existing_hardware = await self.search_hardware(hostname=hostname)
            
            # Map inventory data to Snipe-IT format
            snipeit_data = self.map_inventory_to_snipeit(inventory_data)
            
            if existing_hardware:
                # Update existing hardware
                hardware_id = existing_hardware.get("id")
                result = await self.update_hardware(hardware_id, snipeit_data)
                if result:
                    logger.info(f"Updated hardware {hardware_id} in Snipe-IT")
                    return True
                else:
                    logger.error(f"Failed to update hardware {hardware_id} in Snipe-IT")
                    return False
            else:
                # Create new hardware
                result = await self.create_hardware(snipeit_data)
                if result:
                    hardware_id = result.get("id")
                    logger.info(f"Created new hardware {hardware_id} in Snipe-IT")
                    return True
                else:
                    logger.error("Failed to create new hardware in Snipe-IT")
                    return False
                    
        except Exception as e:
            logger.error(f"Error syncing inventory to Snipe-IT: {e}")
            return False
