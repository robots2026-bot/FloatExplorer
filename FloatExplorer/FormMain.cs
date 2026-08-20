using FloatExplorer.Core;

namespace FloatExplorer;

public partial class FormMain : Form
{
    private readonly TextBox inputBox = new();
    private readonly ComboBox inputKindBox = new();
    private readonly ComboBox roundingBox = new();
    private readonly ComboBox specialValueBox = new();
    private readonly Button compareButton = new();
    private readonly FormatViewControl formatA = new("格式 A", "fp32");
    private readonly FormatViewControl formatB = new("格式 B", "bf16");
    private readonly UlpNumberLineControl numberLine = new();
    private readonly Label absoluteErrorValue = MetricValueLabel();
    private readonly Label relativeErrorValue = MetricValueLabel();
    private readonly Label aUlpErrorValue = MetricValueLabel();
    private readonly Label bUlpErrorValue = MetricValueLabel();
    private readonly Label rangeValue = MetricValueLabel();
    private readonly Label statusLabel = new();

    public FormMain()
    {
        InitializeComponent();
        BuildInterface();
        Shown += (_, _) =>
        {
            formatA.SelectFormat("fp32");
            formatB.SelectFormat("bf16");
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
        root.Controls.Add(BuildFormatArea(), 0, 1);
        root.Controls.Add(BuildComparisonArea(), 0, 2);

        statusLabel.Dock = DockStyle.Fill;
        statusLabel.TextAlign = ContentAlignment.MiddleLeft;
        statusLabel.ForeColor = Color.FromArgb(80, 94, 109);
        statusLabel.Padding = new Padding(4, 0, 0, 0);
        root.Controls.Add(statusLabel, 0, 3);
        Controls.Add(root);

        formatA.SelectedFormatChanged += (_, _) => ApplyInput();
        formatB.SelectedFormatChanged += (_, _) => ApplyInput();
        formatA.ValueEdited += (_, value) => ApplyEditedValue(value, editedA: true);
        formatB.ValueEdited += (_, value) => ApplyEditedValue(value, editedA: false);
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
            new Choice<FloatInputKind>("A 的 Hex 位串", FloatInputKind.HexBits),
            new Choice<FloatInputKind>("A 的二进制位串", FloatInputKind.BinaryBits)
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

        compareButton.Text = "解析并比较";
        compareButton.Dock = DockStyle.Fill;
        compareButton.BackColor = Color.FromArgb(22, 119, 210);
        compareButton.ForeColor = Color.White;
        compareButton.FlatStyle = FlatStyle.Flat;
        compareButton.FlatAppearance.BorderSize = 0;
        compareButton.Click += (_, _) => ApplyInput();

        toolbar.Controls.Add(Labeled("输入数值 / 位串", inputBox), 0, 0);
        toolbar.Controls.Add(Labeled("输入解释", inputKindBox), 1, 0);
        toolbar.Controls.Add(Labeled("舍入模式", roundingBox), 2, 0);
        toolbar.Controls.Add(Labeled("特殊值", specialValueBox), 3, 0);
        toolbar.Controls.Add(compareButton, 4, 0);
        return toolbar;
    }

    private Control BuildFormatArea()
    {
        TableLayoutPanel formats = new() { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = new Padding(0) };
        formats.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        formats.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        formatA.Margin = new Padding(0, 0, 6, 10);
        formatB.Margin = new Padding(6, 0, 0, 10);
        formats.Controls.Add(formatA, 0, 0);
        formats.Controls.Add(formatB, 1, 0);
        return formats;
    }

    private Control BuildComparisonArea()
    {
        GroupBox group = new() { Text = "格式差异与 ULP", Dock = DockStyle.Fill, Padding = new Padding(12), BackColor = Color.White, Margin = new Padding(0) };
        TableLayoutPanel layout = new() { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42));

        Panel linePanel = new() { Dock = DockStyle.Fill };
        Label lineTitle = new() { Text = "A 的前一个值 — A 当前值 — A 的后一个值（橙点为 B）", Dock = DockStyle.Top, Height = 25, ForeColor = Color.FromArgb(70, 84, 100) };
        numberLine.Dock = DockStyle.Fill;
        linePanel.Controls.Add(numberLine);
        linePanel.Controls.Add(lineTitle);

        TableLayoutPanel metrics = new() { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 5, Padding = new Padding(18, 4, 4, 4) };
        metrics.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 52));
        metrics.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48));
        AddMetric(metrics, 0, "绝对误差 |B − A|", absoluteErrorValue);
        AddMetric(metrics, 1, "相对误差", relativeErrorValue);
        AddMetric(metrics, 2, "误差 / A 的 ULP", aUlpErrorValue);
        AddMetric(metrics, 3, "误差 / B 的 ULP", bUlpErrorValue);
        AddMetric(metrics, 4, "指数范围 / 精度", rangeValue);

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
            if (!FloatCodec.TryParseRawBits(inputBox.Text, inputChoice.Value, formatA.SelectedFormat, out ulong rawBits, out string? rawError))
            {
                ShowError(rawError);
                return;
            }

            FloatValue decodedA = FloatCodec.Decode(rawBits, formatA.SelectedFormat);
            FloatValue convertedB = FloatCodec.Quantize(decodedA.Value, formatB.SelectedFormat, roundingChoice.Value);
            PresentValues(decodedA.Value, decodedA, convertedB, $"由 {formatA.SelectedFormat.DisplayName} 原始位串解码");
            return;
        }

        if (!FloatCodec.TryParseInput(inputBox.Text, inputChoice.Value, formatA.SelectedFormat, out double input, out string? error))
        {
            ShowError(error);
            return;
        }

        ApplyNumericValue(input, roundingChoice.Value, "十进制输入");
    }

    private void ApplyNumericValue(double input, FloatRoundingMode roundingMode, string source)
    {
        FloatValue a = FloatCodec.Quantize(input, formatA.SelectedFormat, roundingMode);
        FloatValue b = FloatCodec.Quantize(input, formatB.SelectedFormat, roundingMode);
        PresentValues(input, a, b, source);
    }

    private void PresentValues(double input, FloatValue a, FloatValue b, string source)
    {
        formatA.SetValue(a);
        formatB.SetValue(b);

        FloatComparison comparison = FloatComparison.Create(a, b);
        absoluteErrorValue.Text = FormatViewControl.FormatNumber(comparison.AbsoluteError);
        relativeErrorValue.Text = FormatMetric(comparison.RelativeError, percent: true);
        aUlpErrorValue.Text = FormatMetric(comparison.ErrorInAUlps);
        bUlpErrorValue.Text = FormatMetric(comparison.ErrorInBUlps);
        rangeValue.Text = $"A: e{a.Format.ExponentBits}/f{a.Format.FractionBits}　B: e{b.Format.ExponentBits}/f{b.Format.FractionBits}";
        numberLine.SetValues(a, b);
        statusLabel.ForeColor = Color.FromArgb(69, 85, 101);
        statusLabel.Text = $"{source}：{FormatViewControl.FormatNumber(input)}　→　A {a.Hex}　/　B {b.Hex}　　单击任意彩色位可翻转该位。";
    }

    private void ApplyEditedValue(FloatValue edited, bool editedA)
    {
        inputKindBox.SelectedIndex = 0;
        inputBox.Text = FormatViewControl.FormatNumber(edited.Value).Replace("−", "-", StringComparison.Ordinal);
        FloatRoundingMode roundingMode = ((Choice<FloatRoundingMode>)roundingBox.SelectedItem!).Value;
        FloatValue a = editedA ? edited : FloatCodec.Quantize(edited.Value, formatA.SelectedFormat, roundingMode);
        FloatValue b = editedA ? FloatCodec.Quantize(edited.Value, formatB.SelectedFormat, roundingMode) : edited;
        PresentValues(edited.Value, a, b, $"手动编辑 {edited.Format.DisplayName} 位串");
    }

    private void ShowError(string? error)
    {
        statusLabel.ForeColor = Color.Firebrick;
        statusLabel.Text = error ?? "无法解析输入。";
    }

    private void ApplySpecialValue()
    {
        if (specialValueBox.SelectedIndex <= 0) return;
        FloatFormat format = formatA.SelectedFormat;
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
    private static string FormatMetric(double value, bool percent = false) => double.IsNaN(value) ? "—" : percent ? value.ToString("0.000000E+00") : value.ToString("0.######");

    private sealed record Choice<T>(string Label, T Value)
    {
        public override string ToString() => Label;
    }
}
