using System.Globalization;

namespace FirstApp.Core;

/// <summary>
/// Pure resistor math (no UI dependencies): parsing, colour encoding/decoding,
/// formatting and IEC 60063 E-series rounding.
/// </summary>
public static class ResistorMath
{
    public const int MinExp = -2;   // silver ×0.01
    public const int MaxExp = 9;    // white  ×1G

    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    // ──────────────────────────────────────────────────────────────
    // PARSING
    // ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Parses "4700", "4.7k", "4,7 k", "4k7", "4R7", "2M2", "1G", "470 Ω" …
    /// </summary>
    public static bool TryParse(string? raw, out double value)
    {
        value = -1;
        if (string.IsNullOrWhiteSpace(raw)) return false;

        Span<char> buf = stackalloc char[raw.Length];
        int n = 0;
        foreach (char ch in raw)
        {
            if (char.IsWhiteSpace(ch) || ch == 'Ω' || ch == 'Ω') continue;
            buf[n++] = ch == ',' ? '.' : char.ToUpperInvariant(ch);
        }
        var s = buf[..n];
        if (s.EndsWith("OHM")) s = s[..^3];
        if (s.IsEmpty) return false;

        // Locate a multiplier letter (suffix "4.7K" or RKM infix "4K7")
        int letterPos = -1;
        double mul = 1;
        for (int i = 0; i < s.Length; i++)
        {
            double m = s[i] switch { 'R' => 1, 'K' => 1e3, 'M' => 1e6, 'G' => 1e9, _ => 0 };
            if (m == 0) continue;
            if (letterPos >= 0) return false;  // two letters
            letterPos = i; mul = m;
        }

        double number;
        if (letterPos < 0)
        {
            if (!double.TryParse(s, NumberStyles.Float, Inv, out number)) return false;
        }
        else
        {
            var left = s[..letterPos];
            var right = s[(letterPos + 1)..];
            if (right.IsEmpty)
            {
                if (!double.TryParse(left, NumberStyles.Float, Inv, out number)) return false;
            }
            else
            {
                // RKM notation: the letter replaces the decimal point
                if (left.Contains('.') || right.Contains('.')) return false;
                Span<char> joined = stackalloc char[left.Length + 1 + right.Length];
                left.CopyTo(joined);
                joined[left.Length] = '.';
                right.CopyTo(joined[(left.Length + 1)..]);
                if (!double.TryParse(joined, NumberStyles.Float, Inv, out number)) return false;
            }
        }

        if (double.IsNaN(number) || double.IsInfinity(number) || number < 0) return false;
        value = number * mul;
        return true;
    }

    // ──────────────────────────────────────────────────────────────
    // ENCODE / DECODE
    // ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Encodes a value into significant digits + multiplier exponent.
    /// Returns false when the value cannot be represented (too large / too small).
    /// <paramref name="encoded"/> is the value actually represented by the colours.
    /// </summary>
    public static bool TryEncode(double value, int sigDigits, Span<int> digits, out int multExp, out double encoded)
    {
        multExp = 0; encoded = 0;
        if (value < 0 || double.IsNaN(value) || double.IsInfinity(value)) return false;

        if (value == 0)
        {
            digits[..sigDigits].Clear();
            return true;
        }

        long limit = Pow10Int(sigDigits);           // e.g. 1000 for 3 digits
        int exp = (int)Math.Floor(Math.Log10(value)) - (sigDigits - 1);
        if (exp < MinExp) exp = MinExp;             // allow leading zero digits for tiny values

        long sig = (long)Math.Round(value / Math.Pow(10, exp), MidpointRounding.AwayFromZero);
        if (sig >= limit)                           // rounding overflow, e.g. 999.6 → 1000
        {
            exp++;
            sig = (long)Math.Round(value / Math.Pow(10, exp), MidpointRounding.AwayFromZero);
        }
        if (exp > MaxExp || sig <= 0 || sig >= limit) return false;

        for (int i = sigDigits - 1; i >= 0; i--) { digits[i] = (int)(sig % 10); sig /= 10; }
        multExp = exp;
        encoded = Decode(digits[..sigDigits], exp);
        return true;
    }

    public static double Decode(ReadOnlySpan<int> digits, int multExp)
    {
        long sig = 0;
        foreach (int d in digits) sig = sig * 10 + d;
        return RoundSig(sig * Math.Pow(10, multExp), 9);
    }

    public static bool IsSameValue(double a, double b)
        => Math.Abs(a - b) <= Math.Max(Math.Abs(a), Math.Abs(b)) * 1e-9;

    // ──────────────────────────────────────────────────────────────
    // FORMATTING
    // ──────────────────────────────────────────────────────────────

    public static (double Scale, string Prefix) PrefixFor(double v) => v switch
    {
        >= 1e9 => (1e9, "G"),
        >= 1e6 => (1e6, "M"),
        >= 1e3 => (1e3, "k"),
        _ => (1, "")
    };

    /// <summary>"4.7 kΩ"</summary>
    public static string FormatOhm(double v)
    {
        var (scale, prefix) = PrefixFor(v);
        return $"{FormatNumber(v / scale)} {prefix}Ω";
    }

    /// <summary>Value scaled to its prefix, without unit: (4.7, 1) for 4700 → index 1 = k.</summary>
    public static (string Number, int UnitIndex) SplitForEntry(double v)
    {
        int idx = v >= 1e9 ? 3 : v >= 1e6 ? 2 : v >= 1e3 ? 1 : 0;
        double scale = idx switch { 3 => 1e9, 2 => 1e6, 1 => 1e3, _ => 1 };
        return (FormatNumber(v / scale), idx);
    }

    /// <summary>"4.653 – 4.747 kΩ"</summary>
    public static string FormatRange(double nominal, double tolPercent)
    {
        var (scale, prefix) = PrefixFor(nominal);
        double lo = nominal * (1 - tolPercent / 100), hi = nominal * (1 + tolPercent / 100);
        return $"{FormatNumber(lo / scale)} – {FormatNumber(hi / scale)} {prefix}Ω";
    }

    public static string FormatNumber(double v)
        => RoundSig(v, 6).ToString("0.###", Inv);

    public static double RoundSig(double v, int sig)
    {
        if (v == 0 || double.IsNaN(v) || double.IsInfinity(v)) return v;
        int mag = (int)Math.Floor(Math.Log10(Math.Abs(v)));
        int decimals = sig - 1 - mag;
        if (decimals >= 0 && decimals <= 15) return Math.Round(v, decimals);
        double scale = Math.Pow(10, decimals);
        return Math.Round(v * scale) / scale;
    }

    // ──────────────────────────────────────────────────────────────
    // E-SERIES (IEC 60063)
    // ──────────────────────────────────────────────────────────────

    public static readonly int[] SeriesList = { 3, 6, 12, 24, 48, 96, 192 };

    private static readonly double[] E24 =
    {
        1.0, 1.1, 1.2, 1.3, 1.5, 1.6, 1.8, 2.0, 2.2, 2.4, 2.7, 3.0,
        3.3, 3.6, 3.9, 4.3, 4.7, 5.1, 5.6, 6.2, 6.8, 7.5, 8.2, 9.1,
    };

    private static readonly Dictionary<int, double[]> _seriesCache = new();

    /// <summary>Normalised values (1 ≤ x &lt; 10) of an E-series.</summary>
    public static double[] SeriesValues(int series)
    {
        lock (_seriesCache)
        {
            if (_seriesCache.TryGetValue(series, out var cached)) return cached;
            double[] vals;
            if (series <= 24)
            {
                int step = 24 / series;
                vals = new double[series];
                for (int i = 0; i < series; i++) vals[i] = E24[i * step];
            }
            else
            {
                // E48/E96/E192 follow the formula (3 significant digits),
                // with the single standardised exception 9.19 → 9.20 in E192.
                vals = new double[series];
                for (int i = 0; i < series; i++)
                    vals[i] = Math.Round(Math.Pow(10, (double)i / series), 2);
                if (series == 192) vals[185] = 9.20;
            }
            _seriesCache[series] = vals;
            return vals;
        }
    }

    /// <summary>Nearest standard value (logarithmic distance).</summary>
    public static double RoundToSeries(double value, int series)
    {
        if (value <= 0) return value;
        double decade = Math.Pow(10, Math.Floor(Math.Log10(value)));
        double norm = value / decade;
        if (norm >= 9.9999999) { decade *= 10; norm /= 10; }

        double best = 1, bestD = double.MaxValue, lnNorm = Math.Log(norm);
        foreach (double c in SeriesValues(series))
        {
            double d = Math.Abs(Math.Log(c) - lnNorm);
            if (d < bestD) { bestD = d; best = c; }
        }
        if (Math.Abs(Math.Log(10) - lnNorm) < bestD) best = 10;
        return RoundSig(best * decade, 6);
    }

    public static bool IsInSeries(double value, int series)
        => value > 0 && IsSameValue(RoundToSeries(value, series), RoundSig(value, 6));

    private static long Pow10Int(int n) { long r = 1; while (n-- > 0) r *= 10; return r; }
}
