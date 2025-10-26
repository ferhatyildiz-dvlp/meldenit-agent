using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using MeldenIT.Agent.Core.Models;

namespace MeldenIT.Agent.Core.Services;

public class ApiClient : IApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ApiClient> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public ApiClient(HttpClient httpClient, ILogger<ApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    public async Task<AgentRegistrationResponse> RegisterAgentAsync(AgentRegistrationRequest request)
    {
        _logger.LogInformation("Registering agent with GUID {AgentGuid}", request.AgentGuid);

        try
        {
            var json = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/api/agents/register", content);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<AgentRegistrationResponse>(responseJson, _jsonOptions);

            _logger.LogInformation("Agent registration successful");
            return result ?? new AgentRegistrationResponse();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error during agent registration");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during agent registration");
            throw;
        }
    }

    public async Task<HeartbeatResponse> SendHeartbeatAsync(HeartbeatRequest request)
    {
        _logger.LogDebug("Sending heartbeat for agent {AgentGuid}", request.AgentGuid);

        try
        {
            var json = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/api/agents/heartbeat", content);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<HeartbeatResponse>(responseJson, _jsonOptions);

            _logger.LogDebug("Heartbeat sent successfully");
            return result ?? new HeartbeatResponse();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error during heartbeat");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during heartbeat");
            throw;
        }
    }

    public async Task<InventorySyncResponse> SyncInventoryAsync(InventorySyncRequest request)
    {
        _logger.LogInformation("Syncing {SyncType} inventory for agent {AgentGuid}", request.SyncType, request.AgentGuid);

        try
        {
            var json = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var endpoint = request.SyncType == "delta" ? "/api/inventory/delta" : "/api/inventory/full";
            var response = await _httpClient.PostAsync(endpoint, content);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<InventorySyncResponse>(responseJson, _jsonOptions);

            _logger.LogInformation("Inventory sync completed successfully");
            return result ?? new InventorySyncResponse();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error during inventory sync");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during inventory sync");
            throw;
        }
    }

    public async Task<UpdateCheckResponse> CheckForUpdatesAsync(UpdateCheckRequest request)
    {
        _logger.LogDebug("Checking for updates for agent {AgentGuid}", request.AgentGuid);

        try
        {
            var json = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/api/update/check", content);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<UpdateCheckResponse>(responseJson, _jsonOptions);

            _logger.LogDebug("Update check completed");
            return result ?? new UpdateCheckResponse();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error during update check");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during update check");
            throw;
        }
    }

    public async Task<AgentConfig> GetAgentConfigAsync(string agentGuid)
    {
        _logger.LogDebug("Getting configuration for agent {AgentGuid}", agentGuid);

        try
        {
            var response = await _httpClient.GetAsync($"/api/agents/{agentGuid}/config");
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<AgentConfig>(responseJson, _jsonOptions);

            _logger.LogDebug("Configuration retrieved successfully");
            return result ?? new AgentConfig();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error during config retrieval");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during config retrieval");
            throw;
        }
    }

    public void SetAuthenticationToken(string token)
    {
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        _logger.LogDebug("Authentication token set");
    }

    public void SetBaseUrl(string baseUrl)
    {
        _httpClient.BaseAddress = new Uri(baseUrl);
        _logger.LogDebug("Base URL set to {BaseUrl}", baseUrl);
    }
}
