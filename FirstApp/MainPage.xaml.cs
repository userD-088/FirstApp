using System.ComponentModel;
using FirstApp.Core;
using FirstApp.UI;

namespace FirstApp;

public partial class MainPage : ContentPage
{
    private readonly ResistorViewModel _vm;

    // Chips per band: 0-2 digits, 3 multiplier, 4 tolerance, 5 TCR
    private readonly List<Border>[] _bandChips = { new(), new(), new(), new(), new(), new() };
    private readonly List<Border> _seriesChips = new();
    private readonly Border[] _unitTabs = new Border[4];
    private readonly Label[] _unitLabels = new Label[4];
    private bool _colorRowsBuilt;
    private int _tolChipsMode = -1;

    // Settings snapshot (read once, updated by the settings sheet)
    private bool _anim;
    private bool _haptics;

    // Animation guards
    private bool _resAnimBusy, _inputAnimBusy, _sheetBusy, _introPlayed;
    private const uint SlideMs = 200;
    private const double OutSlide = 0.30;

    // Bottom sheet
    private enum SheetKind { None, Settings, Reference }
    private SheetKind _sheet = SheetKind.None;
    private SettingsView? _settingsView;
    private View? _referenceView;
    private string _referenceKey = "";

    // Cyber FX
    private readonly ScanlinesDrawable _scanlines = new();
    private readonly CornerBracketDrawable _resCorners = new(), _inputCorners = new();
    private CancellationTokenSource? _fxCts;

    private int _toastVersion;

    // ══════════════════════════════════════════════════════════════
    // CONSTRUCTOR
    // ══════════════════════════════════════════════════════════════
    public MainPage()
    {
        InitializeComponent();

        Theme.Set(AppSettings.Theme);
        _anim = AppSettings.Animations;
        _haptics = AppSettings.Haptics;

        _vm = new ResistorViewModel();
        BindingContext = _vm;

        ResistorView.Drawable = new ResistorDrawable(_vm);
        ScanlinesView.Drawable = _scanlines;
        ResCardCorners.Drawable = _resCorners;
        InputCardCorners.Drawable = _inputCorners;

        BuildUnitTabs();
        SheetOverlay.Opacity = 0;

        _vm.RingsChanged += OnRingsChanged;
        _vm.PropertyChanged += OnVmPropertyChanged;

        ApplyTheme();                       // also builds series + tolerance chips
        if (_vm.IsColorMode) EnsureColorRows();
        if (_vm.Mode == 2) RebuildTcrChips();
    }

    /// <summary>Called by the app when it goes to the background.</summary>
    public void SaveState() => _vm.SaveState();

    // ══════════════════════════════════════════════════════════════
    // LIFECYCLE
    // ══════════════════════════════════════════════════════════════
    protected override void OnAppearing()
    {
        base.OnAppearing();
        UpdateWindowTitle();
        ApplySystemBars();
        StartCyberEffects();

        if (!_introPlayed && _anim && Theme.Current.HasCyberFx)
        {
            _introPlayed = true;
            _ = PlayIntroAnimation();
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        StopCyberEffects();
        _vm.SaveState();
    }

    protected override bool OnBackButtonPressed()
    {
        if (_sheet != SheetKind.None) { _ = CloseSheet(); return true; }
        return base.OnBackButtonPressed();
    }

    // ══════════════════════════════════════════════════════════════
    // VIEWMODEL EVENTS
    // ══════════════════════════════════════════════════════════════
    private void OnRingsChanged()
    {
        ResistorView.Invalidate();
        RefreshChipSelection();
    }

    private void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case "" or null:            // language changed → everything
                UpdateAllTabs();
                UpdateStatusColor();
                UpdateWindowTitle();
                RebuildAllChips();
                break;
            case nameof(ResistorViewModel.Mode):
                RebuildToleranceChips();
                if (_vm.Mode == 2) RebuildTcrChips();
                UpdateResTabs();
                break;
            case nameof(ResistorViewModel.InputMode):
                if (_vm.IsColorMode) EnsureColorRows();
                UpdateInputTabs();
                break;
            case nameof(ResistorViewModel.UnitIndex):
                UpdateUnitTabs();
                break;
            case nameof(ResistorViewModel.Series):
                UpdateSeriesChips();
                break;
            case nameof(ResistorViewModel.StatusIsError):
                UpdateStatusColor();
                break;
        }
    }

    private void UpdateStatusColor()
        => StatusLabel.TextColor = _vm.StatusIsError ? Theme.Current.Error : Theme.Current.TextSecondary;

    // ══════════════════════════════════════════════════════════════
    // THEME
    // ══════════════════════════════════════════════════════════════
    private void ApplyTheme()
    {
        var t = Theme.Current;
        bool cyber = t.HasCyberFx;

        Background = Theme.Gradient(t.PageBg, diagonal: true);
        HeaderBdr.Background = Theme.Gradient(t.HeaderBg, diagonal: false);

        Shadow? shadow = t.CardShadow
            ? new Shadow { Brush = Colors.Black, Opacity = 0.08f, Radius = 18, Offset = new Point(0, 6) }
            : null;
        foreach (var card in new[] { ResCardBdr, InputCardBdr })
        {
            card.Stroke = t.CardStroke ?? new SolidColorBrush(t.Accent);
            card.StrokeThickness = t.CardStrokeThickness;
            card.StrokeShape = Theme.Round(t.CardRadius);
            card.Shadow = shadow!;
        }
        ResCardBdr.Background = Theme.Gradient(t.CardBg, diagonal: true);
        InputCardBdr.Background = new SolidColorBrush(t.InputCardBg);

        foreach (var b in new[] { ResTabsBdr, InputTabsBdr, UnitTabsBdr, EntryBdr })
            b.StrokeShape = Theme.Round(t.ControlRadius);
        SuggestionBdr.Stroke = t.Accent;
        SuggestionBdr.BackgroundColor = t.HighBg;
        SuggestionBdr.StrokeShape = Theme.Round(t.ControlRadius);

        ResultLabel.TextColor = t.Result;

        SheetPanel.Stroke = t.CardStroke ?? new SolidColorBrush(t.Accent);
        SheetPanel.StrokeThickness = Math.Max(1, t.CardStrokeThickness);
        SheetPanel.StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle
        {
            CornerRadius = new CornerRadius(t.CardRadius + 2, t.CardRadius + 2, 0, 0),
        };
        SheetPanel.Background = Theme.Gradient(new[] { t.SheetTop, t.SheetBottom }, diagonal: false);

        _resCorners.Accent = t.Accent;
        _inputCorners.Accent = t.Accent;
        ResCardCorners.IsVisible = cyber;
        InputCardCorners.IsVisible = cyber;
        ResCardCorners.Invalidate();
        InputCardCorners.Invalidate();
        ScanlinesView.IsVisible = cyber;

        StopCyberEffects();
        StartCyberEffects();

        RebuildAllChips();
        UpdateAllTabs();
        UpdateSeriesChips();
        UpdateStatusColor();
        ResistorView.Invalidate();
        ApplySystemBars();
    }

    private void UpdateAllTabs()
    {
        UpdateResTabs();
        UpdateInputTabs();
        UpdateUnitTabs();
    }

    private void ApplySystemBars()
    {
        bool fs = AppSettings.Fullscreen;
        HeaderBdr.Padding = new Thickness(12, fs ? 40 : 14, 12, 18);
        var t = Theme.Current;
        var bar = t.HeaderBg[0].Alpha >= 1 ? t.HeaderBg[0] : t.PageBg[0];
        SystemUi.Apply(fs, bar, t.IsLight);
    }

    // ══════════════════════════════════════════════════════════════
    // CYBER EFFECTS  (only with Cyber theme + animations enabled)
    // ══════════════════════════════════════════════════════════════
    private void StartCyberEffects()
    {
        // Animations need a platform handler → started from OnAppearing at the earliest
        if (Handler == null || !Theme.Current.HasCyberFx || !_anim || _fxCts != null) return;
        _fxCts = new CancellationTokenSource();
        var token = _fxCts.Token;

        // Scanlines: translate a pre-drawn layer → no redraw per frame
        var scroll = new Animation(v => ScanlinesView.TranslationY = v, 0, ScanlinesDrawable.Spacing * 2);
        scroll.Commit(ScanlinesView, "scan", 16, 450, Easing.Linear, repeat: () => !token.IsCancellationRequested);

        _ = RunGlitchLoop(token);
    }

    private void StopCyberEffects()
    {
        _fxCts?.Cancel();
        _fxCts?.Dispose();
        _fxCts = null;
        if (Handler != null) ScanlinesView.AbortAnimation("scan");
        ScanlinesView.TranslationY = 0;
        ResultLabel.Opacity = 1;
    }

    private async Task RunGlitchLoop(CancellationToken token)
    {
        var rng = Random.Shared;
        try
        {
            while (!token.IsCancellationRequested)
            {
                await Task.Delay(rng.Next(3000, 9000), token);
                int flickers = rng.Next(2, 5);
                for (int i = 0; i < flickers; i++)
                {
                    ResultLabel.Opacity = 0.08;
                    await Task.Delay(rng.Next(30, 55), token);
                    ResultLabel.Opacity = 1.0;
                    await Task.Delay(rng.Next(35, 90), token);
                }
            }
        }
        catch (TaskCanceledException) { }
        finally { ResultLabel.Opacity = 1.0; }
    }

    private async Task PlayIntroAnimation()
    {
        const uint ms = 170; const double offset = 38;
        View[] parts = { HeaderBdr, ResCardBdr, InputCardBdr };
        foreach (var p in parts) { p.Opacity = 0; p.TranslationY = offset; }
        await Task.Yield();
        for (int i = 0; i < parts.Length; i++)
        {
            var p = parts[i];
            var a = Task.WhenAll(p.FadeToAsync(1, ms, Easing.CubicOut), p.TranslateToAsync(0, 0, ms, Easing.CubicOut));
            if (i == parts.Length - 1) await a; else await Task.Delay(55);
        }
    }

    private async Task Bounce(View v)
    {
        if (!_anim || !Theme.Current.HasCyberFx) return;
        await v.ScaleToAsync(0.86, 55, Easing.CubicIn);
        await v.ScaleToAsync(1.0, 75, Easing.SpringOut);
    }

    // ══════════════════════════════════════════════════════════════
    // FEEDBACK
    // ══════════════════════════════════════════════════════════════
    internal void Tick()
    {
        if (!_haptics) return;
        try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { /* not supported */ }
    }

    private async void ShowToast(string text)
    {
        int version = ++_toastVersion;
        ToastLabel.Text = text;
        Toast.IsVisible = true;
        if (_anim) { Toast.Opacity = 0; await Toast.FadeToAsync(1, 140); } else Toast.Opacity = 1;
        await Task.Delay(1500);
        if (version != _toastVersion) return;
        if (_anim) await Toast.FadeToAsync(0, 200);
        if (version == _toastVersion) Toast.IsVisible = false;
    }

    private async void OnResultTapped(object? sender, TappedEventArgs e)
    {
        if (!_vm.HasValidValue) return;
        try
        {
            await Clipboard.Default.SetTextAsync(_vm.ClipboardText);
            Tick();
            ShowToast(_vm["copied"]);
        }
        catch { /* clipboard unavailable */ }
    }

    private void OnSuggestionTapped(object? sender, TappedEventArgs e)
    {
        Tick();
        _vm.ApplySuggestion();
    }

    // ══════════════════════════════════════════════════════════════
    // RESISTOR TYPE (4 / 5 / 6 band)
    // ══════════════════════════════════════════════════════════════
    private void OnResTabTapped(object? sender, TappedEventArgs e)
    {
        if (e.Parameter is string s && int.TryParse(s, out int mode)) _ = SwitchResistorMode(mode);
    }

    private async Task SwitchResistorMode(int newMode)
    {
        if (_vm.Mode == newMode || _resAnimBusy) return;
        Tick();
        if (!_anim) { _vm.SetMode(newMode); return; }

        _resAnimBusy = true;
        int dir = newMode > _vm.Mode ? 1 : -1;
        double w = ResistorViewContainer.Width > 0 ? ResistorViewContainer.Width : 300;
        try
        {
            await Task.WhenAll(
                ResistorView.TranslateToAsync(-dir * w * OutSlide, 0, SlideMs, Easing.CubicIn),
                ResistorView.FadeToAsync(0, SlideMs, Easing.CubicIn));

            _vm.SetMode(newMode);
            var active = newMode switch { 0 => ResTab0, 1 => ResTab1, _ => ResTab2 };
            _ = Bounce(active);

            ResistorView.TranslationX = dir * w;
            await Task.WhenAll(
                ResistorView.TranslateToAsync(0, 0, SlideMs, Easing.CubicOut),
                ResistorView.FadeToAsync(1, SlideMs, Easing.CubicOut));
        }
        finally
        {
            ResistorView.TranslationX = 0; ResistorView.Opacity = 1;
            _resAnimBusy = false;
        }
    }

    private void UpdateResTabs()
    {
        Ui.SetSegTab(ResTab0, ResTabLabel0, _vm.Mode == 0);
        Ui.SetSegTab(ResTab1, ResTabLabel1, _vm.Mode == 1);
        Ui.SetSegTab(ResTab2, ResTabLabel2, _vm.Mode == 2);
    }

    // ══════════════════════════════════════════════════════════════
    // INPUT MODE (value ↔ colour)
    // ══════════════════════════════════════════════════════════════
    private void OnInputTabTapped(object? sender, TappedEventArgs e)
    {
        if (e.Parameter is string s && int.TryParse(s, out int mode)) _ = SwitchInputMode(mode);
    }

    private async Task SwitchInputMode(int newMode)
    {
        if (_vm.InputMode == newMode || _inputAnimBusy) return;
        Tick();
        ResistanceEntry.Unfocus();
        if (newMode == 1) EnsureColorRows();
        if (!_anim) { _vm.SetInputMode(newMode); return; }

        _inputAnimBusy = true;
        int dir = newMode > _vm.InputMode ? 1 : -1;
        double w = InputContentGrid.Width > 0 ? InputContentGrid.Width : 300;
        VisualElement outgoing = _vm.IsValueMode ? ValueToColorLayout : ColorToValueLayout;
        VisualElement incoming = newMode == 0 ? ValueToColorLayout : ColorToValueLayout;
        try
        {
            await Task.WhenAll(
                outgoing.TranslateToAsync(-dir * w * OutSlide, 0, SlideMs, Easing.CubicIn),
                outgoing.FadeToAsync(0, SlideMs, Easing.CubicIn));

            _vm.SetInputMode(newMode);
            _ = Bounce(newMode == 0 ? InputTab0 : InputTab1);

            incoming.TranslationX = dir * w;
            incoming.Opacity = 0;
            await Task.WhenAll(
                incoming.TranslateToAsync(0, 0, SlideMs, Easing.CubicOut),
                incoming.FadeToAsync(1, SlideMs, Easing.CubicOut));
        }
        finally
        {
            outgoing.TranslationX = 0; outgoing.Opacity = 1;
            incoming.TranslationX = 0; incoming.Opacity = 1;
            _inputAnimBusy = false;
        }
    }

    private void UpdateInputTabs()
    {
        Ui.SetSegTab(InputTab0, InputTabLabel0, _vm.IsValueMode);
        Ui.SetSegTab(InputTab1, InputTabLabel1, _vm.IsColorMode);
    }

    // ══════════════════════════════════════════════════════════════
    // UNIT SEGMENTS
    // ══════════════════════════════════════════════════════════════
    private void BuildUnitTabs()
    {
        for (int i = 0; i < 4; i++)
        {
            int idx = i;
            var lbl = new Label
            {
                Text = ResistorViewModel.UnitLabels[i],
                FontSize = 14,
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center,
            };
            var tab = new Border { StrokeThickness = 0, Padding = new Thickness(0, 10), Content = lbl };
            tab.OnTap(() => { Tick(); _vm.UnitIndex = idx; });
            Grid.SetColumn(tab, i);
            UnitGrid.Children.Add(tab);
            _unitTabs[i] = tab; _unitLabels[i] = lbl;
        }
    }

    private void UpdateUnitTabs()
    {
        for (int i = 0; i < 4; i++) Ui.SetSegTab(_unitTabs[i], _unitLabels[i], i == _vm.UnitIndex);
    }

    // ══════════════════════════════════════════════════════════════
    // CHIPS
    // ══════════════════════════════════════════════════════════════
    private void BuildChipRow(HorizontalStackLayout host, int band, BandOption[] data, bool pill)
    {
        var list = _bandChips[band];
        host.Children.Clear();
        list.Clear();
        for (int i = 0; i < data.Length; i++)
        {
            int idx = i;
            var opt = data[i];
            var chip = Ui.ColorChip(opt, pill, false, $"{_vm[opt.Color.Key]} {opt.Label}");
            chip.OnTap(() =>
            {
                Tick();
                _vm.SelectBand(band, idx);
                _ = Bounce(chip);
            });
            list.Add(chip);
            host.Children.Add(chip);
        }
    }

    /// <summary>Colour → value rows are only needed in colour mode, so they are built lazily.</summary>
    private void EnsureColorRows()
    {
        if (_colorRowsBuilt) return;
        _colorRowsBuilt = true;
        BuildChipRow(Band1ChipsLayout, 0, ColorCodes.Digits, false);
        BuildChipRow(Band2ChipsLayout, 1, ColorCodes.Digits, false);
        BuildChipRow(Band3ChipsLayout, 2, ColorCodes.Digits, false);
        BuildChipRow(MultiplierChipsLayout, 3, ColorCodes.Multipliers, true);
        RefreshChipSelection();
    }

    private void RebuildToleranceChips()
    {
        if (_tolChipsMode == (_vm.Mode == 0 ? 0 : 1)) return;
        _tolChipsMode = _vm.Mode == 0 ? 0 : 1;
        BuildChipRow(ToleranceChipsLayout, 4, _vm.ToleranceOptions, true);
        RefreshChipSelection();
    }

    private void RebuildTcrChips()
    {
        if (_bandChips[5].Count > 0) return;
        BuildChipRow(TcrChipsLayout, 5, ColorCodes.Tcr, true);
        RefreshChipSelection();
    }

    private void RebuildAllChips()
    {
        // Language (accessibility text) or theme (radius, empty-chip colour) changed
        if (_colorRowsBuilt) { _colorRowsBuilt = false; EnsureColorRows(); }
        _tolChipsMode = -1; RebuildToleranceChips();
        BuildSeriesChips();
        if (_bandChips[5].Count > 0) { _bandChips[5].Clear(); RebuildTcrChips(); }
    }

    private void RefreshChipSelection()
    {
        for (int band = 0; band < 6; band++)
        {
            var list = _bandChips[band];
            if (list.Count == 0) continue;
            int sel = band switch
            {
                <= 2 => _vm.Digit(band),
                3 => _vm.MultiplierIndex,
                4 => _vm.ToleranceIndex,
                _ => _vm.TcrIndex,
            };
            for (int i = 0; i < list.Count; i++) Ui.SetChipSelected(list[i], i == sel);
        }
    }

    private void BuildSeriesChips()
    {
        SeriesChipsLayout.Children.Clear();
        _seriesChips.Clear();
        foreach (int s in ResistorMath.SeriesList)
        {
            int series = s;
            var chip = Ui.TextChip($"E{s}", s == _vm.Series);
            chip.OnTap(() => { Tick(); _vm.Series = series; _ = Bounce(chip); });
            _seriesChips.Add(chip);
            SeriesChipsLayout.Children.Add(chip);
        }
    }

    private void UpdateSeriesChips()
    {
        for (int i = 0; i < _seriesChips.Count; i++)
            Ui.SetTextChipSelected(_seriesChips[i], ResistorMath.SeriesList[i] == _vm.Series);
    }

    // ══════════════════════════════════════════════════════════════
    // BOTTOM SHEET (settings / reference)
    // ══════════════════════════════════════════════════════════════
    private void OnOpenSettings(object? sender, TappedEventArgs e) => _ = OpenSheet(SheetKind.Settings);
    private void OnOpenReference(object? sender, TappedEventArgs e) => _ = OpenSheet(SheetKind.Reference);
    private void OnSheetBackdropTapped(object? sender, TappedEventArgs e) => _ = CloseSheet();
    private void OnSheetCloseTapped(object? sender, TappedEventArgs e) => _ = CloseSheet();
    private void OnSheetPanelTapped(object? sender, TappedEventArgs e) { /* swallow taps */ }
    internal void CloseSheetFromChild() => _ = CloseSheet();

    private async Task OpenSheet(SheetKind kind)
    {
        if (_sheetBusy || _sheet == kind) return;
        _sheetBusy = true;
        Tick();
        ResistanceEntry.Unfocus();
        try
        {
            if (kind == SheetKind.Settings)
            {
                _settingsView ??= new SettingsView(this, _vm);
                _settingsView.Rebuild();
                SheetScroll.Content = _settingsView;
                SheetTitle.Text = _vm["settings"];
            }
            else
            {
                string key = $"{_vm.Lang}|{Theme.Index}|{_vm.Series}";
                if (_referenceView == null || _referenceKey != key)
                {
                    _referenceView = ReferenceView.Build(_vm);
                    _referenceKey = key;
                }
                SheetScroll.Content = _referenceView;
                SheetTitle.Text = _vm["refTitle"];
            }

            double h = Height > 0 ? Height : 800;
            SheetScroll.MaximumHeightRequest = Math.Max(280, h * 0.86 - 110);

            SheetOverlay.IsVisible = true;
            _ = SheetScroll.ScrollToAsync(0, 0, false);
            if (_anim)
            {
                SheetPanel.TranslationY = h;
                SheetOverlay.Opacity = 0;
                await Task.WhenAll(
                    SheetOverlay.FadeToAsync(1, 200, Easing.CubicOut),
                    SheetPanel.TranslateToAsync(0, 0, 260, Easing.CubicOut));
            }
            else
            {
                SheetOverlay.Opacity = 1;
                SheetPanel.TranslationY = 0;
            }
            _sheet = kind;
        }
        finally { _sheetBusy = false; }
    }

    private async Task CloseSheet()
    {
        if (_sheetBusy || _sheet == SheetKind.None) return;
        _sheetBusy = true;
        try
        {
            if (_anim)
            {
                double h = Height > 0 ? Height : 800;
                await Task.WhenAll(
                    SheetOverlay.FadeToAsync(0, 180, Easing.CubicIn),
                    SheetPanel.TranslateToAsync(0, h, 220, Easing.CubicIn));
            }
            SheetOverlay.IsVisible = false;
            SheetOverlay.Opacity = 0;
            _sheet = SheetKind.None;
            _vm.SaveState();
        }
        finally { _sheetBusy = false; }
    }

    private void OnSheetPanUpdated(object? sender, PanUpdatedEventArgs e)
    {
        if (_sheet == SheetKind.None || _sheetBusy) return;
        switch (e.StatusType)
        {
            case GestureStatus.Running:
                SheetPanel.TranslationY = Math.Max(0, e.TotalY);
                break;
            case GestureStatus.Completed:
            case GestureStatus.Canceled:
                if (SheetPanel.TranslationY > 110) _ = CloseSheet();
                else _ = SheetPanel.TranslateToAsync(0, 0, 150, Easing.CubicOut);
                break;
        }
    }

    // ══════════════════════════════════════════════════════════════
    // SETTINGS CALLBACKS (invoked by SettingsView)
    // ══════════════════════════════════════════════════════════════
    internal void SelectTheme(int index)
    {
        if (index == Theme.Index) return;
        Theme.Set(index);
        AppSettings.Theme = Theme.Index;
        ApplyTheme();
        _settingsView?.Rebuild();
    }

    internal void SelectLanguage(string code)
    {
        _vm.Lang = code;
        SheetTitle.Text = _vm["settings"];
        _settingsView?.Rebuild();
    }

    internal bool Animations
    {
        get => _anim;
        set
        {
            _anim = value; AppSettings.Animations = value;
            StopCyberEffects(); StartCyberEffects();
        }
    }

    internal bool Haptics
    {
        get => _haptics;
        set { _haptics = value; AppSettings.Haptics = value; if (value) Tick(); }
    }

    internal bool Fullscreen
    {
        get => AppSettings.Fullscreen;
        set { AppSettings.Fullscreen = value; ApplySystemBars(); }
    }

    internal bool ShowRange
    {
        get => _vm.ShowRange;
        set { _vm.ShowRange = value; AppSettings.ShowRange = value; }
    }

    internal bool RememberState
    {
        get => AppSettings.RememberState;
        set { AppSettings.RememberState = value; if (!value) AppSettings.LastState = ""; else _vm.SaveState(); }
    }

    internal async Task ResetSettings()
    {
        bool ok = await DisplayAlertAsync(_vm["resetSettings"], _vm["resetConfirm"], _vm["yes"], _vm["cancel"]);
        if (!ok) return;

        AppSettings.ResetToDefaults();
        _anim = AppSettings.Animations;
        _haptics = AppSettings.Haptics;
        _vm.ShowRange = AppSettings.ShowRange;
        _vm.Series = AppSettings.Series;
        _vm.ResetInput();
        Theme.Set(AppSettings.Theme);
        ApplyTheme();
        _settingsView?.Rebuild();
        ShowToast(_vm["resetDone"]);
    }

    // ══════════════════════════════════════════════════════════════
    // WINDOW TITLE (Android recents)
    // ══════════════════════════════════════════════════════════════
    private void UpdateWindowTitle()
    {
#if ANDROID
        var activity = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity;
        if (activity == null) return;
        string title = _vm["title"];
        activity.Title = title;
        if (OperatingSystem.IsAndroidVersionAtLeast(33))
            activity.SetTaskDescription(new Android.App.ActivityManager.TaskDescription.Builder().SetLabel(title).Build());
        else
#pragma warning disable CA1422
            activity.SetTaskDescription(new Android.App.ActivityManager.TaskDescription(title));
#pragma warning restore CA1422
#endif
    }
}
