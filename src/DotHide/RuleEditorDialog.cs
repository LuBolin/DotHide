using DotHide.Models;

namespace DotHide;

public sealed class RuleEditorDialog : Form
{
    private readonly TextBox _txtValue;
    private readonly ComboBox _cmbState;

    public string RuleValue => _txtValue.Text.Trim();
    public RuleState RuleState => _cmbState.SelectedIndex == 0 ? RuleState.Hide : RuleState.Show;

    private RuleEditorDialog(string title, string label, string value, RuleState state, bool allowBrowseFile, bool allowBrowseFolder)
    {
        Text = title;
        Width = 560;
        Height = allowBrowseFile || allowBrowseFolder ? 190 : 165;
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
            Padding = new Padding(12)
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 92));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 118));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        layout.Controls.Add(new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left }, 0, 0);
        _txtValue = new TextBox { Text = value, Dock = DockStyle.Fill };
        layout.Controls.Add(_txtValue, 1, 0);

        var browsePanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, Margin = new Padding(4, 0, 0, 0) };
        if (allowBrowseFolder)
        {
            var btnFolder = new Button { Text = "Folder...", Width = 104 };
            btnFolder.Click += (_, _) => BrowseFolder();
            browsePanel.Controls.Add(btnFolder);
        }
        if (allowBrowseFile)
        {
            var btnFile = new Button { Text = "File...", Width = 104 };
            btnFile.Click += (_, _) => BrowseFile();
            browsePanel.Controls.Add(btnFile);
        }
        layout.Controls.Add(browsePanel, 2, 0);

        layout.Controls.Add(new Label { Text = "State", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 1);
        _cmbState = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 130, Height = 38, IntegralHeight = false };
        _cmbState.Items.Add("Hide");
        _cmbState.Items.Add("Show");
        _cmbState.SelectedIndex = state == RuleState.Hide ? 0 : 1;
        layout.Controls.Add(_cmbState, 1, 1);

        var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
        var btnOk = new Button { Text = "OK", DialogResult = DialogResult.OK, Width = 86 };
        var btnCancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Width = 86 };
        actions.Controls.Add(btnOk);
        actions.Controls.Add(btnCancel);
        layout.Controls.Add(actions, 1, 2);
        layout.SetColumnSpan(actions, 2);

        AcceptButton = btnOk;
        CancelButton = btnCancel;
        Controls.Add(layout);

        btnOk.Click += (_, e) =>
        {
            if (!string.IsNullOrWhiteSpace(RuleValue))
                return;

            MessageBox.Show(this, $"{label} is required.", "Missing value", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            DialogResult = DialogResult.None;
        };
    }

    public static RuleEditorDialog ForName(string title, string value = "", RuleState state = RuleState.Hide) =>
        new(title, "Name", value, state, allowBrowseFile: false, allowBrowseFolder: false);

    public static RuleEditorDialog ForFolder(string title, string value = "", RuleState state = RuleState.Hide) =>
        new(title, "Folder", value, state, allowBrowseFile: false, allowBrowseFolder: true);

    public static RuleEditorDialog ForExactItem(string title, string value = "", RuleState state = RuleState.Hide) =>
        new(title, "Item", value, state, allowBrowseFile: true, allowBrowseFolder: true);

    private void BrowseFolder()
    {
        using var dialog = new FolderBrowserDialog { Description = "Select a folder" };
        if (dialog.ShowDialog(this) == DialogResult.OK)
            _txtValue.Text = dialog.SelectedPath;
    }

    private void BrowseFile()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Select a file",
            CheckFileExists = true,
            Multiselect = false
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
            _txtValue.Text = dialog.FileName;
    }
}
