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
        BackColor = Color.FromArgb(238, 242, 247);
        Font = new Font("Segoe UI", 9.25f);

        TableLayoutPanel root = new() { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Padding = new Padding(14) };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 112));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 528));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.Controls.Add(BuildToolbar(), 0, 0);

        formatView.Margin = new Padding(0, 0, 0, 12);
        root.Controls.Add(formatView, 0, 1);
        root.Controls.Add(BuildAnalysisArea(), 0, 2);

        statusLabel.Dock = DockStyle.Fill;
        statusLabel.TextAlign = ContentAlignment.MiddleLeft;
        statusLabel.ForeColor = Color.FromArgb(63, 78, 94);
        statusLabel.BackColor = Color.FromArgb(225, 237, 248);
        statusLabel.Padding = new Padding(10, 0, 0, 0);
        statusLabel.Margin = new Padding(0, 6, 0, 0);
        Controls.Add(root);

        formatView.SelectedFormatChanged += (_, _) => ApplyInput();
        formatView.ValueEdited += (_, value) => ApplyEditedValue(value);
    }

    private Control BuildToolbar()
    {
        TableLayoutPanel card = new() { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle, Padding = new Padding(12, 8, 12, 7), Margin = new Padding(0, 0, 0, 12) };
        card.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        card.RowStyles.Add(new RowStyle(SizeType.Absolute, 25));

        TableLayoutPanel toolbar = new() { Dock = DockStyle.Fill, ColumnCount = 5, RowCount = 1, Margin = new Padding(0) };
        toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 36));
        toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15));
        toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 21));
        toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 18));
        toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 10));

        inputBox.Text = "0.1";
        inputBox.Dock = DockStyle.Fill;
        inputBox.Font = new Font("Consolas", 10f);
        inputBox.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) { ApplyInput(); e.SuppressKeyPress = true; } };

        inputKindBox.DropDownStyle = ComboBoxStyle.DropDownList;
        inputKindBox.Dock = DockStyle.Fill;
        inputKindBox.FlatStyle = FlatStyle.Flat;
        inputKindBox.DataSource = new[]
        {
            new Choice<FloatInputKind>("十进制数值", FloatInputKind.Decimal),
            new Choice<FloatInputKind>("Hex 位串", FloatInputKind.HexBits),
            new Choice<FloatInputKind>("二进制位串", FloatInputKind.BinaryBits)
        };

        roundingBox.DropDownStyle = ComboBoxStyle.DropDownList;
        roundingBox.Dock = DockStyle.Fill;
        roundingBox.FlatStyle = FlatStyle.Flat;
        roundingBox.DataSource = new[]
        {
            new Choice<FloatRoundingMode>("最近偶数（RNE）", FloatRoundingMode.ToNearestEven),
            new Choice<FloatRoundingMode>("朝零（RZ）", FloatRoundingMode.TowardZero),
            new Choice<FloatRoundingMode>("朝 +∞（RU）", FloatRoundingMode.TowardPositiveInfinity),
            new Choice<FloatRoundingMode>("朝 −∞（RD）", FloatRoundingMode.TowardNegativeInfinity)
        };

        specialValueBox.DropDownStyle = ComboBoxStyle.DropDownList;
        specialValueBox.Dock = DockStyle.Fill;
        specialValueBox.FlatStyle = FlatStyle.Flat;
        specialValueBox.Items.AddRange(["选择示例…", "0.1（典型舍入）", "+0", "−0", "最小 Subnormal", "最小 Normal", "最大有限值", "+Infinity", "NaN"]);
        specialValueBox.SelectedIndex = 0;
        specialValueBox.SelectedIndexChanged += (_, _) => ApplySpecialValue();

        parseButton.Text = "应用  ↵";
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
        card.Controls.Add(toolbar, 0, 0);
        card.Controls.Add(statusLabel, 0, 1);
        return card;
    }

    private Control BuildAnalysisArea()
    {
        Panel card = new() { Dock = DockStyle.Fill, BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle, Margin = new Padding(0) };
        TableLayoutPanel cardLayout = new() { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Padding = new Padding(12, 8, 12, 10) };
        cardLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        cardLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        Label sectionTitle = new() { Text = "量化误差与相邻值", Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10.5f, FontStyle.Bold), ForeColor = Color.FromArgb(31, 45, 61), TextAlign = ContentAlignment.MiddleLeft };

        TableLayoutPanel layout = new() { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = new Padding(0) };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 61));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 39));

        Panel linePanel = new() { Dock = DockStyle.Fill };
        Label lineTitle = new() { Text = "相邻可表示值　● 量化值　● 原始输入", Dock = DockStyle.Top, Height = 25, ForeColor = Color.FromArgb(70, 84, 100), Font = new Font("Segoe UI", 8.75f) };
        numberLine.Dock = DockStyle.Fill;
        linePanel.Controls.Add(numberLine);
        linePanel.Controls.Add(lineTitle);

        TableLayoutPanel metrics = new() { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 5, BackColor = Color.FromArgb(247, 249, 252), Padding = new Padding(14, 6, 12, 6), Margin = new Padding(14, 0, 0, 0) };
        metrics.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 43));
        metrics.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 57));
        AddMetric(metrics, 0, "原始输入值", inputValueLabel);
        AddMetric(metrics, 1, "量化后的实际值", storedValueLabel);
        AddMetric(metrics, 2, "绝对误差", absoluteErrorLabel);
        AddMetric(metrics, 3, "相对误差", relativeErrorLabel);
        AddMetric(metrics, 4, "误差 / 当前格式 ULP", ulpErrorLabel);

        layout.Controls.Add(linePanel, 0, 0);
        layout.Controls.Add(metrics, 1, 0);
        cardLayout.Controls.Add(sectionTitle, 0, 0);
        cardLayout.Controls.Add(layout, 0, 1);
        card.Controls.Add(cardLayout);
        return card;
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
        relativeErrorLabel.Text = FormatRelativeError(relativeError);
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
        TableLayoutPanel panel = new() { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1, Margin = new Padding(3, 0, 9, 0) };
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 19));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        panel.Controls.Add(new Label { Text = text, Dock = DockStyle.Fill, ForeColor = Color.FromArgb(83, 98, 114), Font = new Font("Segoe UI", 8.25f, FontStyle.Bold) }, 0, 0);
        panel.Controls.Add(control, 0, 1);
        return panel;
    }

    private static void AddMetric(TableLayoutPanel panel, int row, string name, Label value)
    {
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 20));
        panel.Controls.Add(new Label { Text = name, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, ForeColor = Color.FromArgb(83, 98, 114), Font = new Font("Segoe UI", 8.75f) }, 0, row);
        panel.Controls.Add(value, 1, row);
    }

    private static Label MetricValueLabel() => new() { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight, Font = new Font("Consolas", 9f, FontStyle.Bold), ForeColor = Color.FromArgb(18, 94, 151), AutoEllipsis = true, Padding = new Padding(0, 0, 2, 0) };
    private static string FormatMetric(double value) => double.IsNaN(value) ? "—" : value.ToString("0.######E+00");
    private static string FormatRelativeError(double value) => double.IsNaN(value) ? "—" : $"{value:E6}  ({value * 100d:G6}%)";

    private sealed record Choice<T>(string Label, T Value)
    {
        public override string ToString() => Label;
    }
}
