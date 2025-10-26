using MeldenIT.Agent.Core.Models;

namespace MeldenIT.Agent.Core.Services;

public interface IApiClient
{
    Task<AgentRegistrationResponse> RegisterAgentAsync(AgentRegistrationRequest request);
    Task<HeartbeatResponse> SendHeartbeatAsync(HeartbeatRequest request);
    Task<InventorySyncResponse> SyncInventoryAsync(InventorySyncRequest request);
    Task<UpdateCheckResponse> CheckForUpdatesAsync(UpdateCheckRequest request);
    Task<AgentConfig> GetAgentConfigAsync(string agentGuid);
}
