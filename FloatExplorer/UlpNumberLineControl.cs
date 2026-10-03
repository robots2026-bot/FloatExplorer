using System.ComponentModel;
using FloatExplorer.Core;

namespace FloatExplorer;

public sealed class UlpNumberLineControl : Control
{
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public FloatValue? A { get; set; }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public double InputValue { get; set; }

    public UlpNumberLineControl()
    {
        DoubleBuffered = true;
        Height = 88;
        SetStyle(ControlStyles.ResizeRedraw, true);
    }

    public void SetValue(FloatValue quantized, double inputValue)
    {
        A = quantized;
        InputValue = inputValue;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        if (A is null)
        {
            TextRenderer.DrawText(e.Graphics, "暂无可比较数据", Font, ClientRectangle, ForeColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            return;
        }

        if (!double.IsFinite(A.Value) || !double.IsFinite(InputValue) || !double.IsFinite(A.Ulp) || A.Ulp <= 0d)
        {
            TextRenderer.DrawText(e.Graphics, "特殊值不适用相邻 ULP 数轴", Font, ClientRectangle, ForeColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            return;
        }

        int left = 42;
        int right = ClientSize.Width - 42;
        int y = Math.Max(42, ClientSize.Height / 2);
        using SolidBrush rangeBrush = new(Color.FromArgb(240, 246, 252));
        e.Graphics.FillRectangle(rangeBrush, left, y - 12, Math.Max(1, right - left), 24);
        using Pen axisPen = new(Color.FromArgb(103, 116, 131), 2f);
        e.Graphics.DrawLine(axisPen, left, y, right, y);

        double previous = FloatCodec.PreviousRepresentable(A);
        double next = FloatCodec.NextRepresentable(A);
        DrawTick(e.Graphics, left, y, previous);
        DrawTick(e.Graphics, (left + right) / 2, y, A.Value);
        DrawTick(e.Graphics, right, y, next);

        double span = next - previous;
        double position = span > 0d ? (InputValue - previous) / span : 0.5d;
        int bx = left + (int)Math.Round(Math.Clamp(position, 0d, 1d) * (right - left));
        using SolidBrush brush = new(Color.FromArgb(234, 116, 34));
        e.Graphics.FillEllipse(brush, bx - 7, y - 7, 14, 14);
        TextRenderer.DrawText(e.Graphics, "输入", Font, new Rectangle(bx - 22, y - 29, 44, 18), Color.FromArgb(184, 79, 17), TextFormatFlags.HorizontalCenter);

        using SolidBrush aBrush = new(Color.FromArgb(22, 119, 210));
        int ax = (left + right) / 2;
        e.Graphics.FillEllipse(aBrush, ax - 5, y - 5, 10, 10);
        TextRenderer.DrawText(e.Graphics, "量化", Font, new Rectangle(ax - 24, y - 31, 48, 18), Color.FromArgb(18, 94, 151), TextFormatFlags.HorizontalCenter);
    }

    private void DrawTick(Graphics graphics, int x, int y, double value)
    {
        using Pen pen = new(Color.FromArgb(103, 116, 131));
        graphics.DrawLine(pen, x, y - 7, x, y + 7);
        string text = FormatViewControl.FormatNumber(value);
        Rectangle bounds = new(Math.Max(0, x - 92), y + 10, 184, 22);
        using Font labelFont = new("Consolas", 8f);
        TextRenderer.DrawText(graphics, text, labelFont, bounds, ForeColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.EndEllipsis);
    }
}
