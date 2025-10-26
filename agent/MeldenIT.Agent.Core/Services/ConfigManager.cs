using System.Security.Cryptography;
using System.Text.Json;
using MeldenIT.Agent.Core.Models;

namespace MeldenIT.Agent.Core.Services;

public class ConfigManager : IConfigManager
{
    private readonly ILogger<ConfigManager> _logger;
    private readonly string _configPath;
    private readonly string _dataDirectory;

    public ConfigManager(ILogger<ConfigManager> logger)
    {
        _logger = logger;
        _dataDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "MeldenIT", "Agent");
        _configPath = Path.Combine(_dataDirectory, "agent.json");
        
        // Ensure data directory exists
        Directory.CreateDirectory(_dataDirectory);
    }

    public async Task<AgentConfig> LoadConfigAsync()
    {
        _logger.LogInformation("Loading configuration from {ConfigPath}", _configPath);

        try
        {
            if (!File.Exists(_configPath))
            {
                _logger.LogInformation("Configuration file not found, creating default configuration");
                var defaultConfig = CreateDefaultConfig();
                await SaveConfigAsync(defaultConfig);
                return defaultConfig;
            }

            var json = await File.ReadAllTextAsync(_configPath);
            var config = JsonSerializer.Deserialize<AgentConfig>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (config == null)
            {
                _logger.LogWarning("Failed to deserialize configuration, using defaults");
                return CreateDefaultConfig();
            }

            _logger.LogInformation("Configuration loaded successfully");
            return config;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading configuration, using defaults");
            return CreateDefaultConfig();
        }
    }

    public async Task SaveConfigAsync(AgentConfig config)
    {
        _logger.LogInformation("Saving configuration to {ConfigPath}", _configPath);

        try
        {
            var json = JsonSerializer.Serialize(config, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await File.WriteAllTextAsync(_configPath, json);
            _logger.LogInformation("Configuration saved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving configuration");
            throw;
        }
    }

    public async Task<string> GetOrCreateAgentGuidAsync()
    {
        _logger.LogInformation("Getting or creating agent GUID");

        try
        {
            var config = await LoadConfigAsync();
            
            if (string.IsNullOrEmpty(config.AgentGuid))
            {
                config.AgentGuid = Guid.NewGuid().ToString();
                await SaveConfigAsync(config);
                _logger.LogInformation("Generated new agent GUID: {AgentGuid}", config.AgentGuid);
            }

            return config.AgentGuid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting or creating agent GUID");
            throw;
        }
    }

    public async Task<string?> GetDeviceTokenAsync()
    {
        _logger.LogDebug("Retrieving device token from secure storage");

        try
        {
            var config = await LoadConfigAsync();
            return config.DeviceToken;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving device token");
            return null;
        }
    }

    public async Task SaveDeviceTokenAsync(string token)
    {
        _logger.LogInformation("Saving device token to secure storage");

        try
        {
            var config = await LoadConfigAsync();
            config.DeviceToken = token;
            await SaveConfigAsync(config);
            _logger.LogInformation("Device token saved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving device token");
            throw;
        }
    }

    public async Task<bool> IsFirstRunAsync()
    {
        _logger.LogDebug("Checking if this is the first run");

        try
        {
            var config = await LoadConfigAsync();
            return string.IsNullOrEmpty(config.DeviceToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking first run status");
            return true;
        }
    }

    private AgentConfig CreateDefaultConfig()
    {
        return new AgentConfig
        {
            ApiUrl = "https://assit.meldencloud.com",
            SnipeItUrl = "https://assit.meldencloud.com",
            SiteCode = Environment.GetEnvironmentVariable("MELDENIT_SITE_CODE") ?? "UNKNOWN",
            HeartbeatIntervalMinutes = 15,
            DeltaSyncIntervalMinutes = 360,
            FullSyncTime = "03:00",
            ProxyEnabled = false,
            LogLevel = "Information",
            MaxRetryAttempts = 3,
            RetryDelaySeconds = 30
        };
    }
}
