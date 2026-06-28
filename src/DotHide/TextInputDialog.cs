namespace DotHide;

public sealed class TextInputDialog : Form
{
    private readonly TextBox _textBox;

    public string Value => _textBox.Text.Trim();

    public TextInputDialog(string title, string label, string initialValue = "")
    {
        Text = title;
        ClientSize = new Size(500, 180);
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
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(18)
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        layout.Controls.Add(new Label { Text = label, Dock = DockStyle.Fill, Margin = new Padding(0) });
        _textBox = new TextBox { Text = initialValue, Dock = DockStyle.Fill, Margin = new Padding(0) };
        layout.Controls.Add(_textBox);

        var actions = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 44, FlowDirection = FlowDirection.RightToLeft, WrapContents = false };
        var ok = new Button { Text = "OK", DialogResult = DialogResult.OK, Width = 96, Height = 36 };
        var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Width = 96, Height = 36 };
        actions.Controls.Add(ok);
        actions.Controls.Add(cancel);
        layout.Controls.Add(actions);

        ok.Click += (_, _) =>
        {
            if (!string.IsNullOrWhiteSpace(Value))
                return;

            MessageBox.Show(this, "Enter a name first.", "Missing name", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            DialogResult = DialogResult.None;
        };

        AcceptButton = ok;
        CancelButton = cancel;
        Controls.Add(layout);
    }
}
