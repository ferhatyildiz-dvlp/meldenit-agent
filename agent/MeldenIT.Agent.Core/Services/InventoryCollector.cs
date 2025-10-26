using System.Management;
using System.Net.NetworkInformation;
using System.Security.Principal;
using Microsoft.Win32;
using MeldenIT.Agent.Core.Models;

namespace MeldenIT.Agent.Core.Services;

public class InventoryCollector : IInventoryCollector
{
    private readonly ILogger<InventoryCollector> _logger;

    public InventoryCollector(ILogger<InventoryCollector> logger)
    {
        _logger = logger;
    }

    public async Task<InventoryData> CollectFullInventoryAsync()
    {
        _logger.LogInformation("Starting full inventory collection");

        var inventory = new InventoryData
        {
            DeviceIdentity = await GetDeviceIdentityAsync(),
            Hardware = await CollectHardwareInfoAsync(),
            Software = await CollectSoftwareInfoAsync(),
            Network = await CollectNetworkInfoAsync(),
            Bios = await CollectBiosInfoAsync(),
            Usage = await CollectUsageInfoAsync(),
            Tagging = await CollectTaggingInfoAsync()
        };

        _logger.LogInformation("Full inventory collection completed");
        return inventory;
    }

    public async Task<InventoryData> CollectDeltaInventoryAsync(DateTime lastSync)
    {
        _logger.LogInformation("Starting delta inventory collection since {LastSync}", lastSync);

        // For delta sync, we collect everything but mark what has changed
        var inventory = await CollectFullInventoryAsync();
        
        // In a real implementation, you would compare with previous data
        // and only include changed items. For now, we return full data.
        
        _logger.LogInformation("Delta inventory collection completed");
        return inventory;
    }

    public async Task<DeviceIdentity> GetDeviceIdentityAsync()
    {
        _logger.LogInformation("Collecting device identity information");

        var identity = new DeviceIdentity();

        try
        {
            // Get hostname and domain
            identity.Hostname = Environment.MachineName;
            identity.Domain = Environment.UserDomainName;

            // Get logged in user
            identity.LoggedInUser = Environment.UserName;

            // Get UUID from WMI
            using var searcher = new ManagementObjectSearcher("SELECT UUID FROM Win32_ComputerSystemProduct");
            foreach (ManagementObject obj in searcher.Get())
            {
                identity.Uuid = obj["UUID"]?.ToString() ?? string.Empty;
                break;
            }

            // Get serial number from BIOS
            using var biosSearcher = new ManagementObjectSearcher("SELECT SerialNumber FROM Win32_BIOS");
            foreach (ManagementObject obj in biosSearcher.Get())
            {
                identity.SerialNumber = obj["SerialNumber"]?.ToString() ?? string.Empty;
                break;
            }

            // Get SID
            var currentUser = WindowsIdentity.GetCurrent();
            identity.Sid = currentUser.User?.Value;

            // Get OU from registry (if available)
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Group Policy\History");
                if (key != null)
                {
                    var ouValue = key.GetValue("OU");
                    if (ouValue != null)
                    {
                        identity.Ou = ouValue.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not retrieve OU information");
            }

            // Get Asset Tag from WMI
            using var systemSearcher = new ManagementObjectSearcher("SELECT Tag FROM Win32_SystemEnclosure");
            foreach (ManagementObject obj in systemSearcher.Get())
            {
                identity.AssetTag = obj["Tag"]?.ToString();
                break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error collecting device identity");
        }

        return identity;
    }

    private async Task<HardwareInfo> CollectHardwareInfoAsync()
    {
        _logger.LogInformation("Collecting hardware information");

        var hardware = new HardwareInfo();

        try
        {
            // Get manufacturer and model
            using var searcher = new ManagementObjectSearcher("SELECT Manufacturer, Model FROM Win32_ComputerSystem");
            foreach (ManagementObject obj in searcher.Get())
            {
                hardware.Manufacturer = obj["Manufacturer"]?.ToString() ?? string.Empty;
                hardware.Model = obj["Model"]?.ToString() ?? string.Empty;
                break;
            }

            // Get CPU info
            hardware.Cpu = await CollectCpuInfoAsync();

            // Get memory info
            hardware.Memory = await CollectMemoryInfoAsync();

            // Get disk info
            hardware.Disks = await CollectDiskInfoAsync();

            // Get GPU info
            hardware.Gpu = await CollectGpuInfoAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error collecting hardware information");
        }

        return hardware;
    }

    private async Task<CpuInfo> CollectCpuInfoAsync()
    {
        var cpu = new CpuInfo();

        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT Name, NumberOfCores, NumberOfLogicalProcessors, MaxClockSpeed FROM Win32_Processor");
            foreach (ManagementObject obj in searcher.Get())
            {
                cpu.Name = obj["Name"]?.ToString() ?? string.Empty;
                cpu.Cores = Convert.ToInt32(obj["NumberOfCores"] ?? 0);
                cpu.LogicalProcessors = Convert.ToInt32(obj["NumberOfLogicalProcessors"] ?? 0);
                cpu.MaxClockSpeed = Convert.ToUInt32(obj["MaxClockSpeed"] ?? 0);
                break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error collecting CPU information");
        }

        return cpu;
    }

    private async Task<MemoryInfo> CollectMemoryInfoAsync()
    {
        var memory = new MemoryInfo();

        try
        {
            // Get total memory
            using var searcher = new ManagementObjectSearcher("SELECT TotalPhysicalMemory FROM Win32_ComputerSystem");
            foreach (ManagementObject obj in searcher.Get())
            {
                var totalBytes = Convert.ToUInt64(obj["TotalPhysicalMemory"] ?? 0);
                memory.TotalGb = totalBytes / (1024.0 * 1024.0 * 1024.0);
                break;
            }

            // Get memory slots
            using var slotSearcher = new ManagementObjectSearcher("SELECT Capacity, Speed, Manufacturer, PartNumber FROM Win32_PhysicalMemory");
            foreach (ManagementObject obj in slotSearcher.Get())
            {
                var slot = new MemorySlot
                {
                    CapacityGb = Convert.ToUInt64(obj["Capacity"] ?? 0) / (1024.0 * 1024.0 * 1024.0),
                    SpeedMhz = Convert.ToUInt32(obj["Speed"] ?? 0),
                    Manufacturer = obj["Manufacturer"]?.ToString(),
                    PartNumber = obj["PartNumber"]?.ToString()
                };
                memory.Slots.Add(slot);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error collecting memory information");
        }

        return memory;
    }

    private async Task<List<DiskInfo>> CollectDiskInfoAsync()
    {
        var disks = new List<DiskInfo>();

        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT Model, Size, MediaType FROM Win32_DiskDrive");
            foreach (ManagementObject obj in searcher.Get())
            {
                var disk = new DiskInfo
                {
                    Model = obj["Model"]?.ToString() ?? string.Empty,
                    CapacityGb = Convert.ToUInt64(obj["Size"] ?? 0) / (1024.0 * 1024.0 * 1024.0),
                    Type = obj["MediaType"]?.ToString() ?? "Unknown"
                };

                // Get partitions for this disk
                disk.Partitions = await GetDiskPartitionsAsync(disk.Model);
                disks.Add(disk);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error collecting disk information");
        }

        return disks;
    }

    private async Task<List<PartitionInfo>> GetDiskPartitionsAsync(string diskModel)
    {
        var partitions = new List<PartitionInfo>();

        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT DeviceID, Size, FreeSpace, FileSystem FROM Win32_LogicalDisk");
            foreach (ManagementObject obj in searcher.Get())
            {
                var partition = new PartitionInfo
                {
                    DriveLetter = obj["DeviceID"]?.ToString(),
                    SizeGb = Convert.ToUInt64(obj["Size"] ?? 0) / (1024.0 * 1024.0 * 1024.0),
                    FreeSpaceGb = Convert.ToUInt64(obj["FreeSpace"] ?? 0) / (1024.0 * 1024.0 * 1024.0),
                    FileSystem = obj["FileSystem"]?.ToString() ?? string.Empty
                };
                partitions.Add(partition);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error collecting partition information for disk {DiskModel}", diskModel);
        }

        return partitions;
    }

    private async Task<GpuInfo?> CollectGpuInfoAsync()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT Name, AdapterRAM, DriverVersion FROM Win32_VideoController");
            foreach (ManagementObject obj in searcher.Get())
            {
                var name = obj["Name"]?.ToString();
                if (!string.IsNullOrEmpty(name) && !name.Contains("Microsoft Basic"))
                {
                    return new GpuInfo
                    {
                        Name = name,
                        MemoryMb = Convert.ToUInt32(obj["AdapterRAM"] ?? 0) / (1024 * 1024),
                        DriverVersion = obj["DriverVersion"]?.ToString()
                    };
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error collecting GPU information");
        }

        return null;
    }

    private async Task<SoftwareInfo> CollectSoftwareInfoAsync()
    {
        _logger.LogInformation("Collecting software information");

        var software = new SoftwareInfo();

        try
        {
            // Get OS information
            software.OsName = Environment.OSVersion.VersionString;
            software.OsVersion = Environment.OSVersion.Version.ToString();
            software.OsBuild = Environment.OSVersion.Version.Build.ToString();

            // Get installed software
            software.InstalledSoftware = await GetInstalledSoftwareAsync();

            // Get .NET versions
            software.DotNetVersions = await GetDotNetVersionsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error collecting software information");
        }

        return software;
    }

    private async Task<List<InstalledSoftware>> GetInstalledSoftwareAsync()
    {
        var software = new List<InstalledSoftware>();

        try
        {
            // Get software from registry
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall");
            if (key != null)
            {
                foreach (string subKeyName in key.GetSubKeyNames())
                {
                    using var subKey = key.OpenSubKey(subKeyName);
                    if (subKey != null)
                    {
                        var displayName = subKey.GetValue("DisplayName")?.ToString();
                        var version = subKey.GetValue("DisplayVersion")?.ToString();
                        var publisher = subKey.GetValue("Publisher")?.ToString();
                        var installDate = subKey.GetValue("InstallDate")?.ToString();

                        if (!string.IsNullOrEmpty(displayName))
                        {
                            software.Add(new InstalledSoftware
                            {
                                Name = displayName,
                                Version = version ?? string.Empty,
                                Publisher = publisher,
                                InstallDate = ParseInstallDate(installDate)
                            });
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error collecting installed software");
        }

        return software;
    }

    private DateTime? ParseInstallDate(string? installDate)
    {
        if (string.IsNullOrEmpty(installDate))
            return null;

        if (DateTime.TryParseExact(installDate, "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out var date))
            return date;

        return null;
    }

    private async Task<List<string>> GetDotNetVersionsAsync()
    {
        var versions = new List<string>();

        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\NET Framework Setup\NDP");
            if (key != null)
            {
                foreach (string subKeyName in key.GetSubKeyNames())
                {
                    if (subKeyName.StartsWith("v"))
                    {
                        versions.Add(subKeyName);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error collecting .NET versions");
        }

        return versions;
    }

    private async Task<NetworkInfo> CollectNetworkInfoAsync()
    {
        _logger.LogInformation("Collecting network information");

        var network = new NetworkInfo();

        try
        {
            // Get network adapters
            var adapters = NetworkInterface.GetAllNetworkInterfaces();
            foreach (var adapter in adapters)
            {
                var networkAdapter = new NetworkAdapter
                {
                    Name = adapter.Name,
                    MacAddress = adapter.GetPhysicalAddress().ToString(),
                    ConnectionType = adapter.NetworkInterfaceType.ToString(),
                    IsConnected = adapter.OperationalStatus == OperationalStatus.Up
                };

                network.Adapters.Add(networkAdapter);

                // Get IP addresses
                var ipProps = adapter.GetIPProperties();
                foreach (var addr in ipProps.UnicastAddresses)
                {
                    if (addr.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    {
                        network.Ipv4Addresses.Add(addr.Address.ToString());
                    }
                    else if (addr.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
                    {
                        network.Ipv6Addresses.Add(addr.Address.ToString());
                    }
                }

                // Get gateway
                foreach (var gateway in ipProps.GatewayAddresses)
                {
                    network.Gateway = gateway.Address.ToString();
                    break;
                }

                // Get DNS servers
                foreach (var dns in ipProps.DnsAddresses)
                {
                    network.DnsServers.Add(dns.ToString());
                }
            }

            // Get WiFi SSID (if available)
            network.WifiSsid = await GetWifiSsidAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error collecting network information");
        }

        return network;
    }

    private async Task<string?> GetWifiSsidAsync()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT SSID FROM Win32_NetworkAdapterConfiguration WHERE IPEnabled = True");
            foreach (ManagementObject obj in searcher.Get())
            {
                var ssid = obj["SSID"]?.ToString();
                if (!string.IsNullOrEmpty(ssid))
                {
                    return ssid;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not retrieve WiFi SSID");
        }

        return null;
    }

    private async Task<BiosInfo> CollectBiosInfoAsync()
    {
        _logger.LogInformation("Collecting BIOS information");

        var bios = new BiosInfo();

        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT Name, Version, ReleaseDate FROM Win32_BIOS");
            foreach (ManagementObject obj in searcher.Get())
            {
                bios.Name = obj["Name"]?.ToString() ?? string.Empty;
                bios.Version = obj["Version"]?.ToString() ?? string.Empty;
                
                var releaseDate = obj["ReleaseDate"]?.ToString();
                if (!string.IsNullOrEmpty(releaseDate))
                {
                    if (DateTime.TryParseExact(releaseDate, "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out var date))
                    {
                        bios.ReleaseDate = date;
                    }
                }
                break;
            }

            // Get TPM status
            using var tpmSearcher = new ManagementObjectSearcher("SELECT IsEnabled_InitialValue FROM Win32_Tpm");
            foreach (ManagementObject obj in tpmSearcher.Get())
            {
                bios.TpmEnabled = Convert.ToBoolean(obj["IsEnabled_InitialValue"] ?? false);
                break;
            }

            // Get Secure Boot status
            using var secureBootSearcher = new ManagementObjectSearcher("SELECT SecureBootEnabled FROM Win32_SystemEnclosure");
            foreach (ManagementObject obj in secureBootSearcher.Get())
            {
                bios.SecureBoot = Convert.ToBoolean(obj["SecureBootEnabled"] ?? false);
                break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error collecting BIOS information");
        }

        return bios;
    }

    private async Task<UsageInfo> CollectUsageInfoAsync()
    {
        _logger.LogInformation("Collecting usage information");

        var usage = new UsageInfo();

        try
        {
            // Get uptime
            using var searcher = new ManagementObjectSearcher("SELECT LastBootUpTime FROM Win32_OperatingSystem");
            foreach (ManagementObject obj in searcher.Get())
            {
                var lastBootTime = ManagementDateTimeConverter.ToDateTime(obj["LastBootUpTime"]?.ToString() ?? string.Empty);
                usage.LastReboot = lastBootTime;
                usage.UptimeHours = (DateTime.Now - lastBootTime).TotalHours;
                break;
            }

            // Get performance counters (simplified)
            usage.CpuUsageAvg = await GetAverageCpuUsageAsync();
            usage.MemoryUsageAvg = await GetAverageMemoryUsageAsync();
            usage.DiskIoAvg = await GetAverageDiskIoAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error collecting usage information");
        }

        return usage;
    }

    private async Task<double> GetAverageCpuUsageAsync()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT LoadPercentage FROM Win32_Processor");
            foreach (ManagementObject obj in searcher.Get())
            {
                return Convert.ToDouble(obj["LoadPercentage"] ?? 0);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not retrieve CPU usage");
        }

        return 0;
    }

    private async Task<double> GetAverageMemoryUsageAsync()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT TotalVisibleMemorySize, FreePhysicalMemory FROM Win32_OperatingSystem");
            foreach (ManagementObject obj in searcher.Get())
            {
                var total = Convert.ToUInt64(obj["TotalVisibleMemorySize"] ?? 0);
                var free = Convert.ToUInt64(obj["FreePhysicalMemory"] ?? 0);
                var used = total - free;
                return (double)used / total * 100;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not retrieve memory usage");
        }

        return 0;
    }

    private async Task<double> GetAverageDiskIoAsync()
    {
        // Simplified implementation - in real scenario, you'd use PerformanceCounters
        return 0;
    }

    private async Task<TaggingInfo> CollectTaggingInfoAsync()
    {
        _logger.LogInformation("Collecting tagging information");

        var tagging = new TaggingInfo();

        try
        {
            // Get site code from registry or environment
            tagging.SiteCode = Environment.GetEnvironmentVariable("MELDENIT_SITE_CODE") ?? "UNKNOWN";

            // Get location from registry
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\MeldenIT\Agent");
                if (key != null)
                {
                    tagging.Location = key.GetValue("Location")?.ToString();
                    tagging.Department = key.GetValue("Department")?.ToString();
                    tagging.CostCenter = key.GetValue("CostCenter")?.ToString();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not retrieve tagging information from registry");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error collecting tagging information");
        }

        return tagging;
    }
}
