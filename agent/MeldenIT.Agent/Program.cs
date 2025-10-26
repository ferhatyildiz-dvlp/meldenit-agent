using MeldenIT.Agent.Service;
using MeldenIT.Agent.Tray;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MeldenIT.Agent;

class Program
{
    static async Task Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        // Configure logging
        builder.Services.AddLogging(logging =>
        {
            logging.AddEventLog(new Microsoft.Extensions.Logging.EventLog.EventLogSettings
            {
                SourceName = "MeldenIT Agent",
                LogName = "Application"
            });
            logging.AddConsole();
            logging.SetMinimumLevel(LogLevel.Information);
        });

        // Register services
        builder.Services.AddSingleton<IConfigManager, ConfigManager>();
        builder.Services.AddSingleton<IInventoryCollector, InventoryCollector>();
        builder.Services.AddSingleton<IAgentService, AgentService>();
        builder.Services.AddSingleton<ITrayService, TrayService>();

        var host = builder.Build();
        var logger = host.Services.GetRequiredService<ILogger<Program>>();

        try
        {
            // Check command line arguments
            if (args.Length > 0)
            {
                switch (args[0].ToLower())
                {
                    case "--service":
                        logger.LogInformation("Starting as Windows Service");
                        await RunAsServiceAsync(host);
                        break;
                    case "--tray":
                        logger.LogInformation("Starting as Tray Application");
                        await RunAsTrayAsync(host);
                        break;
                    case "--install":
                        logger.LogInformation("Installing Windows Service");
                        await InstallServiceAsync();
                        break;
                    case "--uninstall":
                        logger.LogInformation("Uninstalling Windows Service");
                        await UninstallServiceAsync();
                        break;
                    default:
                        logger.LogInformation("Starting in interactive mode");
                        await RunInteractiveAsync(host);
                        break;
                }
            }
            else
            {
                // Default: start tray application
                logger.LogInformation("Starting as Tray Application (default)");
                await RunAsTrayAsync(host);
            }
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Application terminated unexpectedly");
            throw;
        }
    }

    private static async Task RunAsServiceAsync(IHost host)
    {
        // This would be implemented to run the service
        // For now, we'll just run the agent service logic
        var agentService = host.Services.GetRequiredService<IAgentService>();
        var logger = host.Services.GetRequiredService<ILogger<Program>>();

        logger.LogInformation("Service mode not fully implemented yet");
        
        // Keep running
        await Task.Delay(Timeout.Infinite);
    }

    private static async Task RunAsTrayAsync(IHost host)
    {
        var trayForm = host.Services.GetRequiredService<TrayForm>();
        Application.Run(trayForm);
    }

    private static async Task RunInteractiveAsync(IHost host)
    {
        var logger = host.Services.GetRequiredService<ILogger<Program>>();
        var agentService = host.Services.GetRequiredService<IAgentService>();

        logger.LogInformation("Running in interactive mode");
        logger.LogInformation("Press any key to exit...");

        // Register agent
        var registered = await agentService.RegisterAsync();
        if (registered)
        {
            logger.LogInformation("Agent registered successfully");
        }
        else
        {
            logger.LogError("Failed to register agent");
        }

        // Send heartbeat
        var heartbeat = await agentService.SendHeartbeatAsync();
        if (heartbeat)
        {
            logger.LogInformation("Heartbeat sent successfully");
        }
        else
        {
            logger.LogError("Failed to send heartbeat");
        }

        // Sync inventory
        var sync = await agentService.SyncInventoryAsync("full");
        if (sync)
        {
            logger.LogInformation("Inventory synced successfully");
        }
        else
        {
            logger.LogError("Failed to sync inventory");
        }

        Console.ReadKey();
    }

    private static async Task InstallServiceAsync()
    {
        // This would implement Windows Service installation
        // For now, just log the action
        Console.WriteLine("Service installation not implemented yet");
    }

    private static async Task UninstallServiceAsync()
    {
        // This would implement Windows Service uninstallation
        // For now, just log the action
        Console.WriteLine("Service uninstallation not implemented yet");
    }
}
