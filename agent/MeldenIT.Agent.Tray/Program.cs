using MeldenIT.Agent.Tray;
using MeldenIT.Agent.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

// To customize application configuration such as set high DPI settings or default font,
// see https://aka.ms/applicationconfiguration.
ApplicationConfiguration.Initialize();

var builder = Host.CreateApplicationBuilder(args);

// Configure logging
builder.Services.AddLogging(logging =>
{
    logging.AddEventLog(new Microsoft.Extensions.Logging.EventLog.EventLogSettings
    {
        SourceName = "MeldenIT Agent Tray",
        LogName = "Application"
    });
    logging.AddConsole();
    logging.SetMinimumLevel(LogLevel.Information);
});

// Configure HTTP client
builder.Services.AddHttpClient<IApiClient, ApiClient>(client =>
{
    client.Timeout = TimeSpan.FromMinutes(5);
    client.DefaultRequestHeaders.Add("User-Agent", "MeldenIT-Agent-Tray/1.0");
});

// Register services
builder.Services.AddSingleton<IConfigManager, ConfigManager>();
builder.Services.AddSingleton<IInventoryCollector, InventoryCollector>();
builder.Services.AddSingleton<ITrayService, TrayService>();

// Register the main form
builder.Services.AddTransient<TrayForm>();

var host = builder.Build();

try
{
    var trayForm = host.Services.GetRequiredService<TrayForm>();
    Application.Run(trayForm);
}
catch (Exception ex)
{
    var logger = host.Services.GetRequiredService<ILogger<Program>>();
    logger.LogCritical(ex, "Tray application terminated unexpectedly");
    throw;
}
