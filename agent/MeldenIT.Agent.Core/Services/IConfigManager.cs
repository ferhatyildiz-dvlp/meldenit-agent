using MeldenIT.Agent.Core.Models;

namespace MeldenIT.Agent.Core.Services;

public interface IConfigManager
{
    Task<AgentConfig> LoadConfigAsync();
    Task SaveConfigAsync(AgentConfig config);
    Task<string> GetOrCreateAgentGuidAsync();
    Task<string?> GetDeviceTokenAsync();
    Task SaveDeviceTokenAsync(string token);
    Task<bool> IsFirstRunAsync();
}
