using System.ComponentModel;
using System.Diagnostics;
using DotHide.Models;
using DotHide.Services;

namespace DotHide;

public sealed class MainForm : Form
{
    private const float BaseFontSize = 12F;
    private const float SmallFontSize = 11.5F;
    private const int HeaderTextInset = 5;
    private static readonly Size MinimumContentSize = new(980, 760);

    private static readonly Color AppBackground = Color.FromArgb(246, 247, 249);
    private static readonly Color Surface = Color.White;
    private static readonly Color Border = Color.FromArgb(218, 222, 228);
    private static readonly Color Primary = Color.FromArgb(37, 99, 235);
    private static readonly Color TextMuted = Color.FromArgb(75, 85, 99);

    private readonly DotHideController _controller = new();
    private readonly ToolTip _toolTip = new() { InitialDelay = 200, ReshowDelay = 100 };

    private ComboBox _globalState = null!;
    private CheckBox _strongMode = null!;
    private ListBox _roots = null!;
    private ListBox _globalExceptions = null!;
    private DataGridView _folderConfigs = null!;
    private ListBox _folderExceptions = null!;
    private Label _folderExceptionTitle = null!;
    private DataGridView _preview = null!;
    private Label _status = null!;

    private BindingList<ScanResult> _previewRows = new();

    public MainForm()
    {
        Text = "DotHide";
        ClientSize = new Size(1280, 860);
        MinimumSize = new Size(760, 560);
        StartPosition = FormStartPosition.CenterScreen;
        AutoScaleMode = AutoScaleMode.Dpi;
        Font = new Font("Segoe UI", BaseFontSize);
        BackColor = AppBackground;
        Icon = LoadAppIcon() ?? Icon;

        Controls.Add(BuildScrollableLayout());
        RefreshEverything();
        RefreshPreview();
        FormClosing += (_, _) => _controller.Save();
    }

    private Control BuildScrollableLayout()
    {
        var host = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = AppBackground
        };
        var content = BuildLayout();
        content.Dock = DockStyle.None;
        content.MinimumSize = MinimumContentSize;
        host.Controls.Add(content);

        void ResizeContent()
        {
            content.Size = new Size(
                Math.Max(host.ClientSize.Width, MinimumContentSize.Width),
                Math.Max(host.ClientSize.Height, MinimumContentSize.Height));
        }

        host.SizeChanged += (_, _) => ResizeContent();
        ResizeContent();
        return host;
    }

    private static Icon? LoadAppIcon()
    {
        var stream = typeof(MainForm).Assembly.GetManifestResourceStream("DotHide.AppIcon.ico");
        return stream is null ? null : new Icon(stream);
    }

    private Control BuildLayout()
    {
        var main = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 4,
            ColumnCount = 1,
            Padding = new Padding(18),
            BackColor = AppBackground
        };
        main.RowStyles.Add(new RowStyle(SizeType.Absolute, 88));
        main.RowStyles.Add(new RowStyle(SizeType.Absolute, 410));
        main.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        main.RowStyles.Add(new RowStyle(SizeType.Absolute, 68));

        main.Controls.Add(BuildHeader(), 0, 0);
        main.Controls.Add(BuildConfigArea(), 0, 1);
        main.Controls.Add(BuildPreview(), 0, 2);
        main.Controls.Add(BuildBottomBar(), 0, 3);
        return main;
    }

    private Control BuildHeader()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 2,
            ColumnCount = 2,
            BackColor = AppBackground,
            Margin = new Padding(0)
        };
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 230));

        panel.Controls.Add(new Label
        {
            Text = "DotHide",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 24F, FontStyle.Bold),
            ForeColor = Color.FromArgb(17, 24, 39),
            Margin = new Padding(0)
        }, 0, 0);
        panel.SetColumnSpan(panel.Controls[0], 2);

        panel.Controls.Add(new Label
        {
            Text = "Hide or show dot-prefixed items. Nothing is deleted or moved.",
            Dock = DockStyle.Fill,
            ForeColor = TextMuted,
            Margin = new Padding(HeaderTextInset, 0, 0, 0)
        }, 0, 1);

        var strongRow = new FlowLayoutPanel
        {
            AutoSize = true,
            Dock = DockStyle.None,
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = AppBackground,
            Margin = new Padding(0),
            Padding = new Padding(0, 0, 0, 0)
        };

        _strongMode = new CheckBox
        {
            Text = "",
            AutoSize = false,
            Width = 18,
            Height = 22,
            FlatStyle = FlatStyle.System,
            Margin = new Padding(0, 5, 4, 0)
        };
        _strongMode.CheckedChanged += (_, _) =>
        {
            _controller.SetUseSystemAttribute(_strongMode.Checked);
            RefreshPreview();
        };
        strongRow.Controls.Add(_strongMode);

        var strongLabel = new Label
        {
            Text = "Stronger hide",
            AutoSize = true,
            Margin = new Padding(0, 5, 4, 0),
            Cursor = Cursors.Hand
        };
        strongLabel.Click += (_, _) => _strongMode.Checked = !_strongMode.Checked;
        strongRow.Controls.Add(strongLabel);

        var strongHelpText = "Windows has separate Hidden and System attributes.\n\nHidden alone can still appear as faded items in Explorer when 'Show hidden items' is enabled.\n\nStronger hide sets both Hidden and System, so Explorer keeps dot items hidden unless protected operating system files are shown.";
        strongRow.Controls.Add(HelpIcon(strongHelpText, () => MessageBox.Show(this, strongHelpText, "Stronger Hide", MessageBoxButtons.OK, MessageBoxIcon.Information)));

        panel.Controls.Add(strongRow, 1, 1);
        return panel;
    }

    private Control BuildConfigArea()
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = AppBackground,
            Margin = new Padding(0)
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        layout.Controls.Add(BuildGlobalPanel(), 0, 0);
        layout.Controls.Add(BuildFolderPanel(), 1, 0);
        return layout;
    }

    private Control BuildGlobalPanel()
    {
        var content = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 4, ColumnCount = 1, BackColor = Surface };
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 165));
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 100));

        content.Controls.Add(SectionTitle("Global"), 0, 0);

        var stateRow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Padding = new Padding(0, 10, 0, 10),
            BackColor = Surface
        };
        stateRow.Controls.Add(new Label { Text = "All dot items", AutoSize = true, Margin = new Padding(0, 9, 12, 0) });
        _globalState = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Width = 170,
            Height = 38,
            Margin = new Padding(0),
            IntegralHeight = false
        };
        _globalState.Items.Add("Hide");
        _globalState.Items.Add("Show");
        _globalState.SelectedIndexChanged += (_, _) =>
        {
            if (_globalState.SelectedIndex < 0)
                return;

            _controller.SetGlobalState(_globalState.SelectedIndex == 0 ? RuleState.Hide : RuleState.Show);
            RefreshPreview();
        };
        stateRow.Controls.Add(_globalState);
        content.Controls.Add(stateRow, 0, 1);

        content.Controls.Add(BuildListSection("Managed Folders", out _roots, AddRoot, RemoveRoot, "Add Folder", "Remove", AddDesktopRoot), 0, 2);
        content.Controls.Add(BuildListSection("Exceptions", out _globalExceptions, AddGlobalException, RemoveGlobalException, "Add Name", "Remove"), 0, 3);
        return SurfacePanel(content, new Padding(0, 0, 8, 0));
    }

    private Control BuildFolderPanel()
    {
        var content = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, ColumnCount = 1, BackColor = Surface };
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 174));
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 136));

        content.Controls.Add(SectionTitle("Folders"), 0, 0);

        var configSection = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1, BackColor = Surface, Margin = new Padding(0, 0, 0, 8) };
        configSection.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        configSection.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
        _folderConfigs = Grid();
        _folderConfigs.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Folder", DataPropertyName = nameof(FolderRule.Path), FillWeight = 64 });
        _folderConfigs.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Rule", DataPropertyName = nameof(FolderRule.State), FillWeight = 18 });
        _folderConfigs.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Except.", DataPropertyName = "ExceptionCount", FillWeight = 18 });
        SuppressSelectionHighlight(_folderConfigs);
        _folderConfigs.SelectionChanged += (_, _) => RefreshFolderExceptions();
        configSection.Controls.Add(_folderConfigs, 0, 0);
        configSection.Controls.Add(ButtonRow(("Add Folder", AddFolderConfig), ("Edit", EditFolderConfig), ("Remove", RemoveFolderConfig)), 0, 1);
        content.Controls.Add(configSection, 0, 1);

        var exceptionSection = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, ColumnCount = 1, BackColor = Surface };
        exceptionSection.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        exceptionSection.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        exceptionSection.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
        _folderExceptionTitle = new Label { Text = "Exceptions", Dock = DockStyle.Fill, Font = new Font("Segoe UI", BaseFontSize, FontStyle.Bold), Margin = new Padding(0) };
        _folderExceptions = List();
        exceptionSection.Controls.Add(_folderExceptionTitle, 0, 0);
        exceptionSection.Controls.Add(_folderExceptions, 0, 1);
        exceptionSection.Controls.Add(ButtonRow(("Add Name", AddFolderException), ("Remove", RemoveFolderException)), 0, 2);
        content.Controls.Add(exceptionSection, 0, 2);

        return SurfacePanel(content, new Padding(8, 0, 0, 0));
    }

    private Control BuildListSection(string title, out ListBox list, Action add, Action remove, string addText, string removeText, Action? secondary = null)
    {
        var section = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, ColumnCount = 1, BackColor = Surface, Margin = new Padding(0, 0, 0, 6) };
        section.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        section.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        section.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));

        section.Controls.Add(new Label { Text = title, Dock = DockStyle.Fill, Font = new Font("Segoe UI", BaseFontSize, FontStyle.Bold), Margin = new Padding(0) }, 0, 0);
        list = List();
        section.Controls.Add(list, 0, 1);

        var buttons = secondary is null
            ? ButtonRow((addText, add), (removeText, remove))
            : ButtonRow((addText, add), ("Add Desktop", secondary), (removeText, remove));
        section.Controls.Add(buttons, 0, 2);
        return section;
    }

    private Control BuildPreview()
    {
        var content = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1, BackColor = Surface };
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        content.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        content.Controls.Add(SectionTitle("Preview"), 0, 0);

        _preview = Grid();
        _preview.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Path", DataPropertyName = nameof(ScanResult.Path), FillWeight = 46 });
        _preview.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Attributes", DataPropertyName = nameof(ScanResult.CurrentAttributes), FillWeight = 16 });
        _preview.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Config", DataPropertyName = nameof(ScanResult.RuleSourceDescription), FillWeight = 22 });
        _preview.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Desired", DataPropertyName = nameof(ScanResult.DesiredState), FillWeight = 8 });
        _preview.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Action", DataPropertyName = nameof(ScanResult.PendingAction), FillWeight = 8 });
        foreach (DataGridViewColumn column in _preview.Columns)
            column.SortMode = DataGridViewColumnSortMode.NotSortable;

        SuppressSelectionHighlight(_preview);
        _preview.SelectionChanged += (_, _) => _preview.ClearSelection();
        _preview.CellFormatting += (_, e) =>
        {
            if (_preview.Columns[e.ColumnIndex].DataPropertyName == nameof(ScanResult.PendingAction) &&
                e.Value is PendingAction action)
            {
                e.Value = action == PendingAction.NoChange ? "Done" : action.ToString();
                e.FormattingApplied = true;
            }
        };
        content.Controls.Add(_preview, 0, 1);
        return SurfacePanel(content, new Padding(0));
    }

    private Control BuildBottomBar()
    {
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, BackColor = AppBackground };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 510));

        var statusPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = AppBackground,
            Padding = new Padding(0, 14, 8, 0)
        };
        _status = new Label
        {
            Text = "Ready",
            Dock = DockStyle.Top,
            Height = 38,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = TextMuted
        };
        statusPanel.Controls.Add(_status);
        layout.Controls.Add(statusPanel, 0, 0);

        var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, WrapContents = false, BackColor = AppBackground, Padding = new Padding(0, 12, 0, 0) };
        actions.Controls.Add(Button("Apply", ApplyChanges, primary: true, width: 132));
        actions.Controls.Add(Button("Restore", RestoreAll, width: 104));
        actions.Controls.Add(Button("Save", SaveConfig, width: 76));
        actions.Controls.Add(Button("Config", OpenConfigFolder, width: 96));
        layout.Controls.Add(actions, 1, 0);
        return layout;
    }

    private static Panel SurfacePanel(Control child, Padding margin)
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Surface,
            Padding = new Padding(14),
            Margin = margin,
            BorderStyle = BorderStyle.FixedSingle
        };
        panel.Controls.Add(child);
        return panel;
    }

    private static Control SectionTitle(string title)
    {
        return new Label
        {
            Text = title,
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 14F, FontStyle.Bold),
            ForeColor = Color.FromArgb(17, 24, 39),
            Margin = new Padding(0)
        };
    }

    private static FlowLayoutPanel ButtonRow(params (string Text, Action Action)[] items)
    {
        var row = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Surface,
            Padding = new Padding(0, 4, 0, 0)
        };

        foreach (var item in items)
            row.Controls.Add(Button(item.Text, item.Action));

        return row;
    }

    private Control HelpIcon(string helpText, Action action)
    {
        var icon = new Label
        {
            Width = 17,
            Height = 17,
            Margin = new Padding(0, 7, 0, 0),
            Cursor = Cursors.Hand,
            AccessibleName = "Help",
            BackColor = AppBackground
        };
        _toolTip.SetToolTip(icon, helpText);
        icon.Click += (_, _) => action();
        icon.Paint += (_, e) =>
        {
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using var borderPen = new Pen(Border, 1.4F);
            using var helpFont = new Font("Segoe UI", 7.5F, FontStyle.Bold);
            e.Graphics.DrawEllipse(borderPen, 1.5F, 1.5F, icon.Width - 4, icon.Height - 4);
            TextRenderer.DrawText(
                e.Graphics,
                "?",
                helpFont,
                new Rectangle(0, 0, icon.Width, icon.Height),
                TextMuted,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        };
        return icon;
    }

    private static Button Button(string text, Action action, bool primary = false, int width = 0)
    {
        var button = new Button
        {
            Text = text,
            AutoSize = width == 0,
            Width = width == 0 ? 0 : width,
            Height = 38,
            Margin = new Padding(0, 0, 8, 8),
            FlatStyle = FlatStyle.Flat,
            BackColor = primary ? Primary : Color.White,
            ForeColor = primary ? Color.White : Color.FromArgb(31, 41, 55),
            Font = new Font("Segoe UI", SmallFontSize, primary ? FontStyle.Bold : FontStyle.Regular)
        };
        button.FlatAppearance.BorderColor = primary ? Primary : Border;
        button.FlatAppearance.BorderSize = 1;
        button.Click += (_, _) => action();
        return button;
    }

    private static ListBox List() => new()
    {
        Dock = DockStyle.Fill,
        BorderStyle = BorderStyle.FixedSingle,
        HorizontalScrollbar = true,
        IntegralHeight = false,
        ItemHeight = 30,
        Font = new Font("Segoe UI", SmallFontSize)
    };

    private static DataGridView Grid()
    {
        var grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            AutoGenerateColumns = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
            ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None,
            EnableHeadersVisualStyles = false,
            MultiSelect = false,
            RowHeadersVisible = false,
            RowTemplate = { Height = 32 },
            SelectionMode = DataGridViewSelectionMode.FullRowSelect
        };
        grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(241, 245, 249);
        grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(31, 41, 55);
        grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", SmallFontSize, FontStyle.Bold);
        grid.ColumnHeadersHeight = 34;
        grid.DefaultCellStyle.Font = new Font("Segoe UI", SmallFontSize);
        grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(219, 234, 254);
        grid.DefaultCellStyle.SelectionForeColor = Color.FromArgb(17, 24, 39);
        return grid;
    }

    private static void SuppressSelectionHighlight(DataGridView grid)
    {
        grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(241, 245, 249);
        grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = Color.FromArgb(31, 41, 55);
        grid.DefaultCellStyle.SelectionBackColor = Color.White;
        grid.DefaultCellStyle.SelectionForeColor = Color.FromArgb(17, 24, 39);
    }

    private void RefreshEverything()
    {
        _strongMode.Checked = _controller.Config.UseSystemAttribute;
        _globalState.SelectedIndex = _controller.Config.GlobalState == RuleState.Hide ? 0 : 1;
        RefreshRoots();
        RefreshGlobalExceptions();
        RefreshFolderConfigs();
        RefreshFolderExceptions();
    }

    private void RefreshRoots()
    {
        _roots.DataSource = null;
        _roots.DataSource = _controller.Config.Roots.ToList();
    }

    private void RefreshGlobalExceptions()
    {
        _globalExceptions.DataSource = null;
        _globalExceptions.DataSource = _controller.Config.GlobalExceptions.ToList();
    }

    private void RefreshFolderConfigs()
    {
        var rows = _controller.Config.FolderRules
            .Select(rule => new FolderRuleRow(rule))
            .ToList();
        _folderConfigs.DataSource = null;
        _folderConfigs.DataSource = rows;
    }

    private void RefreshFolderExceptions()
    {
        var rule = SelectedFolderRule();
        _folderExceptionTitle.Text = rule is null
            ? "Exceptions"
            : $"Exceptions ({Opposite(rule.State)})";

        _folderExceptions.DataSource = null;
        _folderExceptions.DataSource = rule?.Exceptions.ToList() ?? new List<string>();
    }

    private void RefreshPreview()
    {
        try
        {
            _previewRows = new BindingList<ScanResult>(_controller.Scan().ToList());
            _preview.DataSource = null;
            _preview.DataSource = _previewRows;
            ColorPreviewRows();
            var pending = _previewRows.Count(row => row.PendingAction != PendingAction.NoChange);
            var status = pending == 0
                ? $"{_previewRows.Count} item(s). Already applied."
                : $"{_previewRows.Count} item(s), {pending} pending change(s).";
            var missingRoots = FileScanner.LastScanErrors.Count(error => error.StartsWith("Root folder not found:", StringComparison.OrdinalIgnoreCase));
            if (missingRoots > 0)
                status += missingRoots == 1
                    ? " 1 configured folder missing."
                    : $" {missingRoots} configured folders missing.";

            _status.Text = status;
        }
        catch (Exception ex)
        {
            _status.Text = $"Preview failed: {ex.Message}";
        }
    }

    private void ColorPreviewRows()
    {
        foreach (DataGridViewRow row in _preview.Rows)
        {
            if (row.DataBoundItem is not ScanResult result)
                continue;

            row.DefaultCellStyle.BackColor = result.PendingAction switch
            {
                PendingAction.Hide => Color.FromArgb(254, 242, 242),
                PendingAction.Show => Color.FromArgb(240, 253, 244),
                _ when !result.Exists => Color.FromArgb(254, 249, 195),
                _ => Color.White
            };
        }
    }

    private void AddRoot()
    {
        using var dialog = new FolderBrowserDialog { Description = "Choose a folder DotHide should manage" };
        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        if (!_controller.AddRoot(dialog.SelectedPath))
            MessageBox.Show(this, "That folder is already managed.", "Duplicate", MessageBoxButtons.OK, MessageBoxIcon.Information);

        RefreshRoots();
        RefreshPreview();
    }

    private void AddDesktopRoot()
    {
        if (!_controller.AddDesktopRoot())
            MessageBox.Show(this, "Desktop is already managed.", "Duplicate", MessageBoxButtons.OK, MessageBoxIcon.Information);

        RefreshRoots();
        RefreshPreview();
    }

    private void RemoveRoot()
    {
        if (_roots.SelectedIndex < 0)
            return;

        _controller.RemoveRootAt(_roots.SelectedIndex);
        RefreshRoots();
        RefreshPreview();
    }

    private void AddGlobalException()
    {
        using var dialog = new TextInputDialog("Add Global Exception", "Name");
        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        AddName(_controller.Config.GlobalExceptions, dialog.Value, "global exception");
        SaveRefreshAll();
    }

    private void RemoveGlobalException()
    {
        if (_globalExceptions.SelectedItem is not string name)
            return;

        _controller.Config.GlobalExceptions.RemoveAll(item => string.Equals(item, name, StringComparison.OrdinalIgnoreCase));
        SaveRefreshAll();
    }

    private void AddFolderConfig()
    {
        using var dialog = new FolderConfigDialog("Add Folder Config");
        if (dialog.ShowDialog(this) != DialogResult.OK || !TryNormalize(dialog.FolderPath, out var path))
            return;

        if (_controller.Config.FolderRules.Any(rule => PathHelper.PathsEqual(rule.Path, path)))
        {
            MessageBox.Show(this, "That folder already has a config.", "Duplicate", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        _controller.Config.FolderRules.Add(new FolderRule { Path = path, State = dialog.State });
        if (!_controller.Config.Roots.Any(root => PathHelper.PathsEqual(root, path)))
            _controller.Config.Roots.Add(path);

        SaveRefreshAll();
    }

    private void EditFolderConfig()
    {
        var rule = SelectedFolderRule();
        if (rule is null)
            return;

        using var dialog = new FolderConfigDialog("Edit Folder Config", rule.Path, rule.State);
        if (dialog.ShowDialog(this) != DialogResult.OK || !TryNormalize(dialog.FolderPath, out var path))
            return;

        if (_controller.Config.FolderRules.Any(other => !ReferenceEquals(other, rule) && PathHelper.PathsEqual(other.Path, path)))
        {
            MessageBox.Show(this, "That folder already has a config.", "Duplicate", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        rule.Path = path;
        rule.State = dialog.State;
        SaveRefreshAll();
    }

    private void RemoveFolderConfig()
    {
        var rule = SelectedFolderRule();
        if (rule is null)
            return;

        _controller.Config.FolderRules.Remove(rule);
        SaveRefreshAll();
    }

    private void AddFolderException()
    {
        var rule = SelectedFolderRule();
        if (rule is null)
            return;

        using var dialog = new TextInputDialog("Add Folder Exception", "Name");
        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        AddName(rule.Exceptions, dialog.Value, "folder exception");
        SaveRefreshAll();
    }

    private void RemoveFolderException()
    {
        var rule = SelectedFolderRule();
        if (rule is null || _folderExceptions.SelectedItem is not string name)
            return;

        rule.Exceptions.RemoveAll(item => string.Equals(item, name, StringComparison.OrdinalIgnoreCase));
        SaveRefreshAll();
    }

    private void AddName(List<string> names, string name, string label)
    {
        if (names.Any(item => string.Equals(item, name, StringComparison.OrdinalIgnoreCase)))
        {
            MessageBox.Show(this, $"That {label} already exists.", "Duplicate", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        names.Add(name);
    }

    private FolderRule? SelectedFolderRule()
    {
        if (_folderConfigs.CurrentRow?.DataBoundItem is FolderRuleRow row)
            return row.Rule;

        return null;
    }

    private bool TryNormalize(string input, out string normalized)
    {
        try
        {
            normalized = PathHelper.NormalizePath(input);
            return true;
        }
        catch (Exception ex)
        {
            normalized = "";
            MessageBox.Show(this, $"That path is not valid:\n{ex.Message}", "Invalid path", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }
    }

    private void ApplyChanges()
    {
        var rows = _controller.Scan().Where(row => row.PendingAction != PendingAction.NoChange).ToList();
        if (rows.Count == 0)
        {
            MessageBox.Show(this, "There are no pending changes.", "Nothing to apply", MessageBoxButtons.OK, MessageBoxIcon.Information);
            RefreshPreview();
            return;
        }

        var hide = rows.Count(row => row.PendingAction == PendingAction.Hide);
        var show = rows.Count(row => row.PendingAction == PendingAction.Show);
        var confirm = $"Apply changes?\n\nHide: {hide}\nShow: {show}\n\nDotHide only changes Windows attributes.";
        if (MessageBox.Show(this, confirm, "Apply Changes", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            return;

        var summary = _controller.ApplyChanges(rows);
        MessageBox.Show(this, $"Hidden: {summary.HiddenCount}\nShown: {summary.ShownCount}\nFailed: {summary.FailedCount}", "Apply Complete", MessageBoxButtons.OK, summary.FailedCount > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
        RefreshPreview();
    }

    private void RestoreAll()
    {
        if (_controller.Config.ManagedItems.Count == 0)
        {
            MessageBox.Show(this, "No managed items to restore.", "Nothing to restore", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (MessageBox.Show(this, $"Restore {_controller.Config.ManagedItems.Count} managed item(s) to their original attributes?", "Restore All", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            return;

        var summary = _controller.RestoreAll();
        MessageBox.Show(this, $"Restored: {summary.RestoredCount}\nSkipped: {summary.SkippedCount}\nFailed: {summary.FailedCount}", "Restore Complete", MessageBoxButtons.OK, summary.FailedCount > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
        RefreshPreview();
    }

    private void SaveConfig()
    {
        _controller.Save();
        _status.Text = "Configuration saved.";
    }

    private void OpenConfigFolder()
    {
        Process.Start(new ProcessStartInfo("explorer.exe", _controller.ConfigFolder) { UseShellExecute = true });
    }

    private void SaveRefreshAll()
    {
        _controller.Save();
        RefreshRoots();
        RefreshGlobalExceptions();
        RefreshFolderConfigs();
        RefreshFolderExceptions();
        RefreshPreview();
    }

    private sealed class FolderRuleRow
    {
        public FolderRuleRow(FolderRule rule) => Rule = rule;
        public FolderRule Rule { get; }
        public string Path => Rule.Path;
        public RuleState State => Rule.State;
        public int ExceptionCount => Rule.Exceptions.Count;
    }

    private static RuleState Opposite(RuleState state) => state == RuleState.Hide ? RuleState.Show : RuleState.Hide;
}
