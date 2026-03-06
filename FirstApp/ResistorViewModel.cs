using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using Microsoft.Maui.Graphics;

namespace FirstApp
{
    /// <summary>
    /// All business logic, state and localisation.
    /// View (code-behind) only handles animations and chip building.
    /// </summary>
    public sealed class ResistorViewModel : INotifyPropertyChanged
    {
        // ═══════════════════════════════════════════════════════════════════
        // INPC
        // ═══════════════════════════════════════════════════════════════════

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

        // ═══════════════════════════════════════════════════════════════════
        // STATIC COLOR CACHE  – created once at class load, zero allocation on hot path
        // ═══════════════════════════════════════════════════════════════════

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
        internal static readonly Color CNone = Color.FromArgb("#1A1A1A"); // ±20% "invisible"

        // ═══════════════════════════════════════════════════════════════════
        // CHIP DATA  (uses cached colors)
        // ═══════════════════════════════════════════════════════════════════

        public static readonly (Color Bg, string Lbl, Color Fg)[] DigitData =
        {
            (CBlack,  "0", CWhite),  (CBrown,  "1", CWhite),
            (CRed,    "2", CWhite),  (COrange, "3", CBlack),
            (CYellow, "4", CBlack),  (CGreen,  "5", CWhite),
            (CBlue,   "6", CWhite),  (CViolet, "7", CWhite),
            (CGray,   "8", CWhite),  (CWhite,  "9", CBlack),
        };

        public static readonly (Color Bg, string Lbl, Color Fg)[] MultData =
        {
            (CSilver, "×0.01", CBlack), (CGold,   "×0.1",  CBlack),
            (CBlack,  "×1",    CWhite), (CBrown,  "×10",   CWhite),
            (CRed,    "×100",  CWhite), (COrange, "×1k",   CBlack),
            (CYellow, "×10k",  CBlack), (CGreen,  "×100k", CWhite),
            (CBlue,   "×1M",   CWhite), (CViolet, "×10M",  CWhite),
            (CGray,   "×100M", CWhite), (CWhite,  "×1G",   CBlack),
        };

        public static readonly (Color Bg, string Lbl, Color Fg)[] TolData4 =
        {
            (CRed,    "±2%",  CWhite),
            (CGold,   "±5%",  CBlack),
            (CSilver, "±10%", CBlack),
            // ±20%: CNone signals "draw nothing" to the Drawable
            (CNone,   "±20%", CWhite),
        };

        public static readonly (Color Bg, string Lbl, Color Fg)[] TolData5 =
        {
            (CGray,   "±0.05%", CWhite), (CViolet, "±0.1%",  CWhite),
            (CBlue,   "±0.25%", CWhite), (CGreen,  "±0.5%",  CWhite),
            (CBrown,  "±1%",    CWhite), (CRed,    "±2%",    CWhite),
            (CGold,   "±5%",    CBlack),
        };

        public static readonly (Color Bg, string Lbl, Color Fg)[] TcrData =
        {
            (CBrown,  "100", CWhite), (CRed,    "50",  CWhite),
            (COrange, "15",  CBlack), (CYellow, "25",  CBlack),
            (CBlue,   "10",  CWhite), (CViolet, "5",   CWhite),
        };

        // Languages: (code, flag emoji, short display name)
        public static readonly (string Code, string Flag, string Short)[] Languages =
        {
            ("en","🇬🇧","EN"), ("de","🇩🇪","DE"), ("es","🇪🇸","ES"), ("fr","🇫🇷","FR"),
            ("pt","🇵🇹","PT"), ("it","🇮🇹","IT"), ("ja","🇯🇵","JA"), ("zh","🇨🇳","ZH"),
            ("ru","🇷🇺","RU"), ("nl","🇳🇱","NL"), ("ko","🇰🇷","KO"), ("tr","🇹🇷","TR"),
            ("pl","🇵🇱","PL"), ("sv","🇸🇪","SV"), ("da","🇩🇰","DA"), ("no","🇳🇴","NO"),
            ("fi","🇫🇮","FI"), ("cs","🇨🇿","CS"), ("el","🇬🇷","EL"), ("uk","🇺🇦","UK"),
            ("ar","🇸🇦","AR"), ("hi","🇮🇳","HI"), ("ro","🇷🇴","RO"),
        };

        // ═══════════════════════════════════════════════════════════════════
        // RINGS  (shared list – Drawable reads directly, no copy)
        // ═══════════════════════════════════════════════════════════════════

        public readonly List<Color> Rings = new();
        private Color _bodyColor = Colors.SteelBlue;
        public Color BodyColor => _bodyColor;

        // CNone sentinel: Drawable skips this ring entirely (±20%)
        public static bool IsInvisibleRing(Color c) => c == CNone;

        /// Raised after ring list changes so View can call Invalidate()
        public event Action? RingsChanged;

        // ═══════════════════════════════════════════════════════════════════
        // STATE
        // ═══════════════════════════════════════════════════════════════════

        private int _resistorMode = 1;
        private int _inputMode = 0;
        private int _selectedSeries = 12;
        private string _lang = "en";
        private string _resultText = "0 Ω";

        // Band selections – read by View for initial chip highlight
        public int Sel1 = 1;
        public int Sel2 = 0;
        public int Sel3 = 0;
        public int SelMul = 2;
        public int SelTol = 4;  // ±1% default for 5-band
        public int SelTcr = 0;

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
                Notify(nameof(InputModeTitleText));
            }
        }

        public int SelectedSeries
        {
            get => _selectedSeries;
            set => Set(ref _selectedSeries, value);
        }

        public string Lang
        {
            get => _lang;
            set
            {
                if (!Set(ref _lang, value)) return;
                Preferences.Set("pref_lang", _lang);
                NotifyAll(); // refreshes every label binding in one shot
            }
        }

        public string ResultText
        {
            get => _resultText;
            private set => Set(ref _resultText, value);
        }

        // ─── Derived ──────────────────────────────────────────────────────

        public bool Ring3Visible => _resistorMode >= 1;
        public bool TcrVisible => _resistorMode == 2;
        public bool IsColorMode => _inputMode == 1;
        public bool IsValueMode => _inputMode == 0;

        public (Color Bg, string Lbl, Color Fg)[] CurrentTolData
            => _resistorMode == 0 ? TolData4 : TolData5;

        // ─── Localised label properties ────────────────────────────────────

        public string TitleText => T("title");
        public string SubtitleText => T("subtitle");
        public string SwipeTypeHint => T("swipeType");
        public string SwipeInputHint => T("swipeMode");
        public string InputModeTitleText => _inputMode == 0 ? T("valToCol") : T("colToVal");
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

        /// <summary>
        /// Liest die Version direkt aus AppInfo (= ApplicationDisplayVersion in der .csproj).
        /// Nie mehr manuell anpassen nötig.
        /// </summary>
        public string AppVersionText =>
            $"Resistor Calculator v{AppInfo.VersionString}";

        /// <summary>
        /// Adaptive title font size – shrinks for longer translated strings
        /// so the header never overflows on any language.
        /// </summary>
        public double TitleFontSize
        {
            get
            {
                int len = TitleText.Length;
                if (len > 28) return 17.0;
                if (len > 22) return 20.0;
                if (len > 16) return 22.0;
                return 24.0;
            }
        }

        // ═══════════════════════════════════════════════════════════════════
        // CONSTRUCTOR
        // ═══════════════════════════════════════════════════════════════════

        public ResistorViewModel()
        {
            _lang = Preferences.Get("pref_lang", "en");
            _selectedSeries = Preferences.Get("pref_series", 12);
            _resistorMode = Preferences.Get("pref_mode", 1);
            InitRings();
        }

        // ═══════════════════════════════════════════════════════════════════
        // PUBLIC API  (called by View)
        // ═══════════════════════════════════════════════════════════════════

        public void SetMode(int mode)
        {
            Rings.Clear();
            _bodyColor = mode == 0 ? Color.FromArgb("#D2B48C") : Colors.SteelBlue;
            int count = mode switch { 0 => 4, 1 => 5, _ => 6 };
            for (int i = 0; i < count; i++) Rings.Add(CBlack);

            SelTol = mode == 0 ? 1 : 4;
            SelTcr = 0;
            Preferences.Set("pref_mode", mode);
            ResistorMode = mode;
            Recalc();
        }

        public void SetInputMode(int mode)
        {
            InputMode = mode;
            Recalc();
        }

        public void OnBandSelected(int band, int idx)
        {
            switch (band)
            {
                case 0: Sel1 = idx; break;
                case 1: Sel2 = idx; break;
                case 2: Sel3 = idx; break;
                case 3: SelMul = idx; break;
                case 4: SelTol = idx; ApplyTolRing(); RingsChanged?.Invoke(); return;
                case 5: SelTcr = idx; ApplyTcrRing(); RingsChanged?.Invoke(); return;
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

        // ═══════════════════════════════════════════════════════════════════
        // CALCULATION
        // ═══════════════════════════════════════════════════════════════════

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
                SafeSet(0, DigitColor(Sel1));
                SafeSet(1, DigitColor(Sel2));
                SafeSet(2, DigitColor(Sel3));
                SafeSet(3, MultColor(multExp));
            }
            else
            {
                baseVal = (Sel1 * 10) + Sel2;
                SafeSet(0, DigitColor(Sel1));
                SafeSet(1, DigitColor(Sel2));
                SafeSet(2, MultColor(multExp));
            }

            ApplyTolRing();
            if (_resistorMode == 2) ApplyTcrRing();

            double final = baseVal * Math.Pow(10, multExp);
            var tolData = CurrentTolData;
            string tolStr = SelTol < tolData.Length ? tolData[SelTol].Lbl : "";
            string tcrStr = (_resistorMode == 2 && SelTcr < TcrData.Length)
                                ? $" {TcrData[SelTcr].Lbl}ppm" : "";
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
            int tolIdx = _resistorMode == 0 ? 3 : 4;
            var data = CurrentTolData;
            if (SelTol < data.Length) SafeSet(tolIdx, data[SelTol].Bg); // CNone = skip drawing
        }

        private void ApplyTcrRing()
        {
            if (SelTcr < TcrData.Length) SafeSet(5, TcrData[SelTcr].Bg);
        }

        // Bounds-checked ring setter – prevents any index-out-of-range crash
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SafeSet(int idx, Color c)
        {
            if ((uint)idx < (uint)Rings.Count) Rings[idx] = c;
        }

        // ═══════════════════════════════════════════════════════════════════
        // MATH & FORMATTING
        // ═══════════════════════════════════════════════════════════════════

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
            double norm = value / decade;
            double best = 1, minD = double.MaxValue;
            for (int n = 0; n <= series; n++)
            {
                double c = Math.Round(Math.Pow(10, (double)n / series), 2);
                double d = Math.Abs(c - norm);
                if (d < minD) { minD = d; best = c; }
            }
            return Math.Round(best * decade, 3);
        }

        // ═══════════════════════════════════════════════════════════════════
        // COLOR LOOKUPS  – use cached statics, zero allocation
        // ═══════════════════════════════════════════════════════════════════

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

        // ═══════════════════════════════════════════════════════════════════
        // LOCALISATION  – 23 languages
        // To add a language: add one entry per inner dictionary + to Languages[]
        // ═══════════════════════════════════════════════════════════════════

        private string T(string key) =>
            Strings.TryGetValue(key, out var row) && row.TryGetValue(_lang, out var v) ? v : key;

        private static readonly Dictionary<string, Dictionary<string, string>> Strings = new()
        {
            ["title"] = new()
            {
                ["en"] = "Resistor Calculator",
                ["de"] = "Widerstandsrechner",
                ["es"] = "Calculadora Resistencias",
                ["fr"] = "Calculatrice Résistances",
                ["pt"] = "Calculadora Resistores",
                ["it"] = "Calcolatore Resistenze",
                ["ja"] = "抵抗カラーコード計算機",
                ["zh"] = "电阻计算器",
                ["ru"] = "Калькулятор резисторов",
                ["nl"] = "Weerstandscalculator",
                ["ko"] = "저항 계산기",
                ["tr"] = "Direnç Hesaplayıcı",
                ["pl"] = "Kalkulator Rezystorów",
                ["sv"] = "Motståndskalkylator",
                ["da"] = "Modstandsberegner",
                ["no"] = "Motstandskalkulator",
                ["fi"] = "Vastusketjulaskuri",
                ["cs"] = "Kalkulátor Odporů",
                ["el"] = "Υπολογιστής Αντιστάσεων",
                ["uk"] = "Калькулятор Резисторів",
                ["ar"] = "حاسبة المقاومة",
                ["hi"] = "रेज़िस्टर कैलकुलेटर",
                ["ro"] = "Calculator Rezistențe",
            },
            ["subtitle"] = new()
            {
                ["en"] = "Color code and value converter",
                ["de"] = "Farbcode und Wert Umrechner",
                ["es"] = "Conversor código de colores",
                ["fr"] = "Convertisseur code couleur",
                ["pt"] = "Conversor de código de cores",
                ["it"] = "Convertitore codice colore",
                ["ja"] = "カラーコードと値の変換",
                ["zh"] = "色环代码与阻值转换",
                ["ru"] = "Конвертер цветовой маркировки",
                ["nl"] = "Kleurcode en waarde omzetter",
                ["ko"] = "컬러코드 및 저항값 변환",
                ["tr"] = "Renk kodu dönüştürücü",
                ["pl"] = "Przelicznik kodu kolorów",
                ["sv"] = "Färgkod och värdeomvandlare",
                ["da"] = "Farvekode og værdiomsætter",
                ["no"] = "Fargekode og verdiomregner",
                ["fi"] = "Värikoodimuunnin",
                ["cs"] = "Převodník kódu barev",
                ["el"] = "Μετατροπέας κωδικού χρώματος",
                ["uk"] = "Конвертер кольорового маркування",
                ["ar"] = "محول كود الألوان",
                ["hi"] = "रंग कोड कनवर्टर",
                ["ro"] = "Convertor cod culori",
            },
            ["swipeType"] = new()
            {
                ["en"] = "❮  swipe to change type  ❯",
                ["de"] = "❮  wischen zum Wechseln  ❯",
                ["es"] = "❮  deslizar para cambiar  ❯",
                ["fr"] = "❮  glisser pour changer  ❯",
                ["pt"] = "❮  deslize para mudar  ❯",
                ["it"] = "❮  scorri per cambiare  ❯",
                ["ja"] = "❮  スワイプで変更  ❯",
                ["zh"] = "❮  滑动更改类型  ❯",
                ["ru"] = "❮  свайп для смены  ❯",
                ["nl"] = "❮  veeg om te wisselen  ❯",
                ["ko"] = "❮  스와이프로 변경  ❯",
                ["tr"] = "❮  kaydır  ❯",
                ["pl"] = "❮  przesuń aby zmienić  ❯",
                ["sv"] = "❮  svep för att ändra  ❯",
                ["da"] = "❮  stryg for at skifte  ❯",
                ["no"] = "❮  sveip for å endre  ❯",
                ["fi"] = "❮  pyyhkäise vaihtaaksesi  ❯",
                ["cs"] = "❮  přejeďte pro změnu  ❯",
                ["el"] = "❮  σύρτε για αλλαγή  ❯",
                ["uk"] = "❮  свайп для зміни  ❯",
                ["ar"] = "❮  اسحب للتغيير  ❯",
                ["hi"] = "❮  बदलने के लिए स्वाइप करें  ❯",
                ["ro"] = "❮  glisați pentru schimbare  ❯",
            },
            ["swipeMode"] = new()
            {
                ["en"] = "❮  swipe to swap mode  ❯",
                ["de"] = "❮  wischen zum Wechseln  ❯",
                ["es"] = "❮  deslizar para modo  ❯",
                ["fr"] = "❮  glisser pour mode  ❯",
                ["pt"] = "❮  deslize para trocar  ❯",
                ["it"] = "❮  scorri per modo  ❯",
                ["ja"] = "❮  スワイプでモード変更  ❯",
                ["zh"] = "❮  滑动切换模式  ❯",
                ["ru"] = "❮  свайп для режима  ❯",
                ["nl"] = "❮  veeg voor modus  ❯",
                ["ko"] = "❮  모드 변경 스와이프  ❯",
                ["tr"] = "❮  modu değiştir  ❯",
                ["pl"] = "❮  przesuń tryb  ❯",
                ["sv"] = "❮  svep för läge  ❯",
                ["da"] = "❮  stryg for mode  ❯",
                ["no"] = "❮  sveip for modus  ❯",
                ["fi"] = "❮  pyyhkäise tila  ❯",
                ["cs"] = "❮  přejeďte pro režim  ❯",
                ["el"] = "❮  σύρτε για λειτουργία  ❯",
                ["uk"] = "❮  свайп для режиму  ❯",
                ["ar"] = "❮  اسحب لتغيير الوضع  ❯",
                ["hi"] = "❮  मोड बदलने के लिए स्वाइप करें  ❯",
                ["ro"] = "❮  glisați pentru mod  ❯",
            },
            ["valToCol"] = new()
            {
                ["en"] = "Value ➞ Color Mode",
                ["de"] = "Wert ➞ Farbe Modus",
                ["es"] = "Valor ➞ Color",
                ["fr"] = "Valeur ➞ Couleur",
                ["pt"] = "Valor ➞ Cor",
                ["it"] = "Valore ➞ Colore",
                ["ja"] = "値 ➞ カラーコード",
                ["zh"] = "阻值 ➞ 色环",
                ["ru"] = "Значение ➞ Цвет",
                ["nl"] = "Waarde ➞ Kleur",
                ["ko"] = "저항값 ➞ 색코드",
                ["tr"] = "Değer ➞ Renk",
                ["pl"] = "Wartość ➞ Kolor",
                ["sv"] = "Värde ➞ Färg",
                ["da"] = "Værdi ➞ Farve",
                ["no"] = "Verdi ➞ Farge",
                ["fi"] = "Arvo ➞ Väri",
                ["cs"] = "Hodnota ➞ Barva",
                ["el"] = "Τιμή ➞ Χρώμα",
                ["uk"] = "Значення ➞ Колір",
                ["ar"] = "القيمة ➞ اللون",
                ["hi"] = "मान ➞ रंग",
                ["ro"] = "Valoare ➞ Culoare",
            },
            ["colToVal"] = new()
            {
                ["en"] = "Color ➞ Value Mode",
                ["de"] = "Farbe ➞ Wert Modus",
                ["es"] = "Color ➞ Valor",
                ["fr"] = "Couleur ➞ Valeur",
                ["pt"] = "Cor ➞ Valor",
                ["it"] = "Colore ➞ Valore",
                ["ja"] = "カラーコード ➞ 値",
                ["zh"] = "色环 ➞ 阻值",
                ["ru"] = "Цвет ➞ Значение",
                ["nl"] = "Kleur ➞ Waarde",
                ["ko"] = "색코드 ➞ 저항값",
                ["tr"] = "Renk ➞ Değer",
                ["pl"] = "Kolor ➞ Wartość",
                ["sv"] = "Färg ➞ Värde",
                ["da"] = "Farve ➞ Værdi",
                ["no"] = "Farge ➞ Verdi",
                ["fi"] = "Väri ➞ Arvo",
                ["cs"] = "Barva ➞ Hodnota",
                ["el"] = "Χρώμα ➞ Τιμή",
                ["uk"] = "Колір ➞ Значення",
                ["ar"] = "اللون ➞ القيمة",
                ["hi"] = "रंग ➞ मान",
                ["ro"] = "Culoare ➞ Valoare",
            },
            ["resistance"] = new()
            {
                ["en"] = "Resistance (Ω)",
                ["de"] = "Widerstand (Ω)",
                ["es"] = "Resistencia (Ω)",
                ["fr"] = "Résistance (Ω)",
                ["pt"] = "Resistência (Ω)",
                ["it"] = "Resistenza (Ω)",
                ["ja"] = "抵抗値 (Ω)",
                ["zh"] = "电阻值 (Ω)",
                ["ru"] = "Сопротивление (Ω)",
                ["nl"] = "Weerstand (Ω)",
                ["ko"] = "저항값 (Ω)",
                ["tr"] = "Direnç (Ω)",
                ["pl"] = "Rezystancja (Ω)",
                ["sv"] = "Resistans (Ω)",
                ["da"] = "Modstand (Ω)",
                ["no"] = "Motstand (Ω)",
                ["fi"] = "Resistanssi (Ω)",
                ["cs"] = "Odpor (Ω)",
                ["el"] = "Αντίσταση (Ω)",
                ["uk"] = "Опір (Ω)",
                ["ar"] = "المقاومة (Ω)",
                ["hi"] = "प्रतिरोध (Ω)",
                ["ro"] = "Rezistență (Ω)",
            },
            ["normSeries"] = new()
            {
                ["en"] = "Norm Series",
                ["de"] = "Normreihe",
                ["es"] = "Serie Norma",
                ["fr"] = "Série normée",
                ["pt"] = "Série de Norma",
                ["it"] = "Serie Norma",
                ["ja"] = "E系列",
                ["zh"] = "E 系列",
                ["ru"] = "Ряд номиналов",
                ["nl"] = "Normserie",
                ["ko"] = "E 계열",
                ["tr"] = "E Serisi",
                ["pl"] = "Seria E",
                ["sv"] = "E-serie",
                ["da"] = "E-serie",
                ["no"] = "E-serie",
                ["fi"] = "E-sarja",
                ["cs"] = "Řada E",
                ["el"] = "Σειρά E",
                ["uk"] = "Ряд E",
                ["ar"] = "سلسلة E",
                ["hi"] = "E श्रृंखला",
                ["ro"] = "Seria E",
            },
            ["ring1"] = new() { ["en"] = "Ring 1", ["de"] = "Ring 1", ["es"] = "Anillo 1", ["fr"] = "Bande 1", ["pt"] = "Anel 1", ["it"] = "Anello 1", ["ja"] = "リング 1", ["zh"] = "环 1", ["ru"] = "Кольцо 1", ["nl"] = "Ring 1", ["ko"] = "링 1", ["tr"] = "Halka 1", ["pl"] = "Pierścień 1", ["sv"] = "Ring 1", ["da"] = "Ring 1", ["no"] = "Ring 1", ["fi"] = "Rengas 1", ["cs"] = "Pruh 1", ["el"] = "Δαχτυλίδι 1", ["uk"] = "Кільце 1", ["ar"] = "حلقة 1", ["hi"] = "रिंग 1", ["ro"] = "Inel 1" },
            ["ring2"] = new() { ["en"] = "Ring 2", ["de"] = "Ring 2", ["es"] = "Anillo 2", ["fr"] = "Bande 2", ["pt"] = "Anel 2", ["it"] = "Anello 2", ["ja"] = "リング 2", ["zh"] = "环 2", ["ru"] = "Кольцо 2", ["nl"] = "Ring 2", ["ko"] = "링 2", ["tr"] = "Halka 2", ["pl"] = "Pierścień 2", ["sv"] = "Ring 2", ["da"] = "Ring 2", ["no"] = "Ring 2", ["fi"] = "Rengas 2", ["cs"] = "Pruh 2", ["el"] = "Δαχτυλίδι 2", ["uk"] = "Кільце 2", ["ar"] = "حلقة 2", ["hi"] = "रिंग 2", ["ro"] = "Inel 2" },
            ["ring3"] = new() { ["en"] = "Ring 3", ["de"] = "Ring 3", ["es"] = "Anillo 3", ["fr"] = "Bande 3", ["pt"] = "Anel 3", ["it"] = "Anello 3", ["ja"] = "リング 3", ["zh"] = "环 3", ["ru"] = "Кольцо 3", ["nl"] = "Ring 3", ["ko"] = "링 3", ["tr"] = "Halka 3", ["pl"] = "Pierścień 3", ["sv"] = "Ring 3", ["da"] = "Ring 3", ["no"] = "Ring 3", ["fi"] = "Rengas 3", ["cs"] = "Pruh 3", ["el"] = "Δαχτυλίδι 3", ["uk"] = "Кільце 3", ["ar"] = "حلقة 3", ["hi"] = "रिंग 3", ["ro"] = "Inel 3" },
            ["mult"] = new() { ["en"] = "Mult.", ["de"] = "Mult.", ["es"] = "Mult.", ["fr"] = "Mult.", ["pt"] = "Mult.", ["it"] = "Molt.", ["ja"] = "乗数", ["zh"] = "倍率", ["ru"] = "Множ.", ["nl"] = "Mult.", ["ko"] = "배율", ["tr"] = "Çarp.", ["pl"] = "Mnożnik", ["sv"] = "Mult.", ["da"] = "Mult.", ["no"] = "Mult.", ["fi"] = "Kerroin", ["cs"] = "Násobič", ["el"] = "Πολλαπλ.", ["uk"] = "Множ.", ["ar"] = "مضاعف", ["hi"] = "गुणक", ["ro"] = "Mult." },
            ["tol"] = new() { ["en"] = "Tol.", ["de"] = "Tol.", ["es"] = "Tol.", ["fr"] = "Tol.", ["pt"] = "Tol.", ["it"] = "Tol.", ["ja"] = "公差", ["zh"] = "误差", ["ru"] = "Доп.", ["nl"] = "Tol.", ["ko"] = "허용오차", ["tr"] = "Tol.", ["pl"] = "Tol.", ["sv"] = "Tol.", ["da"] = "Tol.", ["no"] = "Tol.", ["fi"] = "Tol.", ["cs"] = "Tol.", ["el"] = "Ανοχή", ["uk"] = "Допуск", ["ar"] = "تفاوت", ["hi"] = "सहनशीलता", ["ro"] = "Tol." },
            ["tcr"] = new() { ["en"] = "TCR", ["de"] = "TCR", ["es"] = "TCR", ["fr"] = "TCR", ["pt"] = "TCR", ["it"] = "TCR", ["ja"] = "TCR", ["zh"] = "TCR", ["ru"] = "TCR", ["nl"] = "TCR", ["ko"] = "TCR", ["tr"] = "TCR", ["pl"] = "TCR", ["sv"] = "TCR", ["da"] = "TCR", ["no"] = "TCR", ["fi"] = "TCR", ["cs"] = "TCR", ["el"] = "TCR", ["uk"] = "TCR", ["ar"] = "TCR", ["hi"] = "TCR", ["ro"] = "TCR" },
            ["settings"] = new() { ["en"] = "Settings", ["de"] = "Einstellungen", ["es"] = "Ajustes", ["fr"] = "Paramètres", ["pt"] = "Configurações", ["it"] = "Impostazioni", ["ja"] = "設定", ["zh"] = "设置", ["ru"] = "Настройки", ["nl"] = "Instellingen", ["ko"] = "설정", ["tr"] = "Ayarlar", ["pl"] = "Ustawienia", ["sv"] = "Inställningar", ["da"] = "Indstillinger", ["no"] = "Innstillinger", ["fi"] = "Asetukset", ["cs"] = "Nastavení", ["el"] = "Ρυθμίσεις", ["uk"] = "Налаштування", ["ar"] = "الإعدادات", ["hi"] = "सेटिंग्स", ["ro"] = "Setări" },
            ["language"] = new() { ["en"] = "Language", ["de"] = "Sprache", ["es"] = "Idioma", ["fr"] = "Langue", ["pt"] = "Idioma", ["it"] = "Lingua", ["ja"] = "言語", ["zh"] = "语言", ["ru"] = "Язык", ["nl"] = "Taal", ["ko"] = "언어", ["tr"] = "Dil", ["pl"] = "Język", ["sv"] = "Språk", ["da"] = "Sprog", ["no"] = "Språk", ["fi"] = "Kieli", ["cs"] = "Jazyk", ["el"] = "Γλώσσα", ["uk"] = "Мова", ["ar"] = "اللغة", ["hi"] = "भाषा", ["ro"] = "Limbă" },
            ["about"] = new() { ["en"] = "About", ["de"] = "Über die App", ["es"] = "Acerca de", ["fr"] = "À propos", ["pt"] = "Sobre", ["it"] = "Informazioni", ["ja"] = "アプリについて", ["zh"] = "关于", ["ru"] = "О приложении", ["nl"] = "Over", ["ko"] = "앱 정보", ["tr"] = "Hakkında", ["pl"] = "O aplikacji", ["sv"] = "Om appen", ["da"] = "Om appen", ["no"] = "Om appen", ["fi"] = "Tietoja", ["cs"] = "O aplikaci", ["el"] = "Σχετικά", ["uk"] = "Про додаток", ["ar"] = "حول التطبيق", ["hi"] = "ऐप के बारे में", ["ro"] = "Despre" },
            ["aboutDesc"] = new()
            {
                ["en"] = "Color code calculator for 4-, 5- and 6-band resistors",
                ["de"] = "Farbcode-Rechner für 4-, 5- und 6-Band-Widerstände",
                ["es"] = "Calculadora de código de colores para resistencias",
                ["fr"] = "Calculatrice code couleur pour résistances",
                ["pt"] = "Calculadora de código de cores para resistores",
                ["it"] = "Calcolatore codice colore per resistenze",
                ["ja"] = "4・5・6バンド抵抗器のカラーコード計算機",
                ["zh"] = "4环、5环和6环电阻色环代码计算器",
                ["ru"] = "Калькулятор цветовой маркировки резисторов",
                ["nl"] = "Kleurcode calculator voor weerstanden",
                ["ko"] = "4, 5, 6밴드 저항의 컬러코드 계산기",
                ["tr"] = "4, 5 ve 6 bantlı dirençler için renk kodu hesaplayıcı",
                ["pl"] = "Kalkulator kodu kolorów dla rezystorów",
                ["sv"] = "Färgkodskalkylator för motstånd",
                ["da"] = "Farvekodeberegner for modstande",
                ["no"] = "Fargekodekalkulator for motstander",
                ["fi"] = "Värikoodilaskuri vastuksille",
                ["cs"] = "Kalkulátor kódu barev pro odpory",
                ["el"] = "Υπολογιστής κωδικού χρώματος για αντιστάσεις",
                ["uk"] = "Калькулятор кольорового маркування резисторів",
                ["ar"] = "حاسبة كود الألوان للمقاومات",
                ["hi"] = "रेज़िस्टर के लिए रंग कोड कैलकुलेटर",
                ["ro"] = "Calculator cod culori pentru rezistențe",
            },
            // FIX: 2025 → 2026
            ["copyright"] = new()
            {
                ["en"] = "© 2026 – All rights reserved",
                ["de"] = "© 2026 – Alle Rechte vorbehalten",
                ["es"] = "© 2026 – Todos los derechos reservados",
                ["fr"] = "© 2026 – Tous droits réservés",
                ["pt"] = "© 2026 – Todos os direitos reservados",
                ["it"] = "© 2026 – Tutti i diritti riservati",
                ["ja"] = "© 2026 – 全著作権所有",
                ["zh"] = "© 2026 – 保留所有权利",
                ["ru"] = "© 2026 – Все права защищены",
                ["nl"] = "© 2026 – Alle rechten voorbehouden",
                ["ko"] = "© 2026 – 모든 권리 보유",
                ["tr"] = "© 2026 – Tüm hakları saklıdır",
                ["pl"] = "© 2026 – Wszelkie prawa zastrzeżone",
                ["sv"] = "© 2026 – Alla rättigheter förbehållna",
                ["da"] = "© 2026 – Alle rettigheder forbeholdes",
                ["no"] = "© 2026 – Alle rettigheter forbeholdt",
                ["fi"] = "© 2026 – Kaikki oikeudet pidätetään",
                ["cs"] = "© 2026 – Všechna práva vyhrazena",
                ["el"] = "© 2026 – Με επιφύλαξη παντός δικαιώματος",
                ["uk"] = "© 2026 – Всі права захищені",
                ["ar"] = "© 2026 – جميع الحقوق محفوظة",
                ["hi"] = "© 2026 – सर्वाधिकार सुरक्षित",
                ["ro"] = "© 2026 – Toate drepturile rezervate",
            },
            ["close"] = new() { ["en"] = "Close", ["de"] = "Schließen", ["es"] = "Cerrar", ["fr"] = "Fermer", ["pt"] = "Fechar", ["it"] = "Chiudi", ["ja"] = "閉じる", ["zh"] = "关闭", ["ru"] = "Закрыть", ["nl"] = "Sluiten", ["ko"] = "닫기", ["tr"] = "Kapat", ["pl"] = "Zamknij", ["sv"] = "Stäng", ["da"] = "Luk", ["no"] = "Lukk", ["fi"] = "Sulje", ["cs"] = "Zavřít", ["el"] = "Κλείσιμο", ["uk"] = "Закрити", ["ar"] = "إغلاق", ["hi"] = "बंद करें", ["ro"] = "Închide" },
            ["band4"] = new() { ["en"] = "4-Band (Standard)", ["de"] = "4-Band (Standard)", ["es"] = "4 bandas (Estándar)", ["fr"] = "4 bandes (Standard)", ["pt"] = "4 bandas (Padrão)", ["it"] = "4 bande (Standard)", ["ja"] = "4バンド（標準）", ["zh"] = "4环（标准）", ["ru"] = "4-полосный (Стандарт)", ["nl"] = "4-bands (Standaard)", ["ko"] = "4밴드 (표준)", ["tr"] = "4 Bantlı (Standart)", ["pl"] = "4-pasmowy (Standard)", ["sv"] = "4-bands (Standard)", ["da"] = "4-bands (Standard)", ["no"] = "4-bands (Standard)", ["fi"] = "4-kaista (Vakio)", ["cs"] = "4-pásmový (Standard)", ["el"] = "4 λωρίδες (Τυπικό)", ["uk"] = "4-смуговий (Стандарт)", ["ar"] = "4 أشرطة (قياسي)", ["hi"] = "4-बैंड (मानक)", ["ro"] = "4 benzi (Standard)" },
            ["band5"] = new() { ["en"] = "5-Band (Precision)", ["de"] = "5-Band (Präzision)", ["es"] = "5 bandas (Precisión)", ["fr"] = "5 bandes (Précision)", ["pt"] = "5 bandas (Precisão)", ["it"] = "5 bande (Precisione)", ["ja"] = "5バンド（精密）", ["zh"] = "5环（精密）", ["ru"] = "5-полосный (Точный)", ["nl"] = "5-bands (Precisie)", ["ko"] = "5밴드 (정밀)", ["tr"] = "5 Bantlı (Hassas)", ["pl"] = "5-pasmowy (Precyzja)", ["sv"] = "5-bands (Precision)", ["da"] = "5-bands (Præcision)", ["no"] = "5-bands (Presisjon)", ["fi"] = "5-kaista (Tarkkuus)", ["cs"] = "5-pásmový (Přesnost)", ["el"] = "5 λωρίδες (Ακρίβεια)", ["uk"] = "5-смуговий (Точний)", ["ar"] = "5 أشرطة (دقيق)", ["hi"] = "5-बैंड (सटीक)", ["ro"] = "5 benzi (Precizie)" },
            ["band6"] = new() { ["en"] = "6-Band (TCR)", ["de"] = "6-Band (TCR)", ["es"] = "6 bandas (TCR)", ["fr"] = "6 bandes (TCR)", ["pt"] = "6 bandas (TCR)", ["it"] = "6 bande (TCR)", ["ja"] = "6バンド（TCR）", ["zh"] = "6环（TCR）", ["ru"] = "6-полосный (TCR)", ["nl"] = "6-bands (TCR)", ["ko"] = "6밴드 (TCR)", ["tr"] = "6 Bantlı (TCR)", ["pl"] = "6-pasmowy (TCR)", ["sv"] = "6-bands (TCR)", ["da"] = "6-bands (TCR)", ["no"] = "6-bands (TCR)", ["fi"] = "6-kaista (TCR)", ["cs"] = "6-pásmový (TCR)", ["el"] = "6 λωρίδες (TCR)", ["uk"] = "6-смуговий (TCR)", ["ar"] = "6 أشرطة (TCR)", ["hi"] = "6-बैंड (TCR)", ["ro"] = "6 benzi (TCR)" },
            ["warning"] = new() { ["en"] = "Warning!", ["de"] = "Warnung!", ["es"] = "¡Advertencia!", ["fr"] = "Attention !", ["pt"] = "Aviso!", ["it"] = "Attenzione!", ["ja"] = "警告！", ["zh"] = "警告！", ["ru"] = "Предупреждение!", ["nl"] = "Waarschuwing!", ["ko"] = "경고!", ["tr"] = "Uyarı!", ["pl"] = "Ostrzeżenie!", ["sv"] = "Varning!", ["da"] = "Advarsel!", ["no"] = "Advarsel!", ["fi"] = "Varoitus!", ["cs"] = "Varování!", ["el"] = "Προειδοποίηση!", ["uk"] = "Попередження!", ["ar"] = "تحذير!", ["hi"] = "चेतावनी!", ["ro"] = "Atenție!" },
            ["outOfRange"] = new()
            {
                ["en"] = "Value out of range for standard resistor colors.",
                ["de"] = "Wert außerhalb des Bereichs für Standard-Widerstandsfarben.",
                ["es"] = "Valor fuera de rango para colores estándar.",
                ["fr"] = "Valeur hors plage pour les codes couleur standard.",
                ["pt"] = "Valor fora do intervalo para cores padrão.",
                ["it"] = "Valore fuori range per i colori standard.",
                ["ja"] = "値が標準カラーコードの範囲外です。",
                ["zh"] = "数值超出标准色环范围。",
                ["ru"] = "Значение вне диапазона стандартных цветов.",
                ["nl"] = "Waarde buiten bereik voor standaard weerstandskleuren.",
                ["ko"] = "값이 표준 저항 색코드 범위를 벗어났습니다.",
                ["tr"] = "Değer standart direnç renk kodları için aralık dışında.",
                ["pl"] = "Wartość poza zakresem standardowych kolorów rezystorów.",
                ["sv"] = "Värdet är utanför intervallet för standardmotståndsfärger.",
                ["da"] = "Værdien er uden for området for standard modstandsfarver.",
                ["no"] = "Verdi utenfor området for standard motstandsfarger.",
                ["fi"] = "Arvo on standardivastusvärien alueen ulkopuolella.",
                ["cs"] = "Hodnota mimo rozsah pro standardní barvy odporů.",
                ["el"] = "Τιμή εκτός εύρους για τυπικά χρώματα αντιστάσεων.",
                ["uk"] = "Значення виходить за межі стандартних кольорів.",
                ["ar"] = "القيمة خارج نطاق ألوان المقاومة القياسية.",
                ["hi"] = "मान मानक प्रतिरोधक रंगों की सीमा से बाहर है।",
                ["ro"] = "Valoare în afara intervalului pentru culorile standard.",
            },
        };
    }
}