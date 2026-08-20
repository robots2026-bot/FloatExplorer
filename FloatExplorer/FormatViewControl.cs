using System.Globalization;
using FloatExplorer.Core;

namespace FloatExplorer;

public sealed class FormatViewControl : UserControl
{
    private readonly ComboBox formatBox = new();
    private readonly Label layoutLabel = new();
    private readonly Label descriptionLabel = new();
    private readonly BitFieldControl bitField = new();
    private readonly Label equationLabel = new();
    private readonly TableLayoutPanel properties = new();
    private readonly Dictionary<string, Label> valueLabels = [];

    public event EventHandler? SelectedFormatChanged;
    public event EventHandler<FloatValue>? ValueEdited;

    public FloatFormat SelectedFormat => (FloatFormat)formatBox.SelectedItem!;
    public FloatValue? Value { get; private set; }

    public FormatViewControl(string caption, string defaultFormatId)
    {
        Dock = DockStyle.Fill;
        Padding = new Padding(12);
        BackColor = Color.White;
        BorderStyle = BorderStyle.FixedSingle;

        TableLayoutPanel root = new() { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 7 };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 90));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 8));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));

        Panel header = new() { Dock = DockStyle.Fill };
        Label captionLabel = new() { Text = caption, AutoSize = true, Font = new Font("Segoe UI", 11f, FontStyle.Bold), Location = new Point(0, 7) };
        formatBox.DropDownStyle = ComboBoxStyle.DropDownList;
        formatBox.DataSource = FloatFormat.All.ToList();
        formatBox.DisplayMember = nameof(FloatFormat.DisplayName);
        formatBox.Width = 180;
        formatBox.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        formatBox.Location = new Point(Math.Max(190, Width - formatBox.Width), 2);
        formatBox.SelectedIndexChanged += (_, _) => OnFormatChanged();
        header.Controls.Add(captionLabel);
        header.Controls.Add(formatBox);
        header.Resize += (_, _) => formatBox.Left = header.ClientSize.Width - formatBox.Width;

        descriptionLabel.Dock = DockStyle.Fill;
        descriptionLabel.ForeColor = Color.FromArgb(76, 91, 108);
        descriptionLabel.Font = new Font("Segoe UI", 8.5f);
        descriptionLabel.AutoEllipsis = true;
        descriptionLabel.Padding = new Padding(0, 4, 0, 0);

        bitField.Dock = DockStyle.Fill;
        bitField.RawBitsChanged += (_, raw) =>
        {
            FloatValue edited = FloatCodec.Decode(raw, SelectedFormat);
            SetValue(edited);
            ValueEdited?.Invoke(this, edited);
        };

        equationLabel.Dock = DockStyle.Fill;
        equationLabel.Font = new Font("Consolas", 9.5f);
        equationLabel.BackColor = Color.FromArgb(237, 246, 255);
        equationLabel.ForeColor = Color.FromArgb(28, 74, 112);
        equationLabel.Padding = new Padding(8, 7, 8, 4);
        equationLabel.AutoEllipsis = true;

        properties.Dock = DockStyle.Fill;
        properties.ColumnCount = 4;
        properties.RowCount = 4;
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
        layoutLabel.Font = new Font("Segoe UI", 8f);
        layoutLabel.TextAlign = ContentAlignment.BottomLeft;

        root.Controls.Add(header, 0, 0);
        root.Controls.Add(descriptionLabel, 0, 1);
        root.Controls.Add(bitField, 0, 2);
        root.Controls.Add(equationLabel, 0, 3);
        root.Controls.Add(new Panel(), 0, 4);
        root.Controls.Add(properties, 0, 5);
        root.Controls.Add(layoutLabel, 0, 6);
        Controls.Add(root);

        formatBox.SelectedItem = FloatFormat.All.Single(format => format.Id == defaultFormatId);
        OnFormatChanged();
    }

    public void SetValue(FloatValue value)
    {
        Value = value;
        bitField.Value = value;
        equationLabel.Text = Equation(value);
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
        if (index >= 0) formatBox.SelectedIndex = index;
    }

    private void AddProperty(string name, int column, int row)
    {
        Label key = new() { Text = name, Dock = DockStyle.Fill, ForeColor = Color.FromArgb(95, 106, 119), TextAlign = ContentAlignment.MiddleLeft, Font = new Font("Segoe UI", 8.5f) };
        Label value = new() { Dock = DockStyle.Fill, ForeColor = Color.FromArgb(18, 94, 151), TextAlign = ContentAlignment.MiddleLeft, Font = new Font("Consolas", 8.5f), AutoEllipsis = true };
        properties.Controls.Add(key, column, row);
        properties.Controls.Add(value, column + 1, row);
        valueLabels[name] = value;
    }

    private void OnFormatChanged()
    {
        if (formatBox.SelectedItem is not FloatFormat format) return;
        descriptionLabel.Text = format.Description;
        layoutLabel.Text = $"布局：1 sign + {format.ExponentBits} exponent + {format.FractionBits} fraction = {format.TotalBits} bits　Bias = {format.ExponentBias}";
        SelectedFormatChanged?.Invoke(this, EventArgs.Empty);
    }

    private static string Equation(FloatValue value)
    {
        string sign = value.IsNegative ? "−" : "+";
        return value.Category switch
        {
            FloatCategory.Zero => $"{sign}0",
            FloatCategory.Infinity => $"{sign}∞（指数全 1，Fraction 全 0）",
            FloatCategory.NaN => "NaN（特殊编码，不表示实数）",
            FloatCategory.Subnormal => $"{sign}0.{FractionBits(value)}₂ × 2^({value.Format.MinimumNormalExponent}) = {FormatNumber(value.Value)}",
            _ => $"{sign}1.{FractionBits(value)}₂ × 2^({value.UnbiasedExponent}) = {FormatNumber(value.Value)}"
        };
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
