using MeldenIT.Agent.Core.Models;
using MeldenIT.Agent.Core.Services;

namespace MeldenIT.Agent.Tray;

public class TrayService : ITrayService
{
    private readonly ILogger<TrayService> _logger;
    private readonly IConfigManager _configManager;
    private readonly IApiClient _apiClient;
    private readonly IInventoryCollector _inventoryCollector;
    private readonly string _agentGuid;
    private DateTime? _lastSyncTime;
    private string _status = "Initializing";

    public TrayService(
        ILogger<TrayService> logger,
        IConfigManager configManager,
        IApiClient apiClient,
        IInventoryCollector inventoryCollector)
    {
        _logger = logger;
        _configManager = configManager;
        _apiClient = apiClient;
        _inventoryCollector = inventoryCollector;
        _agentGuid = _configManager.GetOrCreateAgentGuidAsync().Result;
    }

    public async Task<bool> SyncNowAsync()
    {
        _logger.LogInformation("Manual sync requested from tray");

        try
        {
            _status = "Syncing";

            var config = await _configManager.LoadConfigAsync();
            _apiClient.SetBaseUrl(config.ApiUrl);
            _apiClient.SetAuthenticationToken(config.DeviceToken ?? string.Empty);

            var inventory = await _inventoryCollector.CollectFullInventoryAsync();

            var syncRequest = new InventorySyncRequest
            {
                AgentGuid = _agentGuid,
                SyncType = "full",
                Inventory = inventory,
                LastSync = _lastSyncTime
            };

            var response = await _apiClient.SyncInventoryAsync(syncRequest);
            
            if (response.SnipeItUpdated)
            {
                _lastSyncTime = DateTime.UtcNow;
                _status = "Synced";
                _logger.LogInformation("Manual sync completed successfully");
                await ShowNotificationAsync("Sync Complete", "Inventory synchronized successfully");
                return true;
            }
            else
            {
                _logger.LogWarning("Manual sync completed but Snipe-IT was not updated");
                await ShowNotificationAsync("Sync Warning", "Sync completed but Snipe-IT was not updated", true);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during manual sync");
            _status = "Sync Error";
            await ShowNotificationAsync("Sync Error", $"Sync failed: {ex.Message}", true);
            return false;
        }
    }

    public async Task<InventoryData> GetCurrentInventoryAsync()
    {
        try
        {
            return await _inventoryCollector.CollectFullInventoryAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error collecting current inventory");
            return new InventoryData();
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

    public string GetHostname()
    {
        return Environment.MachineName;
    }

    public string GetLoggedInUser()
    {
        return Environment.UserName;
    }

    public async Task ShowNotificationAsync(string title, string message, bool isError = false)
    {
        try
        {
            // This would be implemented with Windows toast notifications
            // For now, we'll just log the notification
            if (isError)
            {
                _logger.LogError("Notification: {Title} - {Message}", title, message);
            }
            else
            {
                _logger.LogInformation("Notification: {Title} - {Message}", title, message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error showing notification");
        }
    }
}
