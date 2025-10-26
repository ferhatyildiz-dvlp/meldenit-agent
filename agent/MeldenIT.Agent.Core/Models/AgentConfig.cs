using System.Text.Json.Serialization;

namespace MeldenIT.Agent.Core.Models;

public class AgentConfig
{
    [JsonPropertyName("api_url")]
    public string ApiUrl { get; set; } = string.Empty;

    [JsonPropertyName("snipeit_url")]
    public string SnipeItUrl { get; set; } = string.Empty;

    [JsonPropertyName("site_code")]
    public string SiteCode { get; set; } = string.Empty;

    [JsonPropertyName("heartbeat_interval")]
    public int HeartbeatIntervalMinutes { get; set; } = 15;

    [JsonPropertyName("delta_sync_interval")]
    public int DeltaSyncIntervalMinutes { get; set; } = 360; // 6 hours

    [JsonPropertyName("full_sync_time")]
    public string FullSyncTime { get; set; } = "03:00";

    [JsonPropertyName("device_token")]
    public string? DeviceToken { get; set; }

    [JsonPropertyName("agent_guid")]
    public string? AgentGuid { get; set; }

    [JsonPropertyName("proxy_enabled")]
    public bool ProxyEnabled { get; set; } = false;

    [JsonPropertyName("proxy_url")]
    public string? ProxyUrl { get; set; }

    [JsonPropertyName("log_level")]
    public string LogLevel { get; set; } = "Information";

    [JsonPropertyName("max_retry_attempts")]
    public int MaxRetryAttempts { get; set; } = 3;

    [JsonPropertyName("retry_delay_seconds")]
    public int RetryDelaySeconds { get; set; } = 30;
}
