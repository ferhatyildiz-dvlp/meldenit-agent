using MeldenIT.Agent.Tray;
using MeldenIT.Agent.Core.Models;

namespace MeldenIT.Agent.Tray;

public partial class TrayForm : Form
{
    private readonly ILogger<TrayForm> _logger;
    private readonly ITrayService _trayService;
    private NotifyIcon? _notifyIcon;
    private ContextMenuStrip? _contextMenu;
    private Timer? _updateTimer;

    public TrayForm(ILogger<TrayForm> logger, ITrayService trayService)
    {
        _logger = logger;
        _trayService = trayService;
        InitializeComponent();
        InitializeTray();
        StartUpdateTimer();
    }

    private void InitializeComponent()
    {
        this.SuspendLayout();
        
        // Form properties
        this.WindowState = FormWindowState.Minimized;
        this.ShowInTaskbar = false;
        this.Visible = false;
        
        this.ResumeLayout(false);
    }

    private void InitializeTray()
    {
        try
        {
            // Create notify icon
            _notifyIcon = new NotifyIcon
            {
                Icon = SystemIcons.Application,
                Text = "MeldenIT Agent",
                Visible = true
            };

            // Create context menu
            _contextMenu = new ContextMenuStrip();
            
            // Status item
            var statusItem = new ToolStripLabel("Status: Initializing");
            _contextMenu.Items.Add(statusItem);
            _contextMenu.Items.Add(new ToolStripSeparator());

            // Hostname item
            var hostnameItem = new ToolStripLabel($"Hostname: {_trayService.GetHostname()}");
            _contextMenu.Items.Add(hostnameItem);

            // User item
            var userItem = new ToolStripLabel($"User: {_trayService.GetLoggedInUser()}");
            _contextMenu.Items.Add(userItem);

            // Last sync item
            var lastSyncItem = new ToolStripLabel("Last Sync: Never");
            _contextMenu.Items.Add(lastSyncItem);

            _contextMenu.Items.Add(new ToolStripSeparator());

            // Sync Now button
            var syncNowItem = new ToolStripMenuItem("Sync Now", null, OnSyncNowClicked);
            _contextMenu.Items.Add(syncNowItem);

            // Settings button
            var settingsItem = new ToolStripMenuItem("Settings", null, OnSettingsClicked);
            _contextMenu.Items.Add(settingsItem);

            // Troubleshooting button
            var troubleshootingItem = new ToolStripMenuItem("Troubleshooting", null, OnTroubleshootingClicked);
            _contextMenu.Items.Add(troubleshootingItem);

            _contextMenu.Items.Add(new ToolStripSeparator());

            // About button
            var aboutItem = new ToolStripMenuItem("About", null, OnAboutClicked);
            _contextMenu.Items.Add(aboutItem);

            // Exit button
            var exitItem = new ToolStripMenuItem("Exit", null, OnExitClicked);
            _contextMenu.Items.Add(exitItem);

            _notifyIcon.ContextMenuStrip = _contextMenu;

            // Handle double-click
            _notifyIcon.DoubleClick += OnTrayIconDoubleClicked;

            _logger.LogInformation("Tray icon initialized");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing tray icon");
        }
    }

    private void StartUpdateTimer()
    {
        _updateTimer = new Timer
        {
            Interval = 30000 // Update every 30 seconds
        };
        _updateTimer.Tick += OnUpdateTimerTick;
        _updateTimer.Start();
    }

    private async void OnUpdateTimerTick(object? sender, EventArgs e)
    {
        try
        {
            await UpdateTrayInfoAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating tray info");
        }
    }

    private async Task UpdateTrayInfoAsync()
    {
        try
        {
            if (_contextMenu == null) return;

            var status = _trayService.GetStatus();
            var lastSync = _trayService.GetLastSyncTime();
            var hostname = _trayService.GetHostname();
            var user = _trayService.GetLoggedInUser();

            // Update status
            if (_contextMenu.Items[0] is ToolStripLabel statusItem)
            {
                statusItem.Text = $"Status: {status}";
            }

            // Update hostname
            if (_contextMenu.Items[2] is ToolStripLabel hostnameItem)
            {
                hostnameItem.Text = $"Hostname: {hostname}";
            }

            // Update user
            if (_contextMenu.Items[3] is ToolStripLabel userItem)
            {
                userItem.Text = $"User: {user}";
            }

            // Update last sync
            if (_contextMenu.Items[4] is ToolStripLabel lastSyncItem)
            {
                if (lastSync.HasValue)
                {
                    lastSyncItem.Text = $"Last Sync: {lastSync.Value:yyyy-MM-dd HH:mm:ss}";
                }
                else
                {
                    lastSyncItem.Text = "Last Sync: Never";
                }
            }

            // Update tooltip
            if (_notifyIcon != null)
            {
                var tooltip = $"MeldenIT Agent\nStatus: {status}\nHostname: {hostname}\nUser: {user}";
                if (lastSync.HasValue)
                {
                    tooltip += $"\nLast Sync: {lastSync.Value:yyyy-MM-dd HH:mm:ss}";
                }
                _notifyIcon.Text = tooltip;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating tray information");
        }
    }

    private async void OnSyncNowClicked(object? sender, EventArgs e)
    {
        try
        {
            _logger.LogInformation("Sync Now clicked from tray");
            
            if (_contextMenu?.Items[0] is ToolStripLabel statusItem)
            {
                statusItem.Text = "Status: Syncing...";
            }

            var success = await _trayService.SyncNowAsync();
            
            if (success)
            {
                await _trayService.ShowNotificationAsync("Sync Complete", "Inventory synchronized successfully");
            }
            else
            {
                await _trayService.ShowNotificationAsync("Sync Failed", "Failed to synchronize inventory", true);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during manual sync");
            await _trayService.ShowNotificationAsync("Sync Error", $"Sync failed: {ex.Message}", true);
        }
    }

    private void OnSettingsClicked(object? sender, EventArgs e)
    {
        try
        {
            _logger.LogInformation("Settings clicked from tray");
            
            // Open settings form (read-only for now)
            var settingsForm = new SettingsForm(_trayService);
            settingsForm.ShowDialog();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error opening settings");
        }
    }

    private void OnTroubleshootingClicked(object? sender, EventArgs e)
    {
        try
        {
            _logger.LogInformation("Troubleshooting clicked from tray");
            
            // Open log folder
            var logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "MeldenIT", "Agent", "logs");
            if (Directory.Exists(logPath))
            {
                System.Diagnostics.Process.Start("explorer.exe", logPath);
            }
            else
            {
                MessageBox.Show("Log folder not found.", "Troubleshooting", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error opening log folder");
            MessageBox.Show($"Error opening log folder: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OnAboutClicked(object? sender, EventArgs e)
    {
        try
        {
            var aboutForm = new AboutForm();
            aboutForm.ShowDialog();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error opening about dialog");
        }
    }

    private void OnExitClicked(object? sender, EventArgs e)
    {
        try
        {
            _logger.LogInformation("Exit requested from tray");
            Application.Exit();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during exit");
        }
    }

    private void OnTrayIconDoubleClicked(object? sender, EventArgs e)
    {
        try
        {
            // Double-click shows settings
            OnSettingsClicked(sender, e);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling tray icon double-click");
        }
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        this.Hide();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        // Hide to tray instead of closing
        if (e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            this.Hide();
        }
        else
        {
            base.OnFormClosing(e);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _updateTimer?.Dispose();
            _notifyIcon?.Dispose();
            _contextMenu?.Dispose();
        }
        base.Dispose(disposing);
    }
}
