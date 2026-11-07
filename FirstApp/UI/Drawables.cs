using FirstApp.Core;

namespace FirstApp.UI;

/// <summary>Draws the resistor. Scales with the available width; allocation-free per frame.</summary>
public sealed class ResistorDrawable : IDrawable
{
    private readonly ResistorViewModel _vm;
    public ResistorDrawable(ResistorViewModel vm) => _vm = vm;

    private static readonly Color Shadow = Colors.Black.WithAlpha(0.28f);
    private static readonly Color Gloss = Colors.White.WithAlpha(0.16f);
    private static readonly Color BandShade = Colors.Black.WithAlpha(0.18f);
    private static readonly Color LeadLight = Color.FromArgb("#D8DDE3");
    private static readonly Color LeadDark = Color.FromArgb("#8A939C");
    private static readonly Color NoneOutline = Colors.White.WithAlpha(0.10f);
    private static readonly float[] Dash = { 3, 3 };

    public void Draw(ICanvas canvas, RectF rect)
    {
        // Reference geometry is 340 × 90, scaled to fit
        float scale = Math.Min(rect.Width / 340f, rect.Height / 90f);
        scale = Math.Clamp(scale, 0.5f, 1.6f);

        float bw = 210f * scale, bh = 56f * scale, rw = 13f * scale;
        float gap = 25f * scale, groupExtra = 16f * scale, lead = 60f * scale;
        float cx = rect.Center.X, cy = rect.Center.Y;
        float bx = cx - bw / 2, by = cy - bh / 2;
        float radius = bh / 2;

        // Leads
        canvas.StrokeLineCap = LineCap.Round;
        canvas.StrokeSize = 5f * scale;
        canvas.StrokeColor = LeadDark;
        canvas.DrawLine(bx - lead, cy + 1, bx + 4, cy + 1);
        canvas.DrawLine(bx + bw - 4, cy + 1, bx + bw + lead, cy + 1);
        canvas.StrokeSize = 2f * scale;
        canvas.StrokeColor = LeadLight;
        canvas.DrawLine(bx - lead, cy - 1, bx + 4, cy - 1);
        canvas.DrawLine(bx + bw - 4, cy - 1, bx + bw + lead, cy - 1);

        // Body + drop shadow
        canvas.FillColor = Shadow;
        canvas.FillRoundedRectangle(bx, by + 5 * scale, bw, bh, radius);
        canvas.FillColor = _vm.BodyColor;
        canvas.FillRoundedRectangle(bx, by, bw, bh, radius);

        // Bands
        var rings = _vm.Rings;
        int count = rings.Count;
        int groupAt = count == 6 ? 4 : count - 1;   // tolerance band sits apart
        float totalW = (count - 1) * gap + rw + groupExtra;
        float x = cx - totalW / 2;
        for (int i = 0; i < count; i++)
        {
            if (i == groupAt) x += groupExtra;
            var c = rings[i];
            if (c.IsNone)
            {
                canvas.StrokeColor = NoneOutline;
                canvas.StrokeSize = 1;
                canvas.StrokeDashPattern = Dash;
                canvas.DrawRectangle(x, by + 2, rw, bh - 4);
                canvas.StrokeDashPattern = null;
            }
            else
            {
                canvas.FillColor = c.Bg;
                canvas.FillRectangle(x, by, rw, bh);
                canvas.FillColor = BandShade;
                canvas.FillRectangle(x, by + bh * 0.72f, rw, bh * 0.28f);
            }
            x += gap;
        }

        // Gloss highlight on top of everything
        canvas.FillColor = Gloss;
        canvas.FillRoundedRectangle(bx + 10 * scale, by + 6 * scale, bw - 20 * scale, bh / 3.2f, bh / 6.4f);
    }
}

/// <summary>Static scanlines. Movement is done by translating the view (GPU), not by redrawing.</summary>
public sealed class ScanlinesDrawable : IDrawable
{
    public const float Spacing = 4f;
    private static readonly Color Line = Color.FromRgba(0, 230, 255, 16);

    public void Draw(ICanvas canvas, RectF rect)
    {
        canvas.StrokeColor = Line;
        canvas.StrokeSize = 1f;
        for (float y = 0; y < rect.Height; y += Spacing)
            canvas.DrawLine(0, y, rect.Width, y);
    }
}

/// <summary>HUD corner brackets drawn inside a card (Cyber theme).</summary>
public sealed class CornerBracketDrawable : IDrawable
{
    public Color Accent { get; set; } = Color.FromArgb("#00E5FF");

    public void Draw(ICanvas canvas, RectF rect)
    {
        const float size = 16f, inset = 3f;
        float r = rect.Width - inset, b = rect.Height - inset;
        canvas.StrokeColor = Accent;
        canvas.StrokeSize = 2f;
        canvas.StrokeLineCap = LineCap.Square;
        canvas.DrawLine(inset, inset, inset + size, inset); canvas.DrawLine(inset, inset, inset, inset + size);
        canvas.DrawLine(r, inset, r - size, inset); canvas.DrawLine(r, inset, r, inset + size);
        canvas.DrawLine(inset, b, inset + size, b); canvas.DrawLine(inset, b, inset, b - size);
        canvas.DrawLine(r, b, r - size, b); canvas.DrawLine(r, b, r, b - size);
    }
}
