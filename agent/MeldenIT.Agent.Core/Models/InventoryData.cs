using System.Text.Json.Serialization;

namespace MeldenIT.Agent.Core.Models;

public class InventoryData
{
    [JsonPropertyName("device_identity")]
    public DeviceIdentity DeviceIdentity { get; set; } = new();

    [JsonPropertyName("hardware")]
    public HardwareInfo Hardware { get; set; } = new();

    [JsonPropertyName("software")]
    public SoftwareInfo Software { get; set; } = new();

    [JsonPropertyName("network")]
    public NetworkInfo Network { get; set; } = new();

    [JsonPropertyName("bios")]
    public BiosInfo Bios { get; set; } = new();

    [JsonPropertyName("usage")]
    public UsageInfo Usage { get; set; } = new();

    [JsonPropertyName("tagging")]
    public TaggingInfo Tagging { get; set; } = new();

    [JsonPropertyName("collected_at")]
    public DateTime CollectedAt { get; set; } = DateTime.UtcNow;
}

public class DeviceIdentity
{
    [JsonPropertyName("hostname")]
    public string Hostname { get; set; } = string.Empty;

    [JsonPropertyName("domain")]
    public string Domain { get; set; } = string.Empty;

    [JsonPropertyName("ou")]
    public string? Ou { get; set; }

    [JsonPropertyName("sid")]
    public string? Sid { get; set; }

    [JsonPropertyName("uuid")]
    public string Uuid { get; set; } = string.Empty;

    [JsonPropertyName("serial_number")]
    public string SerialNumber { get; set; } = string.Empty;

    [JsonPropertyName("asset_tag")]
    public string? AssetTag { get; set; }

    [JsonPropertyName("logged_in_user")]
    public string? LoggedInUser { get; set; }
}

public class HardwareInfo
{
    [JsonPropertyName("manufacturer")]
    public string Manufacturer { get; set; } = string.Empty;

    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("cpu")]
    public CpuInfo Cpu { get; set; } = new();

    [JsonPropertyName("memory")]
    public MemoryInfo Memory { get; set; } = new();

    [JsonPropertyName("disks")]
    public List<DiskInfo> Disks { get; set; } = new();

    [JsonPropertyName("gpu")]
    public GpuInfo? Gpu { get; set; }
}

public class CpuInfo
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("cores")]
    public int Cores { get; set; }

    [JsonPropertyName("logical_processors")]
    public int LogicalProcessors { get; set; }

    [JsonPropertyName("max_clock_speed")]
    public uint MaxClockSpeed { get; set; }
}

public class MemoryInfo
{
    [JsonPropertyName("total_gb")]
    public double TotalGb { get; set; }

    [JsonPropertyName("slots")]
    public List<MemorySlot> Slots { get; set; } = new();
}

public class MemorySlot
{
    [JsonPropertyName("capacity_gb")]
    public double CapacityGb { get; set; }

    [JsonPropertyName("speed_mhz")]
    public uint SpeedMhz { get; set; }

    [JsonPropertyName("manufacturer")]
    public string? Manufacturer { get; set; }

    [JsonPropertyName("part_number")]
    public string? PartNumber { get; set; }
}

public class DiskInfo
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("capacity_gb")]
    public double CapacityGb { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty; // SSD, HDD, NVMe

    [JsonPropertyName("partitions")]
    public List<PartitionInfo> Partitions { get; set; } = new();
}

public class PartitionInfo
{
    [JsonPropertyName("drive_letter")]
    public string? DriveLetter { get; set; }

    [JsonPropertyName("size_gb")]
    public double SizeGb { get; set; }

    [JsonPropertyName("free_space_gb")]
    public double FreeSpaceGb { get; set; }

    [JsonPropertyName("file_system")]
    public string FileSystem { get; set; } = string.Empty;
}

public class GpuInfo
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("memory_mb")]
    public uint MemoryMb { get; set; }

    [JsonPropertyName("driver_version")]
    public string? DriverVersion { get; set; }
}

public class SoftwareInfo
{
    [JsonPropertyName("os_name")]
    public string OsName { get; set; } = string.Empty;

    [JsonPropertyName("os_version")]
    public string OsVersion { get; set; } = string.Empty;

    [JsonPropertyName("os_build")]
    public string OsBuild { get; set; } = string.Empty;

    [JsonPropertyName("installed_software")]
    public List<InstalledSoftware> InstalledSoftware { get; set; } = new();

    [JsonPropertyName("dotnet_versions")]
    public List<string> DotNetVersions { get; set; } = new();
}

public class InstalledSoftware
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    [JsonPropertyName("publisher")]
    public string? Publisher { get; set; }

    [JsonPropertyName("install_date")]
    public DateTime? InstallDate { get; set; }
}

public class NetworkInfo
{
    [JsonPropertyName("adapters")]
    public List<NetworkAdapter> Adapters { get; set; } = new();

    [JsonPropertyName("ipv4_addresses")]
    public List<string> Ipv4Addresses { get; set; } = new();

    [JsonPropertyName("ipv6_addresses")]
    public List<string> Ipv6Addresses { get; set; } = new();

    [JsonPropertyName("gateway")]
    public string? Gateway { get; set; }

    [JsonPropertyName("dns_servers")]
    public List<string> DnsServers { get; set; } = new();

    [JsonPropertyName("wifi_ssid")]
    public string? WifiSsid { get; set; }
}

public class NetworkAdapter
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("mac_address")]
    public string MacAddress { get; set; } = string.Empty;

    [JsonPropertyName("connection_type")]
    public string ConnectionType { get; set; } = string.Empty; // Ethernet, WiFi, etc.

    [JsonPropertyName("is_connected")]
    public bool IsConnected { get; set; }
}

public class BiosInfo
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    [JsonPropertyName("release_date")]
    public DateTime? ReleaseDate { get; set; }

    [JsonPropertyName("tpm_enabled")]
    public bool TpmEnabled { get; set; }

    [JsonPropertyName("secure_boot")]
    public bool SecureBoot { get; set; }
}

public class UsageInfo
{
    [JsonPropertyName("uptime_hours")]
    public double UptimeHours { get; set; }

    [JsonPropertyName("last_reboot")]
    public DateTime LastReboot { get; set; }

    [JsonPropertyName("cpu_usage_avg")]
    public double CpuUsageAvg { get; set; }

    [JsonPropertyName("memory_usage_avg")]
    public double MemoryUsageAvg { get; set; }

    [JsonPropertyName("disk_io_avg")]
    public double DiskIoAvg { get; set; }
}

public class TaggingInfo
{
    [JsonPropertyName("location")]
    public string? Location { get; set; }

    [JsonPropertyName("site_code")]
    public string SiteCode { get; set; } = string.Empty;

    [JsonPropertyName("department")]
    public string? Department { get; set; }

    [JsonPropertyName("cost_center")]
    public string? CostCenter { get; set; }
}
