using MeldenIT.Agent.Core.Models;

namespace MeldenIT.Agent.Tray;

public interface ITrayService
{
    Task<bool> SyncNowAsync();
    Task<InventoryData> GetCurrentInventoryAsync();
    Task<AgentConfig> GetConfigAsync();
    string GetAgentGuid();
    DateTime? GetLastSyncTime();
    string GetStatus();
    string GetHostname();
    string GetLoggedInUser();
    Task ShowNotificationAsync(string title, string message, bool isError = false);
}
