using System.ComponentModel;
using FloatExplorer.Core;

namespace FloatExplorer;

public sealed class FloatingPointFormulaControl : Control
{
    private FloatValue? value;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public FloatValue? Value
    {
        get => value;
        set
        {
            this.value = value;
            Invalidate();
        }
    }

    public FloatingPointFormulaControl()
    {
        DoubleBuffered = true;
        BackColor = Color.FromArgb(237, 246, 255);
        ForeColor = Color.FromArgb(25, 66, 101);
        SetStyle(ControlStyles.ResizeRedraw, true);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        if (value is null) return;

        e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
        using SolidBrush accentBrush = new(Color.FromArgb(22, 119, 210));
        e.Graphics.FillRectangle(accentBrush, 0, 0, 4, ClientSize.Height);
        if (value.Category is FloatCategory.Zero or FloatCategory.Infinity or FloatCategory.NaN)
        {
            DrawSpecialValue(e.Graphics, value);
            return;
        }

        DrawSymbolicFormula(e.Graphics, value);
        using Font calculationFont = new("Consolas", 9.3f);
        using SolidBrush calculationBrush = new(ForeColor);
        e.Graphics.DrawString(NumericExpansion(value), calculationFont, calculationBrush, new RectangleF(20f, 59f, ClientSize.Width - 34f, ClientSize.Height - 61f));
    }

    private void DrawSymbolicFormula(Graphics graphics, FloatValue current)
    {
        using Font mainFont = new("Cambria Math", 17f, FontStyle.Italic);
        using Font fractionFont = new("Cambria Math", 12.5f, FontStyle.Italic);
        using Font superscriptFont = new("Cambria Math", 10f, FontStyle.Italic);
        using SolidBrush brush = new(ForeColor);
        using Pen fractionPen = new(ForeColor, 1.25f);

        float x = 20f;
        const float y = 10f;
        x = DrawText(graphics, "Value = (−1)", mainFont, brush, x, y);
        x = DrawSuperscript(graphics, "S", superscriptFont, brush, x - 2f, y - 1f) + 5f;
        x = DrawText(graphics, " × ", mainFont, brush, x, y);
        x = DrawText(graphics, current.Category == FloatCategory.Normal ? "(1 + " : "(", mainFont, brush, x, y);
        x = DrawFraction(graphics, "Fraction", $"2^{current.Format.FractionBits}", fractionFont, superscriptFont, brush, fractionPen, x, y - 4f);
        x = DrawText(graphics, ") × 2", mainFont, brush, x + 3f, y);
        DrawSuperscript(graphics, current.Category == FloatCategory.Normal ? $"Exponent − {current.Format.ExponentBias}" : $"1 − {current.Format.ExponentBias}", superscriptFont, brush, x - 2f, y - 1f);
    }

    private static float DrawFraction(Graphics graphics, string numerator, string denominator, Font fractionFont, Font superscriptFont, Brush brush, Pen linePen, float x, float y)
    {
        SizeF numeratorSize = graphics.MeasureString(numerator, fractionFont);
        string denominatorBase = denominator[..denominator.IndexOf('^')];
        string denominatorExponent = denominator[(denominator.IndexOf('^') + 1)..];
        SizeF denominatorBaseSize = graphics.MeasureString(denominatorBase, fractionFont);
        SizeF denominatorExponentSize = graphics.MeasureString(denominatorExponent, superscriptFont);
        float denominatorWidth = denominatorBaseSize.Width + denominatorExponentSize.Width - 4f;
        float width = Math.Max(numeratorSize.Width, denominatorWidth) + 10f;
        graphics.DrawString(numerator, fractionFont, brush, x + (width - numeratorSize.Width) / 2f, y);
        graphics.DrawLine(linePen, x + 2f, y + 23f, x + width - 2f, y + 23f);
        float denominatorX = x + (width - denominatorWidth) / 2f;
        graphics.DrawString(denominatorBase, fractionFont, brush, denominatorX, y + 23f);
        graphics.DrawString(denominatorExponent, superscriptFont, brush, denominatorX + denominatorBaseSize.Width - 4f, y + 19f);
        return x + width;
    }

    private static float DrawText(Graphics graphics, string text, Font font, Brush brush, float x, float y)
    {
        graphics.DrawString(text, font, brush, x, y);
        return x + graphics.MeasureString(text, font).Width - 3f;
    }

    private static float DrawSuperscript(Graphics graphics, string text, Font font, Brush brush, float x, float y)
    {
        graphics.DrawString(text, font, brush, x, y);
        return x + graphics.MeasureString(text, font).Width;
    }

    private static string NumericExpansion(FloatValue current)
    {
        int signBit = current.IsNegative ? 1 : 0;
        string signMultiplier = current.IsNegative ? "−1" : "+1";
        double fraction = current.FractionField / Math.ScaleB(1d, current.Format.FractionBits);
        int exponent = current.Category == FloatCategory.Normal ? current.UnbiasedExponent!.Value : current.Format.MinimumNormalExponent;
        double exponentValue = Math.ScaleB(1d, exponent);
        double significand = current.Category == FloatCategory.Normal ? 1d + fraction : fraction;
        string hiddenBit = current.Category == FloatCategory.Normal ? "1 + " : string.Empty;
        string exponentExpression = current.Category == FloatCategory.Normal ? $"{current.ExponentField} − {current.Format.ExponentBias}" : $"1 − {current.Format.ExponentBias}";
        return $"代入：(-1)^{signBit} × ({hiddenBit}{current.FractionField} / 2^{current.Format.FractionBits}) × 2^({exponentExpression}) = {signMultiplier} × {FormatViewControl.FormatNumber(significand)} × {FormatViewControl.FormatNumber(exponentValue)} = {FormatViewControl.FormatNumber(current.Value)}";
    }

    private void DrawSpecialValue(Graphics graphics, FloatValue current)
    {
        using Font titleFont = new("Cambria Math", 16f, FontStyle.Italic);
        using Font detailFont = new("Segoe UI", 9.5f);
        using SolidBrush brush = new(ForeColor);
        string title = current.Category switch
        {
            FloatCategory.Zero => $"Value = {(current.IsNegative ? "−0" : "+0")}",
            FloatCategory.Infinity => $"Value = {(current.IsNegative ? "−∞" : "+∞")}",
            _ => "Value = NaN"
        };
        string detail = current.Category switch
        {
            FloatCategory.Zero => "Exponent 和 Fraction 全为 0，结果为带符号的零。",
            FloatCategory.Infinity => "Exponent 全为 1 且 Fraction 为 0，这是 Infinity 特殊编码。",
            _ => "这是 NaN 特殊编码，不能代入普通浮点数计算公式。"
        };
        graphics.DrawString(title, titleFont, brush, 20f, 10f);
        graphics.DrawString(detail, detailFont, brush, 20f, 50f);
    }
}
