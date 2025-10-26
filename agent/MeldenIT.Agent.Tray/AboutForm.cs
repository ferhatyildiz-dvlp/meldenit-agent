using System.Reflection;

namespace MeldenIT.Agent.Tray;

public partial class AboutForm : Form
{
    public AboutForm()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        this.SuspendLayout();

        // Form properties
        this.Text = "About MeldenIT Agent";
        this.Size = new Size(400, 300);
        this.StartPosition = FormStartPosition.CenterParent;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;

        // Create main panel
        var mainPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 6,
            Padding = new Padding(20)
        };

        // Add rows
        mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));

        // Title
        var titleLabel = new Label
        {
            Text = "MeldenIT Agent",
            Font = new Font("Segoe UI", 16, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter,
            Dock = DockStyle.Fill
        };
        mainPanel.Controls.Add(titleLabel, 0, 0);

        // Version
        var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0.0";
        var versionLabel = new Label
        {
            Text = $"Version: {version}",
            TextAlign = ContentAlignment.MiddleCenter,
            Dock = DockStyle.Fill
        };
        mainPanel.Controls.Add(versionLabel, 0, 1);

        // Description
        var descriptionLabel = new Label
        {
            Text = "Snipe-IT Entegrasyonlu Envanter Ajanı",
            TextAlign = ContentAlignment.MiddleCenter,
            Dock = DockStyle.Fill
        };
        mainPanel.Controls.Add(descriptionLabel, 0, 2);

        // Copyright
        var copyrightLabel = new Label
        {
            Text = "© 2024 MeldenIT. All rights reserved.",
            TextAlign = ContentAlignment.MiddleCenter,
            Dock = DockStyle.Fill
        };
        mainPanel.Controls.Add(copyrightLabel, 0, 3);

        // Spacer
        var spacerLabel = new Label { Text = "" };
        mainPanel.Controls.Add(spacerLabel, 0, 4);

        // Close button
        var closeButton = new Button
        {
            Text = "Close",
            Size = new Size(75, 23),
            Anchor = AnchorStyles.None,
            DialogResult = DialogResult.OK
        };
        mainPanel.Controls.Add(closeButton, 0, 5);

        this.Controls.Add(mainPanel);

        this.ResumeLayout(false);
    }
}
