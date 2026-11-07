namespace FirstApp.Core;

/// <summary>A resistor band colour. <see cref="Key"/> is the localisation key of its name.</summary>
public sealed class BandColor
{
    public string Key { get; }
    public Color Bg { get; }
    public Color Fg { get; }
    public bool IsNone { get; }

    public BandColor(string key, string hex, bool darkText, bool none = false)
    {
        Key = key; Bg = Color.FromArgb(hex); Fg = darkText ? Colors.Black : Colors.White; IsNone = none;
    }
}

/// <summary>One selectable option of a band (colour + label + numeric meaning).</summary>
public readonly record struct BandOption(BandColor Color, string Label, double Value);

/// <summary>IEC 60062 colour code tables.</summary>
public static class ColorCodes
{
    public static readonly BandColor Black = new("colBlack", "#000000", false);
    public static readonly BandColor Brown = new("colBrown", "#7A4A1E", false);
    public static readonly BandColor Red = new("colRed", "#D02525", false);
    public static readonly BandColor Orange = new("colOrange", "#F07800", true);
    public static readonly BandColor Yellow = new("colYellow", "#F2D600", true);
    public static readonly BandColor Green = new("colGreen", "#12963F", false);
    public static readonly BandColor Blue = new("colBlue", "#2457D6", false);
    public static readonly BandColor Violet = new("colViolet", "#8132B5", false);
    public static readonly BandColor Gray = new("colGray", "#808080", false);
    public static readonly BandColor White = new("colWhite", "#FFFFFF", true);
    public static readonly BandColor Gold = new("colGold", "#CFA525", true);
    public static readonly BandColor Silver = new("colSilver", "#BFC4C9", true);
    public static readonly BandColor None = new("colNone", "#1A1A1A", false, none: true);

    private static readonly BandColor[] DigitColors =
        { Black, Brown, Red, Orange, Yellow, Green, Blue, Violet, Gray, White };

    public static readonly BandOption[] Digits = BuildDigits();

    public static readonly BandOption[] Multipliers =
    {
        new(Silver, "×0.01", -2), new(Gold, "×0.1", -1), new(Black, "×1", 0),
        new(Brown, "×10", 1), new(Red, "×100", 2), new(Orange, "×1k", 3),
        new(Yellow, "×10k", 4), new(Green, "×100k", 5), new(Blue, "×1M", 6),
        new(Violet, "×10M", 7), new(Gray, "×100M", 8), new(White, "×1G", 9),
    };

    /// <summary>Index in <see cref="Multipliers"/> for exponent 0 (×1).</summary>
    public const int MultiplierX1 = 2;

    public static readonly BandOption[] Tolerance4 =
    {
        new(Brown, "±1%", 1), new(Red, "±2%", 2), new(Gold, "±5%", 5),
        new(Silver, "±10%", 10), new(None, "±20%", 20),
    };
    public const int Tolerance4Default = 2; // gold

    public static readonly BandOption[] Tolerance5 =
    {
        new(Gray, "±0.05%", 0.05), new(Violet, "±0.1%", 0.1), new(Blue, "±0.25%", 0.25),
        new(Green, "±0.5%", 0.5), new(Brown, "±1%", 1), new(Red, "±2%", 2),
        new(Gold, "±5%", 5), new(Silver, "±10%", 10),
    };
    public const int Tolerance5Default = 4; // brown

    public static readonly BandOption[] Tcr =
    {
        new(Black, "250", 250), new(Brown, "100", 100), new(Red, "50", 50),
        new(Orange, "15", 15), new(Yellow, "25", 25), new(Green, "20", 20),
        new(Blue, "10", 10), new(Violet, "5", 5), new(Gray, "1", 1),
    };
    public const int TcrDefault = 1; // brown

    public static BandColor DigitColor(int d) => DigitColors[Math.Clamp(d, 0, 9)];
    public static int MultiplierIndex(int exp) => Math.Clamp(exp - ResistorMath.MinExp, 0, Multipliers.Length - 1);

    private static BandOption[] BuildDigits()
    {
        var arr = new BandOption[10];
        for (int i = 0; i < 10; i++) arr[i] = new(DigitColors[i], i.ToString(), i);
        return arr;
    }
}
