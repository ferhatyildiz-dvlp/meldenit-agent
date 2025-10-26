using MeldenIT.Agent.Service;
using MeldenIT.Agent.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);

// Configure Windows Service
builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "MeldenITAgentSvc";
});

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

// Configure HTTP client
builder.Services.AddHttpClient<IApiClient, ApiClient>(client =>
{
    client.Timeout = TimeSpan.FromMinutes(5);
    client.DefaultRequestHeaders.Add("User-Agent", "MeldenIT-Agent/1.0");
});

// Register services
builder.Services.AddSingleton<IConfigManager, ConfigManager>();
builder.Services.AddSingleton<IInventoryCollector, InventoryCollector>();
builder.Services.AddSingleton<IAgentService, AgentService>();

// Register the main service
builder.Services.AddHostedService<AgentWorker>();

var host = builder.Build();

try
{
    await host.RunAsync();
}
catch (Exception ex)
{
    var logger = host.Services.GetRequiredService<ILogger<Program>>();
    logger.LogCritical(ex, "Application terminated unexpectedly");
    throw;
}
