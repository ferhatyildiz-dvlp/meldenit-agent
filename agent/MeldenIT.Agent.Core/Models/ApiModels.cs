using System.Text.Json.Serialization;

namespace MeldenIT.Agent.Core.Models;

public class AgentRegistrationRequest
{
    [JsonPropertyName("agent_guid")]
    public string AgentGuid { get; set; } = string.Empty;

    [JsonPropertyName("hostname")]
    public string Hostname { get; set; } = string.Empty;

    [JsonPropertyName("serial")]
    public string Serial { get; set; } = string.Empty;

    [JsonPropertyName("domain")]
    public string Domain { get; set; } = string.Empty;

    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    [JsonPropertyName("site_code")]
    public string SiteCode { get; set; } = string.Empty;
}

public class AgentRegistrationResponse
{
    [JsonPropertyName("device_token")]
    public string DeviceToken { get; set; } = string.Empty;

    [JsonPropertyName("policy")]
    public AgentPolicy Policy { get; set; } = new();
}

public class AgentPolicy
{
    [JsonPropertyName("heartbeat_interval")]
    public int HeartbeatIntervalMinutes { get; set; } = 15;

    [JsonPropertyName("delta_sync_interval")]
    public int DeltaSyncIntervalMinutes { get; set; } = 360;

    [JsonPropertyName("full_sync_time")]
    public string FullSyncTime { get; set; } = "03:00";

    [JsonPropertyName("max_retry_attempts")]
    public int MaxRetryAttempts { get; set; } = 3;

    [JsonPropertyName("retry_delay_seconds")]
    public int RetryDelaySeconds { get; set; } = 30;
}

public class HeartbeatRequest
{
    [JsonPropertyName("agent_guid")]
    public string AgentGuid { get; set; } = string.Empty;

    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    [JsonPropertyName("last_sync")]
    public DateTime? LastSync { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = "healthy";
}

public class HeartbeatResponse
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("config_updated")]
    public bool ConfigUpdated { get; set; }
}

public class InventorySyncRequest
{
    [JsonPropertyName("agent_guid")]
    public string AgentGuid { get; set; } = string.Empty;

    [JsonPropertyName("sync_type")]
    public string SyncType { get; set; } = string.Empty; // "delta" or "full"

    [JsonPropertyName("inventory")]
    public InventoryData Inventory { get; set; } = new();

    [JsonPropertyName("last_sync")]
    public DateTime? LastSync { get; set; }
}

public class InventorySyncResponse
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("snipeit_updated")]
    public bool SnipeItUpdated { get; set; }

    [JsonPropertyName("next_sync")]
    public DateTime? NextSync { get; set; }
}

public class UpdateCheckRequest
{
    [JsonPropertyName("agent_guid")]
    public string AgentGuid { get; set; } = string.Empty;

    [JsonPropertyName("current_version")]
    public string CurrentVersion { get; set; } = string.Empty;
}

public class UpdateCheckResponse
{
    [JsonPropertyName("update_available")]
    public bool UpdateAvailable { get; set; }

    [JsonPropertyName("latest_version")]
    public string? LatestVersion { get; set; }

    [JsonPropertyName("download_url")]
    public string? DownloadUrl { get; set; }

    [JsonPropertyName("release_notes")]
    public string? ReleaseNotes { get; set; }

    [JsonPropertyName("force_update")]
    public bool ForceUpdate { get; set; }
}

public class ApiError
{
    [JsonPropertyName("error")]
    public string Error { get; set; } = string.Empty;

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("details")]
    public string? Details { get; set; }

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
