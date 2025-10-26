using MeldenIT.Agent.Core.Models;

namespace MeldenIT.Agent.Service;

public interface IAgentService
{
    Task<bool> RegisterAsync();
    Task<bool> SendHeartbeatAsync();
    Task<bool> SyncInventoryAsync(string syncType = "delta");
    Task<bool> CheckForUpdatesAsync();
    Task<AgentConfig> GetConfigAsync();
    string GetAgentGuid();
    DateTime? GetLastSyncTime();
    string GetStatus();
}
