using MeldenIT.Agent.Core.Models;
using MeldenIT.Agent.Core.Services;

namespace MeldenIT.Agent.Service;

public class AgentService : IAgentService
{
    private readonly ILogger<AgentService> _logger;
    private readonly IConfigManager _configManager;
    private readonly IApiClient _apiClient;
    private readonly IInventoryCollector _inventoryCollector;
    private readonly string _agentGuid;
    private readonly string _version;
    private DateTime? _lastSyncTime;
    private string _status = "Initializing";

    public AgentService(
        ILogger<AgentService> logger,
        IConfigManager configManager,
        IApiClient apiClient,
        IInventoryCollector inventoryCollector)
    {
        _logger = logger;
        _configManager = configManager;
        _apiClient = apiClient;
        _inventoryCollector = inventoryCollector;
        _agentGuid = _configManager.GetOrCreateAgentGuidAsync().Result;
        _version = GetAssemblyVersion();
    }

    public async Task<bool> RegisterAsync()
    {
        _logger.LogInformation("Starting agent registration process");

        try
        {
            _status = "Registering";

            var config = await _configManager.LoadConfigAsync();
            _apiClient.SetBaseUrl(config.ApiUrl);

            var deviceIdentity = await _inventoryCollector.GetDeviceIdentityAsync();
            
            var registrationRequest = new AgentRegistrationRequest
            {
                AgentGuid = _agentGuid,
                Hostname = deviceIdentity.Hostname,
                Serial = deviceIdentity.SerialNumber,
                Domain = deviceIdentity.Domain,
                Version = _version,
                SiteCode = config.SiteCode
            };

            var response = await _apiClient.RegisterAgentAsync(registrationRequest);
            
            if (!string.IsNullOrEmpty(response.DeviceToken))
            {
                await _configManager.SaveDeviceTokenAsync(response.DeviceToken);
                _apiClient.SetAuthenticationToken(response.DeviceToken);
                
                // Update config with policy
                config.HeartbeatIntervalMinutes = response.Policy.HeartbeatIntervalMinutes;
                config.DeltaSyncIntervalMinutes = response.Policy.DeltaSyncIntervalMinutes;
                config.FullSyncTime = response.Policy.FullSyncTime;
                config.MaxRetryAttempts = response.Policy.MaxRetryAttempts;
                config.RetryDelaySeconds = response.Policy.RetryDelaySeconds;
                
                await _configManager.SaveConfigAsync(config);
                
                _status = "Registered";
                _logger.LogInformation("Agent registration completed successfully");
                return true;
            }

            _logger.LogError("Registration failed: No device token received");
            _status = "Registration Failed";
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during agent registration");
            _status = "Registration Error";
            return false;
        }
    }

    public async Task<bool> SendHeartbeatAsync()
    {
        _logger.LogDebug("Sending heartbeat");

        try
        {
            _status = "Heartbeat";

            var config = await _configManager.LoadConfigAsync();
            _apiClient.SetBaseUrl(config.ApiUrl);
            _apiClient.SetAuthenticationToken(config.DeviceToken ?? string.Empty);

            var heartbeatRequest = new HeartbeatRequest
            {
                AgentGuid = _agentGuid,
                Version = _version,
                LastSync = _lastSyncTime,
                Status = _status
            };

            var response = await _apiClient.SendHeartbeatAsync(heartbeatRequest);
            
            if (response.ConfigUpdated)
            {
                _logger.LogInformation("Configuration updated, reloading");
                await ReloadConfigAsync();
            }

            _status = "Healthy";
            _logger.LogDebug("Heartbeat sent successfully");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending heartbeat");
            _status = "Heartbeat Error";
            return false;
        }
    }

    public async Task<bool> SyncInventoryAsync(string syncType = "delta")
    {
        _logger.LogInformation("Starting {SyncType} inventory sync", syncType);

        try
        {
            _status = $"Syncing ({syncType})";

            var config = await _configManager.LoadConfigAsync();
            _apiClient.SetBaseUrl(config.ApiUrl);
            _apiClient.SetAuthenticationToken(config.DeviceToken ?? string.Empty);

            InventoryData inventory;
            if (syncType == "delta")
            {
                inventory = await _inventoryCollector.CollectDeltaInventoryAsync(_lastSyncTime ?? DateTime.MinValue);
            }
            else
            {
                inventory = await _inventoryCollector.CollectFullInventoryAsync();
            }

            var syncRequest = new InventorySyncRequest
            {
                AgentGuid = _agentGuid,
                SyncType = syncType,
                Inventory = inventory,
                LastSync = _lastSyncTime
            };

            var response = await _apiClient.SyncInventoryAsync(syncRequest);
            
            if (response.SnipeItUpdated)
            {
                _lastSyncTime = DateTime.UtcNow;
                _logger.LogInformation("Inventory sync completed successfully, Snipe-IT updated");
            }
            else
            {
                _logger.LogWarning("Inventory sync completed but Snipe-IT was not updated");
            }

            _status = "Synced";
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during inventory sync");
            _status = "Sync Error";
            return false;
        }
    }

    public async Task<bool> CheckForUpdatesAsync()
    {
        _logger.LogDebug("Checking for updates");

        try
        {
            var config = await _configManager.LoadConfigAsync();
            _apiClient.SetBaseUrl(config.ApiUrl);
            _apiClient.SetAuthenticationToken(config.DeviceToken ?? string.Empty);

            var updateRequest = new UpdateCheckRequest
            {
                AgentGuid = _agentGuid,
                CurrentVersion = _version
            };

            var response = await _apiClient.CheckForUpdatesAsync(updateRequest);
            
            if (response.UpdateAvailable)
            {
                _logger.LogInformation("Update available: {LatestVersion}", response.LatestVersion);
                // TODO: Implement update download and installation
                return true;
            }

            _logger.LogDebug("No updates available");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking for updates");
            return false;
        }
    }

    public async Task<AgentConfig> GetConfigAsync()
    {
        return await _configManager.LoadConfigAsync();
    }

    public string GetAgentGuid()
    {
        return _agentGuid;
    }

    public DateTime? GetLastSyncTime()
    {
        return _lastSyncTime;
    }

    public string GetStatus()
    {
        return _status;
    }

    private async Task ReloadConfigAsync()
    {
        try
        {
            var config = await _configManager.LoadConfigAsync();
            _apiClient.SetBaseUrl(config.ApiUrl);
            _apiClient.SetAuthenticationToken(config.DeviceToken ?? string.Empty);
            _logger.LogInformation("Configuration reloaded");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reloading configuration");
        }
    }

    private string GetAssemblyVersion()
    {
        try
        {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version;
            return version?.ToString() ?? "1.0.0.0";
        }
        catch
        {
            return "1.0.0.0";
        }
    }
}
