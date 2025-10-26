using MeldenIT.Agent.Tray;
using MeldenIT.Agent.Core.Models;

namespace MeldenIT.Agent.Tray;

public partial class SettingsForm : Form
{
    private readonly ITrayService _trayService;
    private readonly ILogger<SettingsForm> _logger;

    public SettingsForm(ITrayService trayService)
    {
        _trayService = trayService;
        _logger = new LoggerFactory().CreateLogger<SettingsForm>();
        InitializeComponent();
        LoadSettings();
    }

    private void InitializeComponent()
    {
        this.SuspendLayout();

        // Form properties
        this.Text = "MeldenIT Agent - Settings";
        this.Size = new Size(500, 400);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;

        // Create main panel
        var mainPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 8,
            Padding = new Padding(10)
        };

        // Add rows
        mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));

        // Add columns
        mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
        mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        // Agent GUID
        mainPanel.Controls.Add(new Label { Text = "Agent GUID:", Anchor = AnchorStyles.Left }, 0, 0);
        var agentGuidLabel = new Label { Text = _trayService.GetAgentGuid(), Anchor = AnchorStyles.Left };
        mainPanel.Controls.Add(agentGuidLabel, 1, 0);

        // Hostname
        mainPanel.Controls.Add(new Label { Text = "Hostname:", Anchor = AnchorStyles.Left }, 0, 1);
        var hostnameLabel = new Label { Text = _trayService.GetHostname(), Anchor = AnchorStyles.Left };
        mainPanel.Controls.Add(hostnameLabel, 1, 1);

        // User
        mainPanel.Controls.Add(new Label { Text = "User:", Anchor = AnchorStyles.Left }, 0, 2);
        var userLabel = new Label { Text = _trayService.GetLoggedInUser(), Anchor = AnchorStyles.Left };
        mainPanel.Controls.Add(userLabel, 1, 2);

        // Status
        mainPanel.Controls.Add(new Label { Text = "Status:", Anchor = AnchorStyles.Left }, 0, 3);
        var statusLabel = new Label { Text = _trayService.GetStatus(), Anchor = AnchorStyles.Left };
        mainPanel.Controls.Add(statusLabel, 1, 3);

        // Last Sync
        mainPanel.Controls.Add(new Label { Text = "Last Sync:", Anchor = AnchorStyles.Left }, 0, 4);
        var lastSyncLabel = new Label { Text = "Loading...", Anchor = AnchorStyles.Left };
        mainPanel.Controls.Add(lastSyncLabel, 1, 4);

        // API URL
        mainPanel.Controls.Add(new Label { Text = "API URL:", Anchor = AnchorStyles.Left }, 0, 5);
        var apiUrlLabel = new Label { Text = "Loading...", Anchor = AnchorStyles.Left };
        mainPanel.Controls.Add(apiUrlLabel, 1, 5);

        // Site Code
        mainPanel.Controls.Add(new Label { Text = "Site Code:", Anchor = AnchorStyles.Left }, 0, 6);
        var siteCodeLabel = new Label { Text = "Loading...", Anchor = AnchorStyles.Left };
        mainPanel.Controls.Add(siteCodeLabel, 1, 6);

        // Buttons
        var buttonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(10)
        };

        var closeButton = new Button
        {
            Text = "Close",
            Size = new Size(75, 23),
            DialogResult = DialogResult.OK
        };
        buttonPanel.Controls.Add(closeButton);

        var syncButton = new Button
        {
            Text = "Sync Now",
            Size = new Size(75, 23)
        };
        syncButton.Click += async (s, e) => await OnSyncNowClicked();
        buttonPanel.Controls.Add(syncButton);

        this.Controls.Add(mainPanel);
        this.Controls.Add(buttonPanel);

        // Store references for updates
        this.Tag = new { lastSyncLabel, apiUrlLabel, siteCodeLabel, statusLabel };

        this.ResumeLayout(false);
    }

    private async void LoadSettings()
    {
        try
        {
            var config = await _trayService.GetConfigAsync();
            var lastSync = _trayService.GetLastSyncTime();

            if (this.Tag is { } tagInfo)
            {
                var info = (dynamic)tagInfo;
                
                if (lastSync.HasValue)
                {
                    info.lastSyncLabel.Text = lastSync.Value.ToString("yyyy-MM-dd HH:mm:ss");
                }
                else
                {
                    info.lastSyncLabel.Text = "Never";
                }

                info.apiUrlLabel.Text = config.ApiUrl;
                info.siteCodeLabel.Text = config.SiteCode;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading settings");
        }
    }

    private async Task OnSyncNowClicked()
    {
        try
        {
            var success = await _trayService.SyncNowAsync();
            if (success)
            {
                MessageBox.Show("Sync completed successfully.", "Sync Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadSettings(); // Refresh the form
            }
            else
            {
                MessageBox.Show("Sync failed. Check the logs for details.", "Sync Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during manual sync from settings");
            MessageBox.Show($"Sync failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
