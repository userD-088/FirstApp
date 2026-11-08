using Microsoft.Maui.Controls.Shapes;

namespace FirstApp.UI;

/// <summary>Complete colour/shape set of one design. Every colour of the UI comes from here.</summary>
public sealed class ThemeProfile
{
    public required string Key { get; init; }          // localisation key of the name
    public required string Fallback { get; init; }     // name if the key is missing
    public bool IsLight { get; init; }
    public bool HasCyberFx { get; init; }
    public string? FontFamily { get; init; }           // null = system default

    // Accent
    public required Color Accent { get; init; }
    public Brush? AccentGradient { get; init; }        // optional: active tabs / buttons
    public required Color OnAccent { get; init; }      // text on accent
    public required Color AccentBorder { get; init; }  // control outlines

    // Backgrounds
    public required Color[] PageBg { get; init; }      // 3 stops, diagonal
    public required Color[] HeaderBg { get; init; }    // 2 stops
    public required Color[] CardBg { get; init; }      // 2 stops, diagonal
    public required Color InputCardBg { get; init; }
    public Brush? CardStroke { get; init; }            // null → Accent
    public double CardStrokeThickness { get; init; } = 1.5;
    public bool CardShadow { get; init; }

    // Surfaces
    public required Color Surface { get; init; }       // segmented control background
    public required Color Field { get; init; }         // entry background
    public required Color Chip { get; init; }
    public required Color ChipAlt { get; init; }
    public required Color ChipEmpty { get; init; }     // "no band" chip
    public required Color HighBg { get; init; }        // highlighted card
    public required Color ChipOutline { get; init; }   // unselected colour chips
    public required Color Selection { get; init; }     // selected colour chip outline
    public required Color HeaderButton { get; init; }

    // Text
    public required Color TextPrimary { get; init; }
    public required Color TextSecondary { get; init; }
    public required Color TextMuted { get; init; }
    public required Color TextDim { get; init; }
    public required Color Placeholder { get; init; }
    public required Color Divider { get; init; }
    public required Color Result { get; init; }
    public required Color Good { get; init; }
    public required Color Warn { get; init; }
    public required Color Info { get; init; }
    public required Color Error { get; init; }

    // Reference table
    public required Color Panel { get; init; }
    public required Color Row1 { get; init; }
    public required Color Row2 { get; init; }

    // Sheet
    public required Color SheetTop { get; init; }
    public required Color SheetBottom { get; init; }
    public required Color Backdrop { get; init; }

    public double CardRadius { get; init; } = 26;
    public double ControlRadius { get; init; } = 13;
    public double ChipRadius { get; init; } = 12;
}

/// <summary>Theme registry. Indices are stored in preferences, so new themes are appended.</summary>
public static class Theme
{
    private static Color C(string hex) => Color.FromArgb(hex);

    public static readonly ThemeProfile[] All =
    {
        // 0 ── Midnight: navy + blue, rounded
        new()
        {
            Key = "themeDefault", Fallback = "Midnight",
            Accent = C("#3B82F6"), OnAccent = Colors.White, AccentBorder = C("#1E3A5F"),
            PageBg = new[] { C("#020617"), C("#0F1E36"), C("#1B3352") },
            HeaderBg = new[] { C("#0B1324"), C("#13203A") },
            CardBg = new[] { C("#0A1428"), C("#0F1B33") },
            InputCardBg = C("#0C1626"),
            CardStroke = new SolidColorBrush(C("#2B4A7A")), CardStrokeThickness = 1,
            Surface = C("#081022"), Field = C("#0F1A2E"), Chip = C("#1A2840"), ChipAlt = C("#14213A"),
            ChipEmpty = C("#2A2A2A"), HighBg = C("#0F2547"), ChipOutline = Colors.Transparent, Selection = Colors.White,
            HeaderButton = C("#1AFFFFFF"),
            TextPrimary = Colors.White, TextSecondary = C("#A5B4CB"), TextMuted = C("#6B7C96"), TextDim = C("#475569"),
            Placeholder = C("#4B5A73"), Divider = C("#1C2B45"),
            Result = C("#34D399"), Good = C("#34D399"), Warn = C("#FBBF24"), Info = C("#93C5FD"), Error = C("#F87171"),
            Panel = C("#081022"), Row1 = C("#0E1A30"), Row2 = C("#0A1426"),
            SheetTop = C("#0E1A30"), SheetBottom = C("#14243F"), Backdrop = C("#B3000000"),
        },
        // 1 ── Cyber: black + neon cyan, square, monospace, scanlines
        new()
        {
            Key = "themeCyber", Fallback = "Cyber", HasCyberFx = true, FontFamily = "monospace",
            Accent = C("#00E5FF"), OnAccent = Colors.Black, AccentBorder = C("#00515E"),
            PageBg = new[] { C("#000000"), C("#00080D"), C("#001218") },
            HeaderBg = new[] { C("#000000"), C("#000A10") },
            CardBg = new[] { C("#000000"), C("#00070B") },
            InputCardBg = C("#000508"), CardStrokeThickness = 2,
            Surface = C("#000A0F"), Field = C("#00090E"), Chip = C("#03161D"), ChipAlt = C("#021015"),
            ChipEmpty = C("#151515"), HighBg = C("#002630"), ChipOutline = Colors.Transparent, Selection = C("#00E5FF"),
            HeaderButton = C("#1A00E5FF"),
            TextPrimary = C("#D8FBFF"), TextSecondary = C("#5FD8E6"), TextMuted = C("#2C8A96"), TextDim = C("#1D5D66"),
            Placeholder = C("#1D5D66"), Divider = C("#003A44"),
            Result = C("#00E5FF"), Good = C("#39FF88"), Warn = C("#FFE14D"), Info = C("#00E5FF"), Error = C("#FF4D6D"),
            Panel = C("#00080C"), Row1 = C("#001017"), Row2 = C("#000A0F"),
            SheetTop = C("#00080C"), SheetBottom = C("#001118"), Backdrop = C("#CC000000"),
            CardRadius = 4, ControlRadius = 2, ChipRadius = 2,
        },
        // 2 ── Plasma: violet → pink, glass cards, gradient outlines, very round
        new()
        {
            Key = "themePlasma", Fallback = "Plasma",
            Accent = C("#D946EF"), OnAccent = Colors.White, AccentBorder = C("#5B2A86"),
            AccentGradient = new LinearGradientBrush(new GradientStopCollection
                { new GradientStop(C("#8B5CF6"), 0), new GradientStop(C("#EC4899"), 1) }, new Point(0, 0), new Point(1, 0)),
            PageBg = new[] { C("#1E0B3A"), C("#3B0F5C"), C("#6B1258") },
            HeaderBg = new[] { C("#00000000"), C("#00000000") },
            CardBg = new[] { C("#26FFFFFF"), C("#12FFFFFF") },
            InputCardBg = C("#1AFFFFFF"),
            CardStroke = new LinearGradientBrush(new GradientStopCollection
                { new GradientStop(C("#A78BFA"), 0), new GradientStop(C("#F472B6"), 1) }, new Point(0, 0), new Point(1, 1)),
            CardStrokeThickness = 1.5,
            Surface = C("#33000000"), Field = C("#33000000"), Chip = C("#26FFFFFF"), ChipAlt = C("#1AFFFFFF"),
            ChipEmpty = C("#33000000"), HighBg = C("#40D946EF"), ChipOutline = Colors.Transparent, Selection = Colors.White,
            HeaderButton = C("#26FFFFFF"),
            TextPrimary = Colors.White, TextSecondary = C("#F0D4FF"), TextMuted = C("#C79BE0"), TextDim = C("#9A6DB8"),
            Placeholder = C("#A77BC4"), Divider = C("#33FFFFFF"),
            Result = C("#FFFFFF"), Good = C("#86EFAC"), Warn = C("#FDE68A"), Info = C("#F0ABFC"), Error = C("#FDA4AF"),
            Panel = C("#33000000"), Row1 = C("#1AFFFFFF"), Row2 = C("#0DFFFFFF"),
            SheetTop = C("#3A1366"), SheetBottom = C("#5A1260"), Backdrop = C("#99100020"),
            CardRadius = 32, ControlRadius = 20, ChipRadius = 18,
        },
        // 3 ── AMOLED: pure black, no outlines, teal, condensed font
        new()
        {
            Key = "themeAmoled", Fallback = "AMOLED", FontFamily = "sans-serif-condensed",
            Accent = C("#14B8A6"), OnAccent = Colors.Black, AccentBorder = C("#1A1A1A"),
            PageBg = new[] { C("#000000"), C("#000000"), C("#000000") },
            HeaderBg = new[] { C("#000000"), C("#000000") },
            CardBg = new[] { C("#0A0A0A"), C("#0A0A0A") },
            InputCardBg = C("#0A0A0A"), CardStrokeThickness = 0,
            Surface = C("#141414"), Field = C("#141414"), Chip = C("#1A1A1A"), ChipAlt = C("#121212"),
            ChipEmpty = C("#202020"), HighBg = C("#062B28"), ChipOutline = Colors.Transparent, Selection = C("#2DD4BF"),
            HeaderButton = C("#141414"),
            TextPrimary = C("#F5F5F5"), TextSecondary = C("#A3A3A3"), TextMuted = C("#737373"), TextDim = C("#525252"),
            Placeholder = C("#525252"), Divider = C("#1F1F1F"),
            Result = C("#2DD4BF"), Good = C("#2DD4BF"), Warn = C("#FACC15"), Info = C("#5EEAD4"), Error = C("#F87171"),
            Panel = C("#0A0A0A"), Row1 = C("#141414"), Row2 = C("#0A0A0A"),
            SheetTop = C("#0A0A0A"), SheetBottom = C("#000000"), Backdrop = C("#CC000000"),
            CardRadius = 22, ControlRadius = 12,
        },
        // 4 ── Light: white cards, dark text, soft shadows
        new()
        {
            Key = "themeLight", Fallback = "Light", IsLight = true,
            Accent = C("#2563EB"), OnAccent = Colors.White, AccentBorder = C("#CBD5E1"),
            PageBg = new[] { C("#F8FAFC"), C("#EEF2F7"), C("#E2E8F0") },
            HeaderBg = new[] { C("#FFFFFF"), C("#F8FAFC") },
            CardBg = new[] { C("#FFFFFF"), C("#FFFFFF") },
            InputCardBg = C("#FFFFFF"),
            CardStroke = new SolidColorBrush(C("#E2E8F0")), CardStrokeThickness = 1, CardShadow = true,
            Surface = C("#F1F5F9"), Field = C("#F8FAFC"), Chip = C("#E9EEF5"), ChipAlt = C("#F1F5F9"),
            ChipEmpty = C("#E5E7EB"), HighBg = C("#DBEAFE"), ChipOutline = C("#CBD5E1"), Selection = C("#0F172A"),
            HeaderButton = C("#0F0F172A"),
            TextPrimary = C("#0F172A"), TextSecondary = C("#475569"), TextMuted = C("#64748B"), TextDim = C("#94A3B8"),
            Placeholder = C("#94A3B8"), Divider = C("#E2E8F0"),
            Result = C("#047857"), Good = C("#047857"), Warn = C("#B45309"), Info = C("#1D4ED8"), Error = C("#DC2626"),
            Panel = C("#F8FAFC"), Row1 = C("#F1F5F9"), Row2 = C("#FFFFFF"),
            SheetTop = C("#FFFFFF"), SheetBottom = C("#F8FAFC"), Backdrop = C("#66000000"),
        },
    };

    /// <summary>Order in the settings picker.</summary>
    public static readonly int[] DisplayOrder = { 0, 4, 1, 2, 3 };

    public static int Index { get; private set; }
    public static ThemeProfile Current { get; private set; } = All[0];

    public static void Set(int index)
    {
        Index = Math.Clamp(index, 0, All.Length - 1);
        Current = All[Index];
        PushResources();
    }

    /// <summary>Publishes the colours used by XAML via {DynamicResource …}.</summary>
    private static void PushResources()
    {
        if (Application.Current?.Resources is not { } r) return;
        var t = Current;
        r["TextPrimary"] = t.TextPrimary;
        r["TextSecondary"] = t.TextSecondary;
        r["TextMuted"] = t.TextMuted;
        r["PlaceholderColor"] = t.Placeholder;
        r["DividerColor"] = t.Divider;
        r["SurfaceColor"] = t.Surface;
        r["FieldColor"] = t.Field;
        r["AccentBorderColor"] = t.AccentBorder;
        r["HeaderButtonColor"] = t.HeaderButton;
        r["ResultColor"] = t.Result;
        r["BackdropColor"] = t.Backdrop;
        r["AppFont"] = t.FontFamily ?? "";
        Application.Current.UserAppTheme = t.IsLight ? AppTheme.Light : AppTheme.Dark;
    }

    // ── Helpers ─────────────────────────────────────────────────────
    public static LinearGradientBrush Gradient(Color[] stops, bool diagonal)
    {
        var coll = new GradientStopCollection();
        for (int i = 0; i < stops.Length; i++)
            coll.Add(new GradientStop(stops[i], stops.Length == 1 ? 0 : (float)i / (stops.Length - 1)));
        return new LinearGradientBrush(coll, new Point(0, 0), diagonal ? new Point(1, 1) : new Point(0, 1));
    }

    public static Brush AccentFill => Current.AccentGradient ?? new SolidColorBrush(Current.Accent);

    public static RoundRectangle Round(double r) => new() { CornerRadius = new CornerRadius(r) };
}
