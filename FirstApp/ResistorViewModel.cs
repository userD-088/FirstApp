using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using FirstApp.Core;

namespace FirstApp;

/// <summary>
/// Single source of truth for the calculator. Both input modes share the same band
/// selection, so switching between "value → colour" and "colour → value" keeps the
/// resistor that is currently shown.
/// </summary>
public sealed class ResistorViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    /// <summary>Raised whenever the drawn band colours change.</summary>
    public event Action? RingsChanged;

    private void Notify([CallerMemberName] string? n = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    private void NotifyAll() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));

    private static readonly double[] UnitScales = { 1, 1e3, 1e6, 1e9 };
    public static readonly string[] UnitLabels = { "Ω", "kΩ", "MΩ", "GΩ" };

    // ──────────────────────────────────────────────────────────────
    // STATE
    // ──────────────────────────────────────────────────────────────
    private int _mode = 1;            // 0 = 4-band, 1 = 5-band, 2 = 6-band
    private int _inputMode;           // 0 = value → colour, 1 = colour → value
    private int _unitIndex;
    private string _entryText = "";
    private int _series;
    private bool _showRange;

    private readonly int[] _digits = { 1, 0, 0 };
    private int _mulIdx = ColorCodes.MultiplierX1;
    private int _tolIdx = ColorCodes.Tolerance5Default;
    private int _tcrIdx = ColorCodes.TcrDefault;

    private BandColor[] _rings = Array.Empty<BandColor>();
    private double _nominal = double.NaN;   // NaN = no valid value
    private double _suggestion = double.NaN;

    // ──────────────────────────────────────────────────────────────
    // LOCALISATION  –  XAML: Text="{Binding [title]}"
    // ──────────────────────────────────────────────────────────────
    public string this[string key] => Loc.T(key);

    public string Lang
    {
        get => Loc.Language;
        set
        {
            if (value == Loc.Language) return;
            Loc.SetLanguage(value);
            AppSettings.Language = Loc.Language;
            Recalculate(notifyAll: true);
        }
    }

    public double TitleFontSize
    {
        get { int l = this["title"].Length; return l > 28 ? 17.0 : l > 22 ? 20.0 : l > 16 ? 22.0 : 24.0; }
    }

    public string AppVersionText => $"Resistor Calculator v{AppInfo.VersionString}";

    // ──────────────────────────────────────────────────────────────
    // MODE PROPERTIES
    // ──────────────────────────────────────────────────────────────
    public int Mode => _mode;
    public int InputMode => _inputMode;
    public bool IsValueMode => _inputMode == 0;
    public bool IsColorMode => _inputMode == 1;
    public bool Ring3Visible => _mode >= 1;
    public bool TcrVisible => _mode == 2;
    public int SigDigits => _mode == 0 ? 2 : 3;

    public BandOption[] ToleranceOptions => _mode == 0 ? ColorCodes.Tolerance4 : ColorCodes.Tolerance5;

    public int Digit(int i) => _digits[i];
    public int MultiplierIndex => _mulIdx;
    public int ToleranceIndex => _tolIdx;
    public int TcrIndex => _tcrIdx;
    public IReadOnlyList<BandColor> Rings => _rings;
    public Color BodyColor { get; private set; } = Color.FromArgb("#6FA8C7");

    public string EntryText
    {
        get => _entryText;
        set
        {
            value ??= "";
            if (value == _entryText) return;
            _entryText = value;
            Notify();
            if (IsValueMode) Recalculate();
        }
    }

    public int UnitIndex
    {
        get => _unitIndex;
        set
        {
            value = Math.Clamp(value, 0, 3);
            if (value == _unitIndex) return;
            _unitIndex = value;
            Notify();
            if (IsValueMode) Recalculate();
        }
    }

    public int Series
    {
        get => _series;
        set
        {
            if (value == _series) return;
            _series = value;
            AppSettings.Series = value;
            Notify();
            Recalculate();
        }
    }

    public bool ShowRange
    {
        get => _showRange;
        set { if (value == _showRange) return; _showRange = value; Notify(); Notify(nameof(HasRange)); }
    }

    // ──────────────────────────────────────────────────────────────
    // OUTPUT PROPERTIES
    // ──────────────────────────────────────────────────────────────
    public string ResultValue { get; private set; } = "—";
    public string ResultDetail { get; private set; } = "";
    public string ResultRange { get; private set; } = "";
    public bool HasRange => _showRange && ResultRange.Length > 0;
    public string BandNames { get; private set; } = "";
    public string StatusText { get; private set; } = "";
    public bool HasStatus => StatusText.Length > 0;
    public bool StatusIsError { get; private set; }
    public string SuggestionText { get; private set; } = "";
    public bool HasSuggestion => SuggestionText.Length > 0;
    public bool HasValidValue => !double.IsNaN(_nominal);

    /// <summary>Plain text for the clipboard, e.g. "4.7 kΩ ±1% (Yellow, Violet, Black, Brown, Brown)".</summary>
    public string ClipboardText
        => HasValidValue ? $"{ResultValue} {ResultDetail}".Trim() + (BandNames.Length > 0 ? $" ({BandNames.Replace(" · ", ", ")})" : "") : "";

    // ══════════════════════════════════════════════════════════════
    // CONSTRUCTOR
    // ══════════════════════════════════════════════════════════════
    public ResistorViewModel()
    {
        Loc.SetLanguage(AppSettings.Language);
        _series = AppSettings.Series;
        _showRange = AppSettings.ShowRange;
        if (AppSettings.RememberState) RestoreState(AppSettings.LastState);
        ApplyBody();
        Recalculate();
    }

    // ══════════════════════════════════════════════════════════════
    // PUBLIC API
    // ══════════════════════════════════════════════════════════════
    public void SetMode(int mode)
    {
        mode = Math.Clamp(mode, 0, 2);
        if (mode == _mode) return;

        double keep = _nominal;
        double tolPercent = ToleranceOptions[Math.Clamp(_tolIdx, 0, ToleranceOptions.Length - 1)].Value;

        _mode = mode;
        _tolIdx = FindTolerance(ToleranceOptions, tolPercent, mode == 0 ? ColorCodes.Tolerance4Default : ColorCodes.Tolerance5Default);
        ApplyBody();

        // Colour mode: keep the resistance (re-encoded with the new number of digits)
        if (IsColorMode && !double.IsNaN(keep)) EncodeIntoSelection(keep);

        Notify(nameof(Mode)); Notify(nameof(Ring3Visible)); Notify(nameof(TcrVisible));
        Notify(nameof(ToleranceOptions)); Notify(nameof(SigDigits));
        Recalculate();
    }

    public void SetInputMode(int mode)
    {
        mode = Math.Clamp(mode, 0, 1);
        if (mode == _inputMode) return;
        _inputMode = mode;

        // Switching to value mode: pre-fill the entry with the resistor that is shown
        if (mode == 0 && !double.IsNaN(_nominal)) WriteEntry(_nominal);

        Notify(nameof(InputMode)); Notify(nameof(IsValueMode)); Notify(nameof(IsColorMode));
        Recalculate();
    }

    /// <summary>band: 0-2 digits, 3 multiplier, 4 tolerance, 5 TCR.</summary>
    public void SelectBand(int band, int idx)
    {
        switch (band)
        {
            case 0: case 1: case 2: _digits[band] = Math.Clamp(idx, 0, 9); break;
            case 3: _mulIdx = Math.Clamp(idx, 0, ColorCodes.Multipliers.Length - 1); break;
            case 4: _tolIdx = Math.Clamp(idx, 0, ToleranceOptions.Length - 1); break;
            case 5: _tcrIdx = Math.Clamp(idx, 0, ColorCodes.Tcr.Length - 1); break;
        }
        if (band <= 3 && IsValueMode) return; // digits are driven by the entry in value mode
        Recalculate();
    }

    /// <summary>Rounds the current value to the selected E-series (if a suggestion exists).</summary>
    public void ApplySuggestion()
    {
        if (double.IsNaN(_suggestion)) return;
        ApplyValue(_suggestion);
    }

    public void ApplyValue(double value)
    {
        if (IsValueMode) WriteEntry(value);
        else EncodeIntoSelection(value);
        Recalculate();
    }

    public void ResetInput()
    {
        _digits[0] = 1; _digits[1] = 0; _digits[2] = 0;
        _mulIdx = ColorCodes.MultiplierX1;
        _tcrIdx = ColorCodes.TcrDefault;
        _tolIdx = _mode == 0 ? ColorCodes.Tolerance4Default : ColorCodes.Tolerance5Default;
        _entryText = ""; _unitIndex = 0;
        Notify(nameof(EntryText)); Notify(nameof(UnitIndex));
        Recalculate(notifyAll: true);
    }

    // ══════════════════════════════════════════════════════════════
    // CALCULATION
    // ══════════════════════════════════════════════════════════════
    private void Recalculate(bool notifyAll = false)
    {
        string status = "";
        bool error = false;

        if (IsValueMode)
        {
            if (string.IsNullOrWhiteSpace(_entryText))
            {
                _nominal = double.NaN;
                status = Loc.T("enterValue");
            }
            else if (!TryGetEntryValue(out double v))
            {
                _nominal = double.NaN;
                status = Loc.T("invalidInput"); error = true;
            }
            else if (!EncodeIntoSelection(v, out double encoded))
            {
                _nominal = double.NaN;
                status = Loc.T("outOfRange"); error = true;
            }
            else
            {
                _nominal = encoded;
                if (!ResistorMath.IsSameValue(encoded, v))
                    status = string.Format(CultureInfo.InvariantCulture, Loc.T("roundedTo"), SigDigits);
            }
        }
        else
        {
            _nominal = ResistorMath.Decode(_digits.AsSpan(0, SigDigits), _mulIdx + ResistorMath.MinExp);
        }

        BuildRings();
        BuildTexts(status, error);

        if (notifyAll) { NotifyAll(); }
        else
        {
            Notify(nameof(ResultValue)); Notify(nameof(ResultDetail)); Notify(nameof(ResultRange));
            Notify(nameof(HasRange)); Notify(nameof(BandNames)); Notify(nameof(StatusText));
            Notify(nameof(HasStatus)); Notify(nameof(StatusIsError)); Notify(nameof(SuggestionText));
            Notify(nameof(HasSuggestion)); Notify(nameof(HasValidValue));
        }
        RingsChanged?.Invoke();
    }

    private void BuildRings()
    {
        int count = _mode switch { 0 => 4, 1 => 5, _ => 6 };
        if (_rings.Length != count) _rings = new BandColor[count];
        int sig = SigDigits;
        for (int i = 0; i < sig; i++) _rings[i] = ColorCodes.DigitColor(_digits[i]);
        _rings[sig] = ColorCodes.Multipliers[_mulIdx].Color;
        _rings[sig + 1] = ToleranceOptions[Math.Clamp(_tolIdx, 0, ToleranceOptions.Length - 1)].Color;
        if (count == 6) _rings[5] = ColorCodes.Tcr[_tcrIdx].Color;
    }

    private void BuildTexts(string status, bool error)
    {
        StatusText = status;
        StatusIsError = error;
        var tol = ToleranceOptions[Math.Clamp(_tolIdx, 0, ToleranceOptions.Length - 1)];

        if (double.IsNaN(_nominal))
        {
            ResultValue = "—"; ResultDetail = ""; ResultRange = ""; BandNames = "";
            SuggestionText = ""; _suggestion = double.NaN;
            return;
        }

        ResultValue = ResistorMath.FormatOhm(_nominal);
        ResultDetail = _mode == 2 ? $"{tol.Label}  ·  {ColorCodes.Tcr[_tcrIdx].Label} ppm/K" : tol.Label;
        ResultRange = _nominal > 0 ? ResistorMath.FormatRange(_nominal, tol.Value) : "";

        var names = new string[_rings.Length];
        for (int i = 0; i < _rings.Length; i++) names[i] = Loc.T(_rings[i].Key);
        BandNames = string.Join(" · ", names);

        if (_nominal > 0 && !ResistorMath.IsInSeries(_nominal, _series))
        {
            _suggestion = ResistorMath.RoundToSeries(_nominal, _series);
            SuggestionText = string.Format(CultureInfo.InvariantCulture, Loc.T("nearestStd"), $"E{_series}")
                             + $"  →  {ResistorMath.FormatOhm(_suggestion)}";
        }
        else
        {
            _suggestion = double.NaN;
            SuggestionText = "";
        }
    }

    private bool TryGetEntryValue(out double value)
    {
        if (!ResistorMath.TryParse(_entryText, out value)) return false;
        // An explicit letter (4k7, 2M2, 4R7…) wins over the unit selector
        bool hasLetter = _entryText.AsSpan().IndexOfAny("kKmMgGrR") >= 0;
        if (!hasLetter) value *= UnitScales[_unitIndex];
        return true;
    }

    private bool EncodeIntoSelection(double value) => EncodeIntoSelection(value, out _);

    private bool EncodeIntoSelection(double value, out double encoded)
    {
        Span<int> d = stackalloc int[3];
        if (!ResistorMath.TryEncode(value, SigDigits, d, out int exp, out encoded)) return false;
        for (int i = 0; i < SigDigits; i++) _digits[i] = d[i];
        _mulIdx = ColorCodes.MultiplierIndex(exp);
        return true;
    }

    private void WriteEntry(double value)
    {
        var (num, unit) = ResistorMath.SplitForEntry(value);
        _unitIndex = unit;
        _entryText = num;
        Notify(nameof(UnitIndex));
        Notify(nameof(EntryText));
    }

    private void ApplyBody()
        => BodyColor = _mode == 0 ? Color.FromArgb("#D9BC8C") : Color.FromArgb("#6FA8C7");

    private static int FindTolerance(BandOption[] opts, double percent, int fallback)
    {
        for (int i = 0; i < opts.Length; i++) if (opts[i].Value == percent) return i;
        return fallback;
    }

    // ══════════════════════════════════════════════════════════════
    // PERSISTENCE
    // ══════════════════════════════════════════════════════════════
    public void SaveState()
    {
        if (!AppSettings.RememberState) return;
        string entry = _entryText.Replace("|", "");
        AppSettings.LastState = string.Join('|',
            _mode, _inputMode, $"{_digits[0]},{_digits[1]},{_digits[2]}", _mulIdx, _tolIdx, _tcrIdx, _unitIndex, entry);
    }

    private void RestoreState(string s)
    {
        if (string.IsNullOrEmpty(s)) return;
        try
        {
            var p = s.Split('|');
            if (p.Length < 8) return;
            _mode = Math.Clamp(int.Parse(p[0], CultureInfo.InvariantCulture), 0, 2);
            _inputMode = Math.Clamp(int.Parse(p[1], CultureInfo.InvariantCulture), 0, 1);
            var d = p[2].Split(',');
            for (int i = 0; i < 3 && i < d.Length; i++) _digits[i] = Math.Clamp(int.Parse(d[i], CultureInfo.InvariantCulture), 0, 9);
            _mulIdx = Math.Clamp(int.Parse(p[3], CultureInfo.InvariantCulture), 0, ColorCodes.Multipliers.Length - 1);
            _tolIdx = Math.Clamp(int.Parse(p[4], CultureInfo.InvariantCulture), 0, ToleranceOptions.Length - 1);
            _tcrIdx = Math.Clamp(int.Parse(p[5], CultureInfo.InvariantCulture), 0, ColorCodes.Tcr.Length - 1);
            _unitIndex = Math.Clamp(int.Parse(p[6], CultureInfo.InvariantCulture), 0, 3);
            _entryText = p[7];
        }
        catch
        {
            // Corrupt state → defaults
            _mode = 1; _inputMode = 0; _entryText = ""; _unitIndex = 0;
            _digits[0] = 1; _digits[1] = 0; _digits[2] = 0;
            _mulIdx = ColorCodes.MultiplierX1; _tolIdx = ColorCodes.Tolerance5Default; _tcrIdx = ColorCodes.TcrDefault;
        }
    }
}
