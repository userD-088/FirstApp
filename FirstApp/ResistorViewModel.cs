using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using Microsoft.Maui.Graphics;
using System.Text.Json;

namespace FirstApp
{
    public sealed class ResistorViewModel : INotifyPropertyChanged
    {
        // ──────────────────────────────────────────────────────────────
        // PROPERTY CHANGE / MVVM HELPERS
        // ──────────────────────────────────────────────────────────────
        public event PropertyChangedEventHandler? PropertyChanged;
        private void Notify([CallerMemberName] string? n = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
        private void NotifyAll()
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
        private bool Set<T>(ref T f, T v, [CallerMemberName] string? n = null)
        {
            if (EqualityComparer<T>.Default.Equals(f, v)) return false;
            f = v; Notify(n); return true;
        }

        // ──────────────────────────────────────────────────────────────
        // COLOR CONSTANTS (RESISTOR COLOR STANDARD)
        // ──────────────────────────────────────────────────────────────
        internal static readonly Color CBlack = Color.FromRgb(0, 0, 0);
        internal static readonly Color CBrown = Color.FromRgb(96, 64, 0);
        internal static readonly Color CRed = Color.FromRgb(200, 32, 32);
        internal static readonly Color COrange = Color.FromRgb(230, 110, 0);
        internal static readonly Color CYellow = Color.FromRgb(220, 200, 0);
        internal static readonly Color CGreen = Color.FromRgb(0, 140, 60);
        internal static readonly Color CBlue = Color.FromRgb(30, 80, 200);
        internal static readonly Color CViolet = Color.FromRgb(120, 40, 160);
        internal static readonly Color CGray = Color.FromRgb(120, 120, 120);
        internal static readonly Color CWhite = Color.FromRgb(255, 255, 255);
        internal static readonly Color CSilver = Color.FromRgb(180, 180, 180);
        internal static readonly Color CGold = Color.FromRgb(200, 160, 0);
        internal static readonly Color CNone = Color.FromArgb("#1A1A1A");

        // ──────────────────────────────────────────────────────────────
        // COLOR BAND DATA TABLES
        // ──────────────────────────────────────────────────────────────
        public static readonly (Color Bg, string Lbl, Color Fg)[] DigitData =
        {
            (CBlack,"0",CWhite),(CBrown,"1",CWhite),(CRed,"2",CWhite),(COrange,"3",CBlack),
            (CYellow,"4",CBlack),(CGreen,"5",CWhite),(CBlue,"6",CWhite),(CViolet,"7",CWhite),
            (CGray,"8",CWhite),(CWhite,"9",CBlack),
        };
        public static readonly (Color Bg, string Lbl, Color Fg)[] MultData =
        {
            (CSilver,"×0.01",CBlack),(CGold,"×0.1",CBlack),(CBlack,"×1",CWhite),(CBrown,"×10",CWhite),
            (CRed,"×100",CWhite),(COrange,"×1k",CBlack),(CYellow,"×10k",CBlack),(CGreen,"×100k",CWhite),
            (CBlue,"×1M",CWhite),(CViolet,"×10M",CWhite),(CGray,"×100M",CWhite),(CWhite,"×1G",CBlack),
        };
        public static readonly (Color Bg, string Lbl, Color Fg)[] TolData4 =
        {
            (CRed,"±2%",CWhite),(CGold,"±5%",CBlack),(CSilver,"±10%",CBlack),(CNone,"±20%",CWhite),
        };
        public static readonly (Color Bg, string Lbl, Color Fg)[] TolData5 =
        {
            (CGray,"±0.05%",CWhite),(CViolet,"±0.1%",CWhite),(CBlue,"±0.25%",CWhite),
            (CGreen,"±0.5%",CWhite),(CBrown,"±1%",CWhite),(CRed,"±2%",CWhite),(CGold,"±5%",CBlack),
        };
        public static readonly (Color Bg, string Lbl, Color Fg)[] TcrData =
        {
            (CBrown,"100",CWhite),(CRed,"50",CWhite),(COrange,"15",CBlack),
            (CYellow,"25",CBlack),(CBlue,"10",CWhite),(CViolet,"5",CWhite),
        };

        // ──────────────────────────────────────────────────────────────
        // SUPPORTED UI LANGUAGES
        // ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Create a custom language by (code, flag emoji, short label).
        /// The code is used to load the corresponding JSON in \Resources\Localization,
        /// so it must match the filename (without .json).
        /// Emoji can be used; rare languages use png "" (empty string),
        /// png is in format flag_{code}.png in Images\Flags.
        /// </summary>

        // Main languages
        public static readonly (string Code, string Flag, string Short)[] PrimaryLanguages =
        {
            ("en","🇬🇧","EN"), ("de","🇩🇪","DE"), ("es","🇪🇸","ES"), ("ru","🇷🇺","RU"), ("fr","🇫🇷","FR"),
            ("pt","🇵🇹","PT"), ("ja","🇯🇵","JA"), ("zh","🇨🇳","ZH"),
        };

        // Secondary languages – only loaded when user clicks "More languages"
        public static readonly (string Code, string Flag, string Short)[] SecondaryLanguages =
        {
            ("it","🇮🇹","IT"), ("nl","🇳🇱","NL"), ("ko","🇰🇷","KO"), ("tr","🇹🇷","TR"),
            ("pl","🇵🇱","PL"), ("sv","🇸🇪","SV"), ("da","🇩🇰","DA"), ("no","🇳🇴","NO"),
            ("fi","🇫🇮","FI"), ("cs","🇨🇿","CS"), ("el","🇬🇷","EL"), ("uk","🇺🇦","UK"),
            ("ar","🇸🇦","AR"), ("hi","🇮🇳","HI"), ("ro","🇷🇴","RO"), ("hu","🇭🇺","HU"),
            ("he","🇮🇱","HE"), ("id","🇮🇩","ID"), ("vi","🇻🇳","VI"), ("th","🇹🇭","TH"),
            ("ta","🇮🇳","TA"), ("te","🇮🇳","TE"), ("fa","🇮🇷","FA"), ("ur","🇵🇰","UR"),
            ("sw","🇰🇪","SW"), ("sk","🇸🇰","SK"), ("sl","🇸🇮","SL"), ("hr","🇭🇷","HR"),
            ("bg","🇧🇬","BG"), ("lt","🇱🇹","LT"), ("lv","🇱🇻","LV"), ("et","🇪🇪","ET"),
            ("ms","🇲🇾","MS"), ("gl","","GL"),     ("ca","","CA"),     ("eu","","EU"),
            ("la","🇻🇦","LA"), ("cy","","CY"),     ("af","🇿🇦","AF"), ("sq","🇦🇱","SQ"),
            ("am","🇪🇹","AM"), ("is","🇮🇸","IS"), ("eo","","EO"),     ("haw","","HAW"),
            ("rap","🗿","RAP"),("pa","🇮🇳","PA"), ("jv","🇮🇩","JV"), ("mr","🇮🇳","MR"),
            ("wuu","🇨🇳","WUU"),("ha","🇳🇬","HA"),("kn","🇮🇳","KN"), ("gu","🇮🇳","GU"),
            ("tl","🇵🇭","TL"), ("sr","🇷🇸","SR"), ("ka","🇬🇪","KA"), ("hy","🇦🇲","HY"),
            ("az","🇦🇿","AZ"), ("bn","🇧🇩","BN"), ("ml","🇮🇳","ML"),
        };

        // Combined – used only for default-language detection
        private static readonly (string Code, string Flag, string Short)[] _allLanguages
            = [.. PrimaryLanguages, .. SecondaryLanguages];

        // ──────────────────────────────────────────────────────────────
        // RESISTOR STATE (BAND COLORS, etc.)
        // ──────────────────────────────────────────────────────────────
        public readonly List<Color> Rings = new();
        private Color _bodyColor = Colors.SteelBlue;
        public Color BodyColor => _bodyColor;
        public static bool IsInvisibleRing(Color c) => c == CNone;
        public event Action? RingsChanged;

        // ──────────────────────────────────────────────────────────────
        // INTERNAL STATE FIELDS
        // ──────────────────────────────────────────────────────────────
        private int _resistorMode = 1;
        private int _inputMode = 0;
        private int _selectedSeries = 12;
        private string _lang = "en";
        private string _resultText = "0 Ω";

        // ──────────────────────────────────────────────────────────────
        // BAND SELECTION INDICES
        // ──────────────────────────────────────────────────────────────
        public int Sel1 = 1, Sel2 = 0, Sel3 = 0, SelMul = 2, SelTol = 4, SelTcr = 0;

        // ──────────────────────────────────────────────────────────────
        // VIEWMODEL PROPERTIES
        // ──────────────────────────────────────────────────────────────
        public int ResistorMode
        {
            get => _resistorMode;
            private set
            {
                if (_resistorMode == value) return;
                _resistorMode = value;
                Notify(); Notify(nameof(Ring3Visible)); Notify(nameof(TcrVisible));
                Notify(nameof(ModeLabelText)); Notify(nameof(CurrentTolData));
            }
        }
        public int InputMode
        {
            get => _inputMode;
            private set
            {
                if (_inputMode == value) return;
                _inputMode = value;
                Notify(); Notify(nameof(IsColorMode)); Notify(nameof(IsValueMode));
            }
        }
        public int SelectedSeries { get => _selectedSeries; set => Set(ref _selectedSeries, value); }
        public string Lang
        {
            get => _lang;
            set { if (!Set(ref _lang, value)) return; Preferences.Set("pref_lang", _lang); NotifyAll(); }
        }
        public string ResultText { get => _resultText; private set => Set(ref _resultText, value); }

        // ──────────────────────────────────────────────────────────────
        // UI VISIBILITY FLAGS
        // ──────────────────────────────────────────────────────────────
        public bool Ring3Visible => _resistorMode >= 1;
        public bool TcrVisible => _resistorMode == 2;
        public bool IsColorMode => _inputMode == 1;
        public bool IsValueMode => _inputMode == 0;

        // ──────────────────────────────────────────────────────────────
        // DATA SOURCES DEPENDING ON MODE
        // ──────────────────────────────────────────────────────────────
        public (Color Bg, string Lbl, Color Fg)[] CurrentTolData
            => _resistorMode == 0 ? TolData4 : TolData5;

        // ──────────────────────────────────────────────────────────────
        // LOCALIZED UI TEXT PROPERTIES
        // ──────────────────────────────────────────────────────────────
        public string TitleText => T("title");
        public string SubtitleText => T("subtitle");
        public string ModeLabelText => _resistorMode switch { 0 => T("band4"), 1 => T("band5"), _ => T("band6") };
        public string ResistanceLabelText => T("resistance");
        public string NormSeriesLabelText => T("normSeries");
        public string Ring1Text => T("ring1");
        public string Ring2Text => T("ring2");
        public string Ring3Text => T("ring3");
        public string MultText => T("mult");
        public string TolText => T("tol");
        public string TcrText => T("tcr");
        public string SettingsTitleText => T("settings");
        public string LangSectionText => T("language");
        public string AboutTitleText => T("about");
        public string AboutDescText => T("aboutDesc");
        public string CopyrightText => T("copyright");
        public string CloseText => T("close");
        public string WarningTitle => T("warning");
        public string OutOfRangeText => T("outOfRange");
        public string ValToColTabText => T("valToCol");
        public string ColToValTabText => T("colToVal");
        public string Band4BtnText => T("band4");
        public string Band5BtnText => T("band5");
        public string Band6BtnText => T("band6");
        public string MoreLangsText => T("moreLangs");
        public string LessLangsText => T("lessLangs");
        public string SupportText => T("support");
        public string PrivacyPolicyText => T("privacyPolicy");
        public string AboutDevText => T("aboutDev");

        // ──────────────────────────────────────────────────────────────
        // APP INFO
        // ──────────────────────────────────────────────────────────────
        public string AppVersionText => $"Resistor Calculator v{AppInfo.VersionString}";
        public double TitleFontSize
        {
            get { int l = TitleText.Length; return l > 28 ? 17.0 : l > 22 ? 20.0 : l > 16 ? 22.0 : 24.0; }
        }

        // ══════════════════════════════════════════════════════════════
        // CONSTRUCTOR
        // ══════════════════════════════════════════════════════════════
        public ResistorViewModel()
        {
            _lang = Preferences.Get("pref_lang", GetDefaultLanguage());
            _selectedSeries = Preferences.Get("pref_series", 12);
            _resistorMode = 1;
            InitRings();
        }

        // ══════════════════════════════════════════════════════════════
        // PUBLIC API (UI INTERACTION)
        // ══════════════════════════════════════════════════════════════
        public void SetMode(int mode)
        {
            Rings.Clear();
            _bodyColor = mode == 0 ? Color.FromArgb("#D2B48C") : Colors.SteelBlue;

            int count = mode switch { 0 => 4, 1 => 5, _ => 6 };
            for (int i = 0; i < count; i++) Rings.Add(CBlack);

            SelTol = mode == 0 ? 1 : 4; SelTcr = 0;
            ResistorMode = mode;
            Recalc();
        }
        public void SetInputMode(int mode) { InputMode = mode; Recalc(); }
        public void OnBandSelected(int band, int idx)
        {
            switch (band)
            {
                case 0: Sel1 = idx; break;
                case 1: Sel2 = idx; break;
                case 2: Sel3 = idx; break;
                case 3: SelMul = idx; break;

                case 4:
                    SelTol = idx;
                    if (IsColorMode) RecalcColorToValue();
                    else { ApplyTolRing(); RingsChanged?.Invoke(); }
                    return;

                case 5:
                    SelTcr = idx;
                    if (IsColorMode) RecalcColorToValue();
                    else { ApplyTcrRing(); RingsChanged?.Invoke(); }
                    return;
            }
            if (IsColorMode) RecalcColorToValue();
        }

        public void RecalcFromText(string? text)
        {
            ResultText = "";
            double v = ParseInput(text);
            if (v < 0) return;

            FillRingsFromValue(v);
            ApplyTolRing();
            if (_resistorMode == 2) ApplyTcrRing();
            RingsChanged?.Invoke();
        }
        public double SetSeries(int series, string? currentText)
        {
            SelectedSeries = series;
            Preferences.Set("pref_series", series);
            double v = ParseInput(currentText);
            if (v <= 0) return -1;

            double rounded = RoundToSeries(v, series);
            RecalcFromText(FormatR(rounded));
            return rounded;
        }

        // ══════════════════════════════════════════════════════════════
        // INTERNAL CALCULATION LOGIC
        // ══════════════════════════════════════════════════════════════
        private void InitRings()
        {
            Rings.Clear();
            int count = _resistorMode switch { 0 => 4, 1 => 5, _ => 6 };
            for (int i = 0; i < count; i++) Rings.Add(CBlack);
        }
        private void Recalc() => RecalcColorToValue();
        private void RecalcColorToValue()
        {
            if (Rings.Count == 0) return;
            int multExp = SelMul - 2;
            double baseVal;
            if (_resistorMode >= 1)
            {
                baseVal = (Sel1 * 100) + (Sel2 * 10) + Sel3;
                SafeSet(0, DigitColor(Sel1)); SafeSet(1, DigitColor(Sel2));
                SafeSet(2, DigitColor(Sel3)); SafeSet(3, MultColor(multExp));
            }
            else
            {
                baseVal = (Sel1 * 10) + Sel2;
                SafeSet(0, DigitColor(Sel1)); SafeSet(1, DigitColor(Sel2)); SafeSet(2, MultColor(multExp));
            }
            ApplyTolRing();
            if (_resistorMode == 2) ApplyTcrRing();

            double final = baseVal * Math.Pow(10, multExp);
            var tolData = CurrentTolData;
            string tolStr = SelTol < tolData.Length ? tolData[SelTol].Lbl : "";
            string tcrStr = (_resistorMode == 2 && SelTcr < TcrData.Length) ? $" {TcrData[SelTcr].Lbl}ppm" : "";
            ResultText = $"{FormatR(final)}Ω  {tolStr}{tcrStr}";
            RingsChanged?.Invoke();
        }
        private void FillRingsFromValue(double value)
        {
            if (value == 0)
            {
                int cap = _resistorMode >= 1 ? 4 : 3;
                for (int i = 0; i < cap && i < Rings.Count; i++) Rings[i] = CBlack;
                return;
            }

            int sigD = _resistorMode >= 1 ? 3 : 2;
            int mulExp = (int)Math.Floor(Math.Log10(value)) - (sigD - 1);

            if (mulExp < -2 || mulExp > 9) { ResultText = "OUT_OF_RANGE"; return; }

            int sig = (int)Math.Round(value / Math.Pow(10, mulExp));
            if (_resistorMode >= 1)
            {
                SafeSet(0, DigitColor(sig / 100));
                SafeSet(1, DigitColor((sig / 10) % 10));
                SafeSet(2, DigitColor(sig % 10));
                SafeSet(3, MultColor(mulExp));
            }
            else
            {
                SafeSet(0, DigitColor(sig / 10));
                SafeSet(1, DigitColor(sig % 10));
                SafeSet(2, MultColor(mulExp));
            }
        }
        private void ApplyTolRing()
        {
            int ti = _resistorMode == 0 ? 3 : 4;
            var d = CurrentTolData;
            if (SelTol < d.Length) SafeSet(ti, d[SelTol].Bg);
        }
        private void ApplyTcrRing()
        {
            if (SelTcr < TcrData.Length) SafeSet(5, TcrData[SelTcr].Bg);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SafeSet(int idx, Color c)
        {
            if ((uint)idx < (uint)Rings.Count) Rings[idx] = c;
        }

        // ══════════════════════════════════════════════════════════════
        // VALUE PARSING / FORMATTING
        // ══════════════════════════════════════════════════════════════
        public static double ParseInput(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return -1;

            string s = raw.Trim().ToUpperInvariant().Replace(",", ".");
            double mul = 1;

            if (s.EndsWith("K")) { mul = 1e3; s = s[..^1]; }
            else if (s.EndsWith("M")) { mul = 1e6; s = s[..^1]; }
            else if (s.EndsWith("G")) { mul = 1e9; s = s[..^1]; }

            return double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out double r)
                ? r * mul : -1;
        }
        public static string FormatR(double v) => v switch
        {
            >= 1e9 => (v / 1e9).ToString("0.##", CultureInfo.InvariantCulture) + "G",
            >= 1e6 => (v / 1e6).ToString("0.##", CultureInfo.InvariantCulture) + "M",
            >= 1e3 => (v / 1e3).ToString("0.##", CultureInfo.InvariantCulture) + "k",
            _ => Math.Round(v, 2).ToString("0.##", CultureInfo.InvariantCulture)
        };
        public static double RoundToSeries(double value, int series)
        {
            if (value <= 0) return value;

            double decade = Math.Pow(10, Math.Floor(Math.Log10(value)));
            double norm = value / decade, best = 1, minD = double.MaxValue;

            for (int n = 0; n <= series; n++)
            {
                double c = Math.Round(Math.Pow(10, (double)n / series), 2);
                double d = Math.Abs(c - norm);
                if (d < minD) { minD = d; best = c; }
            }
            return Math.Round(best * decade, 3);
        }

        // ══════════════════════════════════════════════════════════════
        // COLOR LOOKUP HELPERS
        // ══════════════════════════════════════════════════════════════
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Color DigitColor(int d) => d switch
        {
            0 => CBlack,
            1 => CBrown,
            2 => CRed,
            3 => COrange,
            4 => CYellow,
            5 => CGreen,
            6 => CBlue,
            7 => CViolet,
            8 => CGray,
            9 => CWhite,
            _ => Colors.Transparent
        };
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Color MultColor(int exp) => exp switch
        {
            -2 => CSilver,
            -1 => CGold,
            0 => CBlack,
            1 => CBrown,
            2 => CRed,
            3 => COrange,
            4 => CYellow,
            5 => CGreen,
            6 => CBlue,
            7 => CViolet,
            8 => CGray,
            9 => CWhite,
            _ => Colors.Transparent
        };

        // ══════════════════════════════════════════════════════════════
        // LOCALIZATION ENGINE
        // ══════════════════════════════════════════════════════════════

        // FIX C: ConcurrentDictionary – thread-sicher für PreloadLang (Background-Thread)
        // vs. T() (UI-Thread)
        private static readonly ConcurrentDictionary<string, Dictionary<string, string>> _langCache = new();

        private static string GetDefaultLanguage()
        {
            var sys = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            bool supported = Array.Exists(_allLanguages, l => l.Code == sys);
            return supported ? sys : "en";
        }

        // FIX A: internal statt private – direkt aus MainPage.Loc() aufrufbar, kein Reflection
        internal string T(string key)
        {
            if (!_langCache.TryGetValue(_lang, out var dict))
                dict = LoadLang(_lang);

            if (dict.TryGetValue(key, out var val)) return val;

            // Fallback to English
            if (_lang != "en")
            {
                if (!_langCache.TryGetValue("en", out var en))
                    en = LoadLang("en");
                if (en.TryGetValue(key, out var enVal)) return enVal;
            }

            return key; // last resort: return the key itself
        }

        private static Dictionary<string, string> LoadLang(string code)
        {
            try
            {
                using var stream = Task.Run(() =>
                    FileSystem.OpenAppPackageFileAsync($"{code}.json")).GetAwaiter().GetResult();
                using var reader = new StreamReader(stream);
                var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(reader.ReadToEnd())
                           ?? new();
                _langCache.TryAdd(code, dict);
                return dict;
            }
            catch
            {
                var empty = new Dictionary<string, string>();
                _langCache.TryAdd(code, empty);
                return empty;
            }
        }

        /// <summary>
        /// Pre-loads the JSON for a given language code into the cache.
        /// Call from a background thread when the user expands "More languages"
        /// so that switching feels instant afterwards.
        /// </summary>
        public static void PreloadLang(string code)
        {
            if (!_langCache.ContainsKey(code))
                LoadLang(code);
        }
    }
}
