using System.Globalization;
using FloatExplorer.Core;

namespace FloatExplorer;

public sealed class FormatViewControl : UserControl
{
    private readonly ComboBox formatBox = new();
    private readonly Label layoutLabel = new();
    private readonly Label descriptionLabel = new();
    private readonly BitFieldControl bitField = new();
    private readonly Label signDetailLabel = new();
    private readonly Label exponentDetailLabel = new();
    private readonly Label precisionDetailLabel = new();
    private readonly FloatingPointFormulaControl formulaControl = new();
    private readonly TableLayoutPanel properties = new();
    private readonly Dictionary<string, Label> valueLabels = [];

    public event EventHandler? SelectedFormatChanged;
    public event EventHandler<FloatValue>? ValueEdited;

    public FloatFormat SelectedFormat => (FloatFormat)formatBox.SelectedItem!;
    public FloatValue? Value { get; private set; }

    public FormatViewControl(string caption, string defaultFormatId)
    {
        Dock = DockStyle.Fill;
        Padding = new Padding(14, 10, 14, 8);
        BackColor = Color.White;
        BorderStyle = BorderStyle.FixedSingle;

        TableLayoutPanel root = new() { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 6, Margin = new Padding(0) };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 88));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 122));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 94));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 112));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));

        Panel header = new() { Dock = DockStyle.Fill };
        Label captionLabel = new() { Text = caption, AutoSize = true, Font = new Font("Segoe UI", 12f, FontStyle.Bold), ForeColor = Color.FromArgb(29, 43, 58), Location = new Point(0, 2) };
        formatBox.DropDownStyle = ComboBoxStyle.DropDownList;
        formatBox.DataSource = FloatFormat.All.ToList();
        formatBox.DisplayMember = nameof(FloatFormat.DisplayName);
        formatBox.FlatStyle = FlatStyle.Flat;
        formatBox.Font = new Font("Segoe UI", 9.25f, FontStyle.Bold);
        formatBox.Width = 210;
        formatBox.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        formatBox.Location = new Point(Math.Max(220, Width - formatBox.Width), 4);
        formatBox.SelectedIndexChanged += (_, _) => OnFormatChanged();
        header.Controls.Add(captionLabel);
        header.Controls.Add(formatBox);
        header.Resize += (_, _) => formatBox.Left = header.ClientSize.Width - formatBox.Width;

        descriptionLabel.AutoSize = false;
        descriptionLabel.Location = new Point(0, 30);
        descriptionLabel.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right;
        descriptionLabel.Width = Math.Max(200, header.ClientSize.Width - 230);
        descriptionLabel.Height = 23;
        descriptionLabel.ForeColor = Color.FromArgb(76, 91, 108);
        descriptionLabel.Font = new Font("Segoe UI", 8.75f);
        descriptionLabel.AutoEllipsis = true;
        header.Controls.Add(descriptionLabel);
        header.Resize += (_, _) => descriptionLabel.Width = Math.Max(200, header.ClientSize.Width - 230);

        bitField.Dock = DockStyle.Fill;
        bitField.RawBitsChanged += (_, raw) =>
        {
            FloatValue edited = FloatCodec.Decode(raw, SelectedFormat);
            SetValue(edited);
            ValueEdited?.Invoke(this, edited);
        };

        TableLayoutPanel details = new() { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1, Margin = new Padding(0), Padding = new Padding(0, 4, 0, 6) };
        details.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20));
        details.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35));
        details.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));
        details.Controls.Add(CreateDetailBlock("正负号（Sign）", Color.FromArgb(255, 232, 235), Color.FromArgb(190, 58, 73), signDetailLabel), 0, 0);
        details.Controls.Add(CreateDetailBlock("指数（Exponent）", Color.FromArgb(255, 247, 224), Color.FromArgb(154, 104, 0), exponentDetailLabel), 1, 0);
        details.Controls.Add(CreateDetailBlock("数据精度（Fraction）", Color.FromArgb(228, 247, 252), Color.FromArgb(0, 112, 145), precisionDetailLabel), 2, 0);

        formulaControl.Dock = DockStyle.Fill;

        properties.Dock = DockStyle.Fill;
        properties.BackColor = Color.FromArgb(247, 249, 252);
        properties.Padding = new Padding(12, 7, 12, 7);
        properties.ColumnCount = 4;
        properties.RowCount = 4;
        properties.RowStyles.Add(new RowStyle(SizeType.Percent, 25));
        properties.RowStyles.Add(new RowStyle(SizeType.Percent, 25));
        properties.RowStyles.Add(new RowStyle(SizeType.Percent, 25));
        properties.RowStyles.Add(new RowStyle(SizeType.Percent, 25));
        properties.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 22));
        properties.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28));
        properties.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 22));
        properties.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28));
        AddProperty("Hex", 0, 0);
        AddProperty("分类", 2, 0);
        AddProperty("十进制值", 0, 1);
        AddProperty("实际指数", 2, 1);
        AddProperty("指数域", 0, 2);
        AddProperty("Fraction", 2, 2);
        AddProperty("ULP", 0, 3);
        AddProperty("相邻值", 2, 3);

        layoutLabel.Dock = DockStyle.Fill;
        layoutLabel.ForeColor = Color.FromArgb(84, 98, 114);
        layoutLabel.Font = new Font("Segoe UI", 8.25f);
        layoutLabel.TextAlign = ContentAlignment.MiddleLeft;

        root.Controls.Add(header, 0, 0);
        root.Controls.Add(bitField, 0, 1);
        root.Controls.Add(details, 0, 2);
        root.Controls.Add(formulaControl, 0, 3);
        root.Controls.Add(properties, 0, 4);
        root.Controls.Add(layoutLabel, 0, 5);
        Controls.Add(root);

        formatBox.SelectedItem = FloatFormat.All.Single(format => format.Id == defaultFormatId);
        OnFormatChanged();
    }

    public void SetValue(FloatValue value)
    {
        Value = value;
        bitField.Value = value;
        formulaControl.Value = value;
        UpdateExplanation(value);
        valueLabels["Hex"].Text = value.Hex;
        valueLabels["分类"].Text = value.CategoryName;
        valueLabels["十进制值"].Text = FormatNumber(value.Value);
        valueLabels["实际指数"].Text = value.UnbiasedExponent?.ToString(CultureInfo.InvariantCulture) ?? "—";
        valueLabels["指数域"].Text = $"{value.ExponentField} (0x{value.ExponentField:X})";
        valueLabels["Fraction"].Text = $"{value.FractionField} (0x{value.FractionField:X})";
        valueLabels["ULP"].Text = FormatNumber(value.Ulp);
        valueLabels["相邻值"].Text = $"{FormatNumber(FloatCodec.PreviousRepresentable(value))} … {FormatNumber(FloatCodec.NextRepresentable(value))}";
    }

    public void SelectFormat(string formatId)
    {
        int index = FloatFormat.All.ToList().FindIndex(format => format.Id == formatId);
        if (index < 0) return;
        if (formatBox.SelectedIndex == index) OnFormatChanged();
        else formatBox.SelectedIndex = index;
    }

    private void AddProperty(string name, int column, int row)
    {
        Label key = new() { Text = name, Dock = DockStyle.Fill, ForeColor = Color.FromArgb(95, 106, 119), TextAlign = ContentAlignment.MiddleLeft, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), Margin = new Padding(2, 1, 3, 1) };
        Label value = new() { Dock = DockStyle.Fill, ForeColor = Color.FromArgb(18, 94, 151), TextAlign = ContentAlignment.MiddleLeft, Font = new Font("Consolas", 8.75f), AutoEllipsis = true, Margin = new Padding(2, 1, 8, 1) };
        properties.Controls.Add(key, column, row);
        properties.Controls.Add(value, column + 1, row);
        valueLabels[name] = value;
    }

    private static Control CreateDetailBlock(string title, Color background, Color titleColor, Label detailLabel)
    {
        Panel panel = new() { Dock = DockStyle.Fill, BackColor = background, BorderStyle = BorderStyle.FixedSingle, Margin = new Padding(2, 0, 2, 0), Padding = new Padding(10, 7, 10, 7) };
        Label titleLabel = new() { Text = title, Dock = DockStyle.Top, Height = 23, Font = new Font("Segoe UI", 9f, FontStyle.Bold), ForeColor = titleColor };
        detailLabel.Dock = DockStyle.Fill;
        detailLabel.Font = new Font("Consolas", 8.8f);
        detailLabel.ForeColor = Color.FromArgb(38, 48, 60);
        detailLabel.Padding = new Padding(0, 2, 0, 0);
        detailLabel.AutoEllipsis = false;
        panel.Controls.Add(detailLabel);
        panel.Controls.Add(titleLabel);
        return panel;
    }

    private void OnFormatChanged()
    {
        if (formatBox.SelectedItem is not FloatFormat format) return;
        descriptionLabel.Text = format.Description;
        layoutLabel.Text = $"布局：1 sign + {format.ExponentBits} exponent + {format.FractionBits} fraction = {format.TotalBits} bits　Bias = {format.ExponentBias}";
        SelectedFormatChanged?.Invoke(this, EventArgs.Empty);
    }

    private void UpdateExplanation(FloatValue value)
    {
        int signBit = value.IsNegative ? 1 : 0;
        string signMultiplier = value.IsNegative ? "−1" : "+1";
        string signName = value.IsNegative ? "负数" : "正数";
        string exponentBits = Convert.ToString(value.ExponentField, 2).PadLeft(value.Format.ExponentBits, '0');
        string fractionBits = FractionBits(value);
        double fractionScale = Math.ScaleB(1d, value.Format.FractionBits);
        double fractionValue = value.FractionField / fractionScale;

        signDetailLabel.Text = $"原始值：s = {signBit}\r\n符号结果：(-1)^{signBit} = {signMultiplier}\r\n因此该数为：{signName}";

        switch (value.Category)
        {
            case FloatCategory.Normal:
                int actualExponent = value.UnbiasedExponent!.Value;
                double exponentResult = Math.ScaleB(1d, actualExponent);
                double significand = 1d + fractionValue;
                exponentDetailLabel.Text = $"原始位：{exponentBits}₂ = {value.ExponentField}\r\n实际指数：{value.ExponentField} − Bias {value.Format.ExponentBias} = {actualExponent}\r\n指数结果：2^{actualExponent} = {FormatNumber(exponentResult)}";
                precisionDetailLabel.Text = $"原始位：{fractionBits}₂ = {value.FractionField}\r\n小数部分：{value.FractionField} / 2^{value.Format.FractionBits} = {FormatNumber(fractionValue)}\r\n有效数字：1 + {FormatNumber(fractionValue)} = {FormatNumber(significand)}";
                break;
            case FloatCategory.Subnormal:
                int subnormalExponent = value.Format.MinimumNormalExponent;
                double subnormalExponentResult = Math.ScaleB(1d, subnormalExponent);
                exponentDetailLabel.Text = $"原始位：{exponentBits}₂ = 0（Subnormal 标记）\r\n实际指数：1 − Bias {value.Format.ExponentBias} = {subnormalExponent}\r\n指数结果：2^{subnormalExponent} = {FormatNumber(subnormalExponentResult)}";
                precisionDetailLabel.Text = $"原始位：{fractionBits}₂ = {value.FractionField}\r\n没有隐藏的 1\r\n有效数字：{value.FractionField} / 2^{value.Format.FractionBits} = {FormatNumber(fractionValue)}";
                break;
            case FloatCategory.Zero:
                exponentDetailLabel.Text = $"原始位：{exponentBits}₂ = 0\r\n指数全 0，Fraction 也全 0\r\n该编码表示零，不计算实际指数";
                precisionDetailLabel.Text = $"原始位：{fractionBits}₂ = 0\r\n有效数字：0\r\n数据精度结果：0";
                break;
            case FloatCategory.Infinity:
                exponentDetailLabel.Text = $"原始位：{exponentBits}₂ = {value.ExponentField}\r\n指数位全 1\r\n这是 Infinity 特殊编码，不减 Bias";
                precisionDetailLabel.Text = $"原始位：{fractionBits}₂ = 0\r\nFraction 全 0\r\n与全 1 指数共同表示 Infinity";
                break;
            default:
                exponentDetailLabel.Text = $"原始位：{exponentBits}₂ = {value.ExponentField}\r\n指数位全 1\r\n这是 NaN 特殊编码，不计算实际指数";
                precisionDetailLabel.Text = $"原始位：{fractionBits}₂ = {value.FractionField}\r\nFraction 非特殊零值\r\n该 payload 表示 NaN，不是普通数值精度";
                break;
        }
    }

    private static string FractionBits(FloatValue value) => Convert.ToString((long)value.FractionField, 2).PadLeft(value.Format.FractionBits, '0');

    internal static string FormatNumber(double value)
    {
        if (double.IsNaN(value)) return "NaN";
        if (double.IsPositiveInfinity(value)) return "+Infinity";
        if (double.IsNegativeInfinity(value)) return "−Infinity";
        if (value == 0d && BitConverter.DoubleToInt64Bits(value) < 0) return "−0";
        return value.ToString("G17", CultureInfo.InvariantCulture);
    }
}
