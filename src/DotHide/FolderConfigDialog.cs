using DotHide.Models;

namespace DotHide;

public sealed class FolderConfigDialog : Form
{
    private readonly TextBox _pathBox;
    private readonly ComboBox _stateBox;

    public string FolderPath => _pathBox.Text.Trim();
    public RuleState State => _stateBox.SelectedIndex == 0 ? RuleState.Hide : RuleState.Show;

    public FolderConfigDialog(string title, string path = "", RuleState state = RuleState.Hide)
    {
        Text = title;
        ClientSize = new Size(720, 230);
        AutoScaleMode = AutoScaleMode.Dpi;
        Font = new Font("Segoe UI", 12F);
        MinimizeBox = false;
        MaximizeBox = false;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        ShowInTaskbar = false;

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 3,
            Padding = new Padding(18)
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 124));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 126));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        layout.Controls.Add(new Label { Text = "Folder", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
        _pathBox = new TextBox { Text = path, Dock = DockStyle.Fill, Margin = new Padding(0, 4, 8, 0) };
        layout.Controls.Add(_pathBox, 1, 0);
        var browse = new Button { Text = "Browse...", Width = 112, Height = 36 };
        browse.Click += (_, _) => BrowseFolder();
        layout.Controls.Add(browse, 2, 0);

        layout.Controls.Add(new Label { Text = "Rule", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 1);
        _stateBox = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 150, Height = 38, IntegralHeight = false };
        _stateBox.Items.Add("Hide");
        _stateBox.Items.Add("Show");
        _stateBox.SelectedIndex = state == RuleState.Hide ? 0 : 1;
        layout.Controls.Add(_stateBox, 1, 1);

        var actions = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 48, FlowDirection = FlowDirection.RightToLeft, WrapContents = false };
        var ok = new Button { Text = "OK", DialogResult = DialogResult.OK, Width = 96, Height = 36 };
        var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Width = 96, Height = 36 };
        actions.Controls.Add(ok);
        actions.Controls.Add(cancel);
        layout.Controls.Add(actions, 1, 2);
        layout.SetColumnSpan(actions, 2);

        ok.Click += (_, _) =>
        {
            if (!string.IsNullOrWhiteSpace(FolderPath))
                return;

            MessageBox.Show(this, "Choose a folder first.", "Missing folder", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            DialogResult = DialogResult.None;
        };

        AcceptButton = ok;
        CancelButton = cancel;
        Controls.Add(layout);
    }

    private void BrowseFolder()
    {
        using var dialog = new FolderBrowserDialog { Description = "Select a folder" };
        if (dialog.ShowDialog(this) == DialogResult.OK)
            _pathBox.Text = dialog.SelectedPath;
    }
}
