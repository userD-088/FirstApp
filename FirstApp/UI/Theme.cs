using Microsoft.Maui.Controls.Shapes;

namespace FirstApp.UI;

public sealed class ThemeProfile
{
    public required string Key { get; init; }          // localisation key of the name
    public required string Fallback { get; init; }     // name if the key is missing
    public required Color Accent { get; init; }
    public required Color AccentBorder { get; init; }
    public required Color Chip { get; init; }
    public required Color ChipAlt { get; init; }
    public required Color HighBg { get; init; }
    public required Color Result { get; init; }
    public required Color[] PageBg { get; init; }       // 3 stops, diagonal
    public required Color[] HeaderBg { get; init; }     // 2 stops
    public required Color[] CardBg { get; init; }       // 2 stops, diagonal
    public required Color InputCardBg { get; init; }
    public required Color SheetTop { get; init; }
    public required Color SheetBottom { get; init; }
    public double CardRadius { get; init; } = 26;
    public double ControlRadius { get; init; } = 13;
    public bool HasCyberFx { get; init; }
}

/// <summary>Theme palette + shared static colours.</summary>
public static class Theme
{
    // ── Neutral text / surface colours (theme independent) ──────────
    public static readonly Color TextMuted = Color.FromArgb("#64748B");
    public static readonly Color TextSub = Color.FromArgb("#94A3B8");
    public static readonly Color TextLight = Color.FromArgb("#E2E8F0");
    public static readonly Color TextDim = Color.FromArgb("#475569");
    public static readonly Color TextDimmer = Color.FromArgb("#374151");
    public static readonly Color TextBlue = Color.FromArgb("#93C5FD");
    public static readonly Color TextTan = Color.FromArgb("#D2B48C");
    public static readonly Color TextSteel = Color.FromArgb("#6BA3BE");
    public static readonly Color TextYellow = Color.FromArgb("#FBBF24");
    public static readonly Color TextGreen = Color.FromArgb("#10B981");
    public static readonly Color Error = Color.FromArgb("#F87171");
    public static readonly Color PanelDark = Color.FromArgb("#060D1A");
    public static readonly Color RowDark1 = Color.FromArgb("#0D1520");
    public static readonly Color RowDark2 = Color.FromArgb("#070F1A");
    public static readonly Color Divider = Color.FromArgb("#1E293B");
    public static readonly Color ChipEmpty = Color.FromArgb("#2A2A2A");
    public static readonly Color BorderNone = Color.FromArgb("#444444");

    public static readonly ThemeProfile[] All =
    {
        new()
        {
            Key = "themeDefault", Fallback = "Default",
            Accent = C("#2563EB"), AccentBorder = C("#1E3A5F"), Chip = C("#1F2937"), ChipAlt = C("#1A2535"),
            HighBg = C("#0E1F3A"), Result = C("#10B981"),
            PageBg = new[] { C("#000000"), C("#1A2A3F"), C("#203A53") },
            HeaderBg = new[] { C("#0F172A"), C("#1E293B") },
            CardBg = new[] { C("#020617"), C("#0F172A") },
            InputCardBg = C("#0D1520"), SheetTop = C("#0F172A"), SheetBottom = C("#1A2A3F"),
        },
        new()
        {
            Key = "themeCyber", Fallback = "Cyber",
            Accent = C("#00E5FF"), AccentBorder = C("#005066"), Chip = C("#041520"), ChipAlt = C("#031018"),
            HighBg = C("#021828"), Result = C("#00E5FF"),
            PageBg = new[] { C("#000000"), C("#000D1A"), C("#001525") },
            HeaderBg = new[] { C("#000508"), C("#000F1C") },
            CardBg = new[] { C("#000305"), C("#000A14") },
            InputCardBg = C("#000810"), SheetTop = C("#000A12"), SheetBottom = C("#001320"),
            CardRadius = 6, ControlRadius = 4, HasCyberFx = true,
        },
        new()
        {
            Key = "themePlasma", Fallback = "Plasma",
            Accent = C("#8B5CF6"), AccentBorder = C("#3D1F7A"), Chip = C("#1A0F30"), ChipAlt = C("#150C28"),
            HighBg = C("#1F0F40"), Result = C("#C4B5FD"),
            PageBg = new[] { C("#050010"), C("#0F0525"), C("#160830") },
            HeaderBg = new[] { C("#080018"), C("#120228") },
            CardBg = new[] { C("#030010"), C("#08001C") },
            InputCardBg = C("#0A0018"), SheetTop = C("#0C0220"), SheetBottom = C("#170833"),
        },
        new()
        {
            Key = "themeAmoled", Fallback = "AMOLED",
            Accent = C("#14B8A6"), AccentBorder = C("#134E4A"), Chip = C("#111111"), ChipAlt = C("#0A0A0A"),
            HighBg = C("#042F2E"), Result = C("#2DD4BF"),
            PageBg = new[] { C("#000000"), C("#000000"), C("#000000") },
            HeaderBg = new[] { C("#000000"), C("#000000") },
            CardBg = new[] { C("#000000"), C("#000000") },
            InputCardBg = C("#000000"), SheetTop = C("#050505"), SheetBottom = C("#000000"),
        },
    };

    public static int Index { get; private set; }
    public static ThemeProfile Current { get; private set; } = All[0];

    public static void Set(int index)
    {
        Index = Math.Clamp(index, 0, All.Length - 1);
        Current = All[Index];
    }

    // ── Helpers ─────────────────────────────────────────────────────
    private static Color C(string hex) => Color.FromArgb(hex);

    public static LinearGradientBrush Gradient(Color[] stops, bool diagonal)
    {
        var coll = new GradientStopCollection();
        for (int i = 0; i < stops.Length; i++)
            coll.Add(new GradientStop(stops[i], stops.Length == 1 ? 0 : (float)i / (stops.Length - 1)));
        return new LinearGradientBrush(coll, new Point(0, 0), diagonal ? new Point(1, 1) : new Point(0, 1));
    }

    public static RoundRectangle Round(double r) => new() { CornerRadius = new CornerRadius(r) };
}
