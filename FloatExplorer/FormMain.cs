using FloatExplorer.Core;

namespace FloatExplorer;

public partial class FormMain : Form
{
    private readonly TextBox inputBox = new();
    private readonly ComboBox inputKindBox = new();
    private readonly ComboBox roundingBox = new();
    private readonly ComboBox specialValueBox = new();
    private readonly Button parseButton = new();
    private readonly FormatViewControl formatView = new("浮点格式", "fp32");
    private readonly UlpNumberLineControl numberLine = new();
    private readonly Label inputValueLabel = MetricValueLabel();
    private readonly Label storedValueLabel = MetricValueLabel();
    private readonly Label absoluteErrorLabel = MetricValueLabel();
    private readonly Label relativeErrorLabel = MetricValueLabel();
    private readonly Label ulpErrorLabel = MetricValueLabel();
    private readonly Label statusLabel = new();

    public FormMain()
    {
        InitializeComponent();
        BuildInterface();
        Shown += (_, _) =>
        {
            formatView.SelectFormat("fp32");
            ApplyInput();
        };
    }

    private void BuildInterface()
    {
        BackColor = Color.FromArgb(244, 246, 248);
        Font = new Font("Segoe UI", 9f);

        TableLayoutPanel root = new() { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4, Padding = new Padding(12) };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 78));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 190));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        root.Controls.Add(BuildToolbar(), 0, 0);

        formatView.Margin = new Padding(0, 0, 0, 10);
        root.Controls.Add(formatView, 0, 1);
        root.Controls.Add(BuildAnalysisArea(), 0, 2);

        statusLabel.Dock = DockStyle.Fill;
        statusLabel.TextAlign = ContentAlignment.MiddleLeft;
        statusLabel.ForeColor = Color.FromArgb(80, 94, 109);
        statusLabel.Padding = new Padding(4, 0, 0, 0);
        root.Controls.Add(statusLabel, 0, 3);
        Controls.Add(root);

        formatView.SelectedFormatChanged += (_, _) => ApplyInput();
        formatView.ValueEdited += (_, value) => ApplyEditedValue(value);
    }

    private Control BuildToolbar()
    {
        TableLayoutPanel toolbar = new() { Dock = DockStyle.Fill, ColumnCount = 5, RowCount = 1, BackColor = Color.White, Padding = new Padding(10, 7, 10, 7), Margin = new Padding(0, 0, 0, 10) };
        toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35));
        toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15));
        toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 23));
        toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 17));
        toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 10));

        inputBox.Text = "0.1";
        inputBox.Dock = DockStyle.Fill;
        inputBox.Font = new Font("Consolas", 10f);
        inputBox.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) { ApplyInput(); e.SuppressKeyPress = true; } };

        inputKindBox.DropDownStyle = ComboBoxStyle.DropDownList;
        inputKindBox.Dock = DockStyle.Fill;
        inputKindBox.DataSource = new[]
        {
            new Choice<FloatInputKind>("十进制数值", FloatInputKind.Decimal),
            new Choice<FloatInputKind>("Hex 位串", FloatInputKind.HexBits),
            new Choice<FloatInputKind>("二进制位串", FloatInputKind.BinaryBits)
        };

        roundingBox.DropDownStyle = ComboBoxStyle.DropDownList;
        roundingBox.Dock = DockStyle.Fill;
        roundingBox.DataSource = new[]
        {
            new Choice<FloatRoundingMode>("最近偶数（RNE）", FloatRoundingMode.ToNearestEven),
            new Choice<FloatRoundingMode>("朝零（RZ）", FloatRoundingMode.TowardZero),
            new Choice<FloatRoundingMode>("朝 +∞（RU）", FloatRoundingMode.TowardPositiveInfinity),
            new Choice<FloatRoundingMode>("朝 −∞（RD）", FloatRoundingMode.TowardNegativeInfinity)
        };

        specialValueBox.DropDownStyle = ComboBoxStyle.DropDownList;
        specialValueBox.Dock = DockStyle.Fill;
        specialValueBox.Items.AddRange(["选择示例…", "0.1（典型舍入）", "+0", "−0", "最小 Subnormal", "最小 Normal", "最大有限值", "+Infinity", "NaN"]);
        specialValueBox.SelectedIndex = 0;
        specialValueBox.SelectedIndexChanged += (_, _) => ApplySpecialValue();

        parseButton.Text = "解析";
        parseButton.Dock = DockStyle.Fill;
        parseButton.BackColor = Color.FromArgb(22, 119, 210);
        parseButton.ForeColor = Color.White;
        parseButton.FlatStyle = FlatStyle.Flat;
        parseButton.FlatAppearance.BorderSize = 0;
        parseButton.Click += (_, _) => ApplyInput();

        toolbar.Controls.Add(Labeled("输入数值 / 位串", inputBox), 0, 0);
        toolbar.Controls.Add(Labeled("输入解释", inputKindBox), 1, 0);
        toolbar.Controls.Add(Labeled("舍入模式", roundingBox), 2, 0);
        toolbar.Controls.Add(Labeled("特殊值", specialValueBox), 3, 0);
        toolbar.Controls.Add(parseButton, 4, 0);
        return toolbar;
    }

    private Control BuildAnalysisArea()
    {
        GroupBox group = new() { Text = "输入值与量化结果", Dock = DockStyle.Fill, Padding = new Padding(12), BackColor = Color.White, Margin = new Padding(0) };
        TableLayoutPanel layout = new() { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42));

        Panel linePanel = new() { Dock = DockStyle.Fill };
        Label lineTitle = new() { Text = "前一个可表示值 — 当前量化值 — 后一个可表示值（橙点为原始输入）", Dock = DockStyle.Top, Height = 25, ForeColor = Color.FromArgb(70, 84, 100) };
        numberLine.Dock = DockStyle.Fill;
        linePanel.Controls.Add(numberLine);
        linePanel.Controls.Add(lineTitle);

        TableLayoutPanel metrics = new() { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 5, Padding = new Padding(18, 4, 4, 4) };
        metrics.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48));
        metrics.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 52));
        AddMetric(metrics, 0, "原始输入值", inputValueLabel);
        AddMetric(metrics, 1, "量化后的实际值", storedValueLabel);
        AddMetric(metrics, 2, "绝对误差", absoluteErrorLabel);
        AddMetric(metrics, 3, "相对误差", relativeErrorLabel);
        AddMetric(metrics, 4, "误差 / 当前格式 ULP", ulpErrorLabel);

        layout.Controls.Add(linePanel, 0, 0);
        layout.Controls.Add(metrics, 1, 0);
        group.Controls.Add(layout);
        return group;
    }

    private void ApplyInput()
    {
        if (inputKindBox.SelectedItem is not Choice<FloatInputKind> inputChoice || roundingBox.SelectedItem is not Choice<FloatRoundingMode> roundingChoice) return;
        if (inputChoice.Value != FloatInputKind.Decimal)
        {
            if (!FloatCodec.TryParseRawBits(inputBox.Text, inputChoice.Value, formatView.SelectedFormat, out ulong rawBits, out string? rawError))
            {
                ShowError(rawError);
                return;
            }

            FloatValue decoded = FloatCodec.Decode(rawBits, formatView.SelectedFormat);
            PresentValue(decoded.Value, decoded, "由原始位串精确解码");
            return;
        }

        if (!FloatCodec.TryParseInput(inputBox.Text, inputChoice.Value, formatView.SelectedFormat, out double input, out string? error))
        {
            ShowError(error);
            return;
        }

        FloatValue quantized = FloatCodec.Quantize(input, formatView.SelectedFormat, roundingChoice.Value);
        PresentValue(input, quantized, "十进制输入量化");
    }

    internal void LoadPreview(string formatId, string input)
    {
        formatView.SelectFormat(formatId);
        inputKindBox.SelectedIndex = 0;
        inputBox.Text = input;
        ApplyInput();
        Refresh();
    }

    private void PresentValue(double input, FloatValue quantized, string source)
    {
        formatView.SetValue(quantized);
        double absoluteError = input.Equals(quantized.Value) || double.IsNaN(input) && double.IsNaN(quantized.Value) ? 0d : Math.Abs(quantized.Value - input);
        double relativeError = double.IsFinite(absoluteError) && double.IsFinite(input) && input != 0d ? absoluteError / Math.Abs(input) : absoluteError;
        double ulpError = double.IsFinite(absoluteError) && double.IsFinite(quantized.Ulp) && quantized.Ulp > 0d ? absoluteError / quantized.Ulp : double.NaN;

        inputValueLabel.Text = FormatViewControl.FormatNumber(input);
        storedValueLabel.Text = FormatViewControl.FormatNumber(quantized.Value);
        absoluteErrorLabel.Text = FormatViewControl.FormatNumber(absoluteError);
        relativeErrorLabel.Text = FormatMetric(relativeError);
        ulpErrorLabel.Text = FormatMetric(ulpError);
        numberLine.SetValue(quantized, input);

        statusLabel.ForeColor = Color.FromArgb(69, 85, 101);
        statusLabel.Text = $"{source}：{formatView.SelectedFormat.DisplayName}　{quantized.Hex}　　单击任意彩色位可翻转该位。";
    }

    private void ApplyEditedValue(FloatValue edited)
    {
        inputKindBox.SelectedIndex = 0;
        inputBox.Text = FormatViewControl.FormatNumber(edited.Value).Replace("−", "-", StringComparison.Ordinal);
        PresentValue(edited.Value, edited, "手动编辑位串");
    }

    private void ApplySpecialValue()
    {
        if (specialValueBox.SelectedIndex <= 0) return;
        FloatFormat format = formatView.SelectedFormat;
        string input = specialValueBox.SelectedIndex switch
        {
            1 => "0.1",
            2 => "0",
            3 => "-0",
            4 => FormatViewControl.FormatNumber(FloatCodec.Decode(1, format).Value),
            5 => FormatViewControl.FormatNumber(FloatCodec.Decode(1UL << format.FractionBits, format).Value),
            6 => FormatViewControl.FormatNumber(FloatCodec.Decode((Convert.ToUInt64(format.MaxFiniteExponentField) << format.FractionBits) | Convert.ToUInt64(format.MaxFiniteFractionField), format).Value),
            7 => "Infinity",
            8 => "NaN",
            _ => inputBox.Text
        };
        inputKindBox.SelectedIndex = 0;
        inputBox.Text = input;
        ApplyInput();
    }

    private void ShowError(string? error)
    {
        statusLabel.ForeColor = Color.Firebrick;
        statusLabel.Text = error ?? "无法解析输入。";
    }

    private static Control Labeled(string text, Control control)
    {
        TableLayoutPanel panel = new() { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1, Margin = new Padding(4, 0, 8, 0) };
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 18));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        panel.Controls.Add(new Label { Text = text, Dock = DockStyle.Fill, ForeColor = Color.FromArgb(83, 98, 114), Font = new Font("Segoe UI", 8f) }, 0, 0);
        panel.Controls.Add(control, 0, 1);
        return panel;
    }

    private static void AddMetric(TableLayoutPanel panel, int row, string name, Label value)
    {
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 20));
        panel.Controls.Add(new Label { Text = name, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, ForeColor = Color.FromArgb(83, 98, 114) }, 0, row);
        panel.Controls.Add(value, 1, row);
    }

    private static Label MetricValueLabel() => new() { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight, Font = new Font("Consolas", 9f, FontStyle.Bold), ForeColor = Color.FromArgb(18, 94, 151), AutoEllipsis = true };
    private static string FormatMetric(double value) => double.IsNaN(value) ? "—" : value.ToString("0.######E+00");

    private sealed record Choice<T>(string Label, T Value)
    {
        public override string ToString() => Label;
    }
}
