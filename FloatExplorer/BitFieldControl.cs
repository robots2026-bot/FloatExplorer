using System.ComponentModel;
using FloatExplorer.Core;

namespace FloatExplorer;

public sealed class BitFieldControl : Control
{
    private FloatValue? value;
    private int hoveredCell = -1;
    private readonly ToolTip bitToolTip = new() { InitialDelay = 250, ReshowDelay = 100, AutoPopDelay = 5000 };

    public event EventHandler<ulong>? RawBitsChanged;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public FloatValue? Value
    {
        get => value;
        set
        {
            this.value = value;
            hoveredCell = -1;
            Invalidate();
        }
    }

    public BitFieldControl()
    {
        DoubleBuffered = true;
        SetStyle(ControlStyles.ResizeRedraw | ControlStyles.Selectable, true);
        Height = 86;
        MinimumSize = new Size(260, 78);
        Cursor = Cursors.Hand;
        TabStop = true;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        if (value is null) return;

        e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        int count = value.Format.TotalBits;
        float gap = count > 20 ? 2f : 4f;
        float cellWidth = Math.Max(9f, (ClientSize.Width - gap * (count - 1)) / count);
        float contentWidth = count * cellWidth + (count - 1) * gap;
        float startX = Math.Max(0f, (ClientSize.Width - contentWidth) / 2f);
        const float labelY = 2f;
        const float bitY = 20f;
        const float bitHeight = 34f;

        DrawFieldLabel(e.Graphics, "S", startX, cellWidth, labelY, SignColor);
        DrawFieldLabel(e.Graphics, "Exponent", startX + cellWidth + gap, value.Format.ExponentBits * cellWidth + (value.Format.ExponentBits - 1) * gap, labelY, ExponentColor);
        float fractionX = startX + (1 + value.Format.ExponentBits) * (cellWidth + gap);
        DrawFieldLabel(e.Graphics, "Fraction", fractionX, value.Format.FractionBits * cellWidth + (value.Format.FractionBits - 1) * gap, labelY, FractionColor);

        string bits = value.BitString;
        using Font bitFont = new("Consolas", count > 20 ? 9f : 11f, FontStyle.Bold);
        using Font indexFont = new("Consolas", 7f);
        for (int i = 0; i < count; i++)
        {
            RectangleF rect = new(startX + i * (cellWidth + gap), bitY, cellWidth, bitHeight);
            Color fill = FieldColor(i, value.Format);
            if (i == hoveredCell) fill = ControlPaint.Light(fill, 0.18f);
            using SolidBrush brush = new(fill);
            using Pen border = new(i == hoveredCell ? Color.DodgerBlue : ControlPaint.Dark(fill, 0.18f), i == hoveredCell ? 2f : 1f);
            e.Graphics.FillRoundedRectangle(brush, rect, 4f);
            e.Graphics.DrawRoundedRectangle(border, rect, 4f);
            TextRenderer.DrawText(e.Graphics, bits[i].ToString(), bitFont, Rectangle.Round(rect), Color.FromArgb(25, 32, 42), TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
            TextRenderer.DrawText(e.Graphics, (count - 1 - i).ToString(), indexFont, new Rectangle((int)rect.X, (int)(bitY + bitHeight + 3), (int)rect.Width, 16), ForeColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.NoPadding);
        }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        int hit = HitTest(e.Location);
        if (hit == hoveredCell) return;
        hoveredCell = hit;
        bitToolTip.SetToolTip(this, hit >= 0 && value is not null ? BitHint(hit, value.Format) : null);
        Invalidate();
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        hoveredCell = -1;
        bitToolTip.SetToolTip(this, null);
        Invalidate();
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        Focus();
        if (e.Button != MouseButtons.Left || value is null) return;
        int cell = HitTest(e.Location);
        if (cell < 0) return;
        int bitIndex = value.Format.TotalBits - 1 - cell;
        RawBitsChanged?.Invoke(this, value.RawBits ^ (1UL << bitIndex));
    }

    protected override bool IsInputKey(Keys keyData) => keyData is Keys.Left or Keys.Right or Keys.Space || base.IsInputKey(keyData);

    protected override void Dispose(bool disposing)
    {
        if (disposing) bitToolTip.Dispose();
        base.Dispose(disposing);
    }

    private int HitTest(Point point)
    {
        if (value is null || point.Y is < 20 or > 54) return -1;
        int count = value.Format.TotalBits;
        float gap = count > 20 ? 2f : 4f;
        float cellWidth = Math.Max(9f, (ClientSize.Width - gap * (count - 1)) / count);
        float contentWidth = count * cellWidth + (count - 1) * gap;
        float startX = Math.Max(0f, (ClientSize.Width - contentWidth) / 2f);
        int cell = (int)((point.X - startX) / (cellWidth + gap));
        if (cell < 0 || cell >= count) return -1;
        float localX = point.X - (startX + cell * (cellWidth + gap));
        return localX <= cellWidth ? cell : -1;
    }

    private static Color FieldColor(int cell, FloatFormat format) => cell == 0 ? SignColor : cell <= format.ExponentBits ? ExponentColor : FractionColor;

    private static string BitHint(int cell, FloatFormat format)
    {
        int bitIndex = format.TotalBits - 1 - cell;
        string field = cell == 0 ? "符号位 Sign" : cell <= format.ExponentBits ? "指数位 Exponent" : "精度位 Fraction";
        return $"位 {bitIndex} · {field} · 单击翻转";
    }

    private static void DrawFieldLabel(Graphics graphics, string text, float x, float width, float y, Color color)
    {
        using Font font = new("Segoe UI", 8f, FontStyle.Bold);
        TextRenderer.DrawText(graphics, text, font, new Rectangle((int)x, (int)y, (int)width, 16), ControlPaint.Dark(color, 0.25f), TextFormatFlags.HorizontalCenter | TextFormatFlags.NoPadding);
    }

    private static readonly Color SignColor = Color.FromArgb(255, 142, 153);
    private static readonly Color ExponentColor = Color.FromArgb(255, 210, 112);
    private static readonly Color FractionColor = Color.FromArgb(131, 220, 242);
}

internal static class GraphicsExtensions
{
    public static void FillRoundedRectangle(this Graphics graphics, Brush brush, RectangleF bounds, float radius)
    {
        using System.Drawing.Drawing2D.GraphicsPath path = RoundedPath(bounds, radius);
        graphics.FillPath(brush, path);
    }

    public static void DrawRoundedRectangle(this Graphics graphics, Pen pen, RectangleF bounds, float radius)
    {
        using System.Drawing.Drawing2D.GraphicsPath path = RoundedPath(bounds, radius);
        graphics.DrawPath(pen, path);
    }

    private static System.Drawing.Drawing2D.GraphicsPath RoundedPath(RectangleF bounds, float radius)
    {
        float diameter = radius * 2f;
        System.Drawing.Drawing2D.GraphicsPath path = new();
        path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }
}
