using MeldenIT.Agent.Service;

namespace MeldenIT.Agent.Service;

public class AgentWorker : BackgroundService
{
    private readonly ILogger<AgentWorker> _logger;
    private readonly IAgentService _agentService;
    private readonly IConfigManager _configManager;
    private Timer? _heartbeatTimer;
    private Timer? _deltaSyncTimer;
    private Timer? _fullSyncTimer;
    private Timer? _updateCheckTimer;

    public AgentWorker(ILogger<AgentWorker> logger, IAgentService agentService, IConfigManager configManager)
    {
        _logger = logger;
        _agentService = agentService;
        _configManager = configManager;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("MeldenIT Agent Service starting");

        try
        {
            // Check if this is the first run
            var isFirstRun = await _configManager.IsFirstRunAsync();
            if (isFirstRun)
            {
                _logger.LogInformation("First run detected, registering agent");
                var registered = await _agentService.RegisterAsync();
                if (!registered)
                {
                    _logger.LogError("Failed to register agent, service will exit");
                    return;
                }
            }

            // Start timers
            await StartTimersAsync();

            _logger.LogInformation("MeldenIT Agent Service started successfully");

            // Keep the service running
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Service shutdown requested");
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Service encountered an unexpected error");
        }
        finally
        {
            await StopTimersAsync();
            _logger.LogInformation("MeldenIT Agent Service stopped");
        }
    }

    private async Task StartTimersAsync()
    {
        var config = await _configManager.LoadConfigAsync();

        // Heartbeat timer (every 15 minutes by default)
        _heartbeatTimer = new Timer(async _ => await SendHeartbeatAsync(), null, 
            TimeSpan.Zero, TimeSpan.FromMinutes(config.HeartbeatIntervalMinutes));

        // Delta sync timer (every 6 hours by default)
        _deltaSyncTimer = new Timer(async _ => await SyncInventoryAsync("delta"), null,
            TimeSpan.FromMinutes(30), TimeSpan.FromMinutes(config.DeltaSyncIntervalMinutes));

        // Full sync timer (daily at configured time)
        _fullSyncTimer = new Timer(async _ => await SyncInventoryAsync("full"), null,
            GetNextFullSyncTime(config.FullSyncTime), TimeSpan.FromDays(1));

        // Update check timer (every 4 hours)
        _updateCheckTimer = new Timer(async _ => await CheckForUpdatesAsync(), null,
            TimeSpan.FromHours(1), TimeSpan.FromHours(4));

        _logger.LogInformation("All timers started");
    }

    private async Task StopTimersAsync()
    {
        _heartbeatTimer?.Dispose();
        _deltaSyncTimer?.Dispose();
        _fullSyncTimer?.Dispose();
        _updateCheckTimer?.Dispose();
        _logger.LogInformation("All timers stopped");
    }

    private async Task SendHeartbeatAsync()
    {
        try
        {
            _logger.LogDebug("Sending heartbeat");
            var success = await _agentService.SendHeartbeatAsync();
            if (!success)
            {
                _logger.LogWarning("Heartbeat failed");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending heartbeat");
        }
    }

    private async Task SyncInventoryAsync(string syncType)
    {
        try
        {
            _logger.LogInformation("Starting {SyncType} inventory sync", syncType);
            var success = await _agentService.SyncInventoryAsync(syncType);
            if (!success)
            {
                _logger.LogWarning("{SyncType} inventory sync failed", syncType);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during {SyncType} inventory sync", syncType);
        }
    }

    private async Task CheckForUpdatesAsync()
    {
        try
        {
            _logger.LogDebug("Checking for updates");
            var updateAvailable = await _agentService.CheckForUpdatesAsync();
            if (updateAvailable)
            {
                _logger.LogInformation("Update available");
                // TODO: Implement update download and installation
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking for updates");
        }
    }

    private TimeSpan GetNextFullSyncTime(string syncTime)
    {
        try
        {
            if (TimeSpan.TryParse(syncTime, out var targetTime))
            {
                var now = DateTime.Now;
                var today = now.Date.Add(targetTime);
                
                if (today > now)
                {
                    return today - now;
                }
                else
                {
                    return today.AddDays(1) - now;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error calculating next full sync time, using default");
        }

        // Default to 3:00 AM tomorrow
        var tomorrow = DateTime.Today.AddDays(1).AddHours(3);
        return tomorrow - DateTime.Now;
    }
}
