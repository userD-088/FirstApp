using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;

namespace FirstApp
{
    public partial class MainPage : ContentPage
    {
        private readonly ResistorViewModel _vm;

        // --- Chip lists
        private readonly List<Border> _chips1 = new();
        private readonly List<Border> _chips2 = new();
        private readonly List<Border> _chips3 = new();
        private readonly List<Border> _chipsMul = new();
        private readonly List<Border> _chipsTol = new();
        private readonly List<Border> _chipsTcr = new();

        // --- Unit dropdown
        // 0 = Ohm  1 = K  2 = M  3 = G
        private static readonly string[] UnitLabels = { "Ω", "K", "M", "G" };
        private static readonly string[] UnitSuffixes = { "", "k", "M", "G" };
        private int _unitIdx = 0;
        private bool _unitDropdownOpen = false;
        private bool _unitAnimBusy = false;
        private bool _suppressTextChanged = false;

        // --- Animation guards
        private bool _resAnimBusy;
        private bool _inputAnimBusy;
        private bool _settingsOpen;

        // --- Text debounce
        private CancellationTokenSource? _textCts;

        // --- Animation constants
        private const uint SlideMs = 200;
        private const double OutSlide = 0.30;
        private const uint DropMs = 160;

        // --- Language section state
        private bool _langExpanded = false;
        private bool _secondaryChipsBuilt = false;

        // INIT

        public MainPage()
        {
            InitializeComponent();

            _vm = new ResistorViewModel();
            BindingContext = _vm;

            _vm.RingsChanged += () => ResistorView?.Invalidate();
            _vm.PropertyChanged += OnVmPropertyChanged;

            BuildChipRow(Band1ChipsLayout, _chips1, ResistorViewModel.DigitData, 0, _vm.Sel1);
            BuildChipRow(Band2ChipsLayout, _chips2, ResistorViewModel.DigitData, 1, _vm.Sel2);
            BuildChipRow(Band3ChipsLayout, _chips3, ResistorViewModel.DigitData, 2, _vm.Sel3);
            BuildChipRow(MultiplierChipsLayout, _chipsMul, ResistorViewModel.MultData, 3, _vm.SelMul, pill: true);

            RebuildToleranceChips();
            RebuildSeriesChips();
            RebuildPrimaryLangChips();

            // Prepare unit dropdown
            UnitDropdown.Opacity = 0;
            UnitDropdown.IsVisible = false;
            ApplyUnitHighlight();

            UpdateResistorTypeTabs();
            UpdateInputModeTabs();

            ResistorView.Drawable = new ResistorDrawable(_vm.Rings, () => _vm.BodyColor);

            SettingsPanel.TranslationY = 2000;
            SettingsOverlay.Opacity = 0;

            _vm.SetMode(_vm.ResistorMode);
        }

        // --- Language / locale change
        private void OnVmPropertyChanged(object? sender,
            System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName is "" or nameof(ResistorViewModel.Lang))
            {
                UpdateResistorTypeTabs();
                UpdateInputModeTabs();
                UpdateMoreToggleLabel();
            }
        }

        // --- JIT pre-warm
        protected override void OnAppearing()
        {
            base.OnAppearing();
            _ = PreWarmJit();
        }

        private async Task PreWarmJit()
        {
            await Task.Yield();
            await Task.WhenAll(
                ResistorView.FadeTo(1, 1),
                ResistorView.TranslateTo(0, 0, 1),
                ValueToColorLayout.FadeTo(1, 1),
                ValueToColorLayout.TranslateTo(0, 0, 1),
                ColorToValueLayout.FadeTo(1, 1),
                ColorToValueLayout.TranslateTo(0, 0, 1),
                SettingsPanel.TranslateTo(0, SettingsPanel.TranslationY, 1),
                SettingsOverlay.FadeTo(0, 1)
            );
        }

        // UNIT DROPDOWN  (Ohm / K / M / G)

        private async void OnUnitBtnTapped(object? sender, TappedEventArgs e)
        {
            if (_unitAnimBusy) return;

            if (_unitDropdownOpen) await CloseUnitDropdown();
            else await OpenUnitDropdown();
        }

        private async Task OpenUnitDropdown()
        {
            if (_unitAnimBusy) return;
            _unitAnimBusy = true;
            _unitDropdownOpen = true;

            // Absolute position of UnitBtn relative to RootGrid
            var pos = GetAbsolutePosition(UnitBtn, RootGrid);
            double left = pos.X;  // left edge of button
            double top = pos.Y + UnitBtn.Height + 4;  // just below button

            UnitDropdown.Margin = new Thickness(left, top, 0, 0);
            UnitDropdown.TranslationY = -10;
            UnitDropdown.Opacity = 0;
            UnitDropdown.IsVisible = true;
            UnitChevron.Text = "▴";

            await Task.WhenAll(
                UnitDropdown.FadeTo(1, DropMs, Easing.CubicOut),
                UnitDropdown.TranslateTo(0, 0, DropMs, Easing.CubicOut)
            );

            _unitAnimBusy = false;
        }

        // Walks up the visual tree from element to relativeTo, summing X/Y offsets.
        private static Point GetAbsolutePosition(VisualElement element, VisualElement relativeTo)
        {
            double x = 0, y = 0;
            VisualElement? current = element;
            while (current != null && current != relativeTo)
            {
                x += current.X;
                y += current.Y;
                current = current.Parent as VisualElement;
            }
            return new Point(x, y);
        }

        private async Task CloseUnitDropdown()
        {
            if (_unitAnimBusy || !_unitDropdownOpen) return;
            _unitAnimBusy = true;
            _unitDropdownOpen = false;
            UnitChevron.Text = "▾";

            await Task.WhenAll(
                UnitDropdown.FadeTo(0, DropMs, Easing.CubicIn),
                UnitDropdown.TranslateTo(0, -10, DropMs, Easing.CubicIn)
            );

            UnitDropdown.IsVisible = false;
            _unitAnimBusy = false;
        }

        // -- Option taps
        private void OnUnitOpt0Tapped(object? s, TappedEventArgs e) => _ = SelectUnit(0);
        private void OnUnitOpt1Tapped(object? s, TappedEventArgs e) => _ = SelectUnit(1);
        private void OnUnitOpt2Tapped(object? s, TappedEventArgs e) => _ = SelectUnit(2);
        private void OnUnitOpt3Tapped(object? s, TappedEventArgs e) => _ = SelectUnit(3);

        private async Task SelectUnit(int idx)
        {
            _unitIdx = idx;
            ApplyUnitHighlight();
            await CloseUnitDropdown();
            TriggerRecalc();
        }

        // Updates button label and option highlight silently (no animation).
        private void SetUnitSilent(int idx)
        {
            _unitIdx = idx;
            ApplyUnitHighlight();
        }

        private void ApplyUnitHighlight()
        {
            // Button label
            UnitBtnLabel.Text = UnitLabels[_unitIdx];

            // Highlight active option
            Border[] opts = { UnitOpt0, UnitOpt1, UnitOpt2, UnitOpt3 };
            for (int i = 0; i < opts.Length; i++)
            {
                bool active = i == _unitIdx;
                opts[i].BackgroundColor = active
                    ? Color.FromArgb("#2563EB")
                    : Color.FromArgb("#1A2535");
                if (opts[i].Content is Label lbl)
                {
                    lbl.TextColor = active ? Colors.White : Color.FromArgb("#94A3B8");
                    lbl.FontAttributes = active ? FontAttributes.Bold : FontAttributes.None;
                }
            }
        }

        // RESISTOR-TYPE TABS

        private void OnResTab0Tapped(object? sender, TappedEventArgs e) => _ = SwitchResistorMode(0);
        private void OnResTab1Tapped(object? sender, TappedEventArgs e) => _ = SwitchResistorMode(1);
        private void OnResTab2Tapped(object? sender, TappedEventArgs e) => _ = SwitchResistorMode(2);

        private async Task SwitchResistorMode(int newMode)
        {
            if (_vm.ResistorMode == newMode || _resAnimBusy) return;
            _resAnimBusy = true;

            int dir = newMode > _vm.ResistorMode ? 1 : -1;
            double w = ResistorViewContainer.Width > 0 ? ResistorViewContainer.Width : 300;

            await Task.WhenAll(
                ResistorView.TranslateTo(-dir * w * OutSlide, 0, SlideMs, Easing.CubicIn),
                ResistorView.FadeTo(0, SlideMs, Easing.CubicIn)
            );

            _vm.SetMode(newMode);
            RebuildToleranceChips();
            if (newMode == 2) RebuildTcrChips();
            UpdateResistorTypeTabs();
            ResistorView.Invalidate();

            ResistorView.TranslationX = dir * w;
            ResistorView.Opacity = 0;

            await Task.WhenAll(
                ResistorView.TranslateTo(0, 0, SlideMs, Easing.CubicOut),
                ResistorView.FadeTo(1, SlideMs, Easing.CubicOut)
            );

            _resAnimBusy = false;
        }

        private void UpdateResistorTypeTabs()
        {
            SetSegTab(ResTab0, ResTabLabel0, _vm.ResistorMode == 0);
            SetSegTab(ResTab1, ResTabLabel1, _vm.ResistorMode == 1);
            SetSegTab(ResTab2, ResTabLabel2, _vm.ResistorMode == 2);
        }

        // INPUT-MODE TABS

        private void OnInputTab0Tapped(object? sender, TappedEventArgs e) => _ = SwitchInputMode(0);
        private void OnInputTab1Tapped(object? sender, TappedEventArgs e) => _ = SwitchInputMode(1);

        private async Task SwitchInputMode(int newMode)
        {
            if (_vm.InputMode == newMode || _inputAnimBusy) return;
            _inputAnimBusy = true;

            // close unit dropdown if open
            if (_unitDropdownOpen) await CloseUnitDropdown();

            int dir = newMode > _vm.InputMode ? 1 : -1;
            double w = InputContentGrid.Width > 0 ? InputContentGrid.Width : 300;
            var outgoing = _vm.IsValueMode
                ? (VisualElement)ValueToColorLayout
                : ColorToValueLayout;

            await Task.WhenAll(
                outgoing.TranslateTo(-dir * w * OutSlide, 0, SlideMs, Easing.CubicIn),
                outgoing.FadeTo(0, SlideMs, Easing.CubicIn)
            );

            _vm.SetInputMode(newMode);
            UpdateInputModeTabs();

            var incoming = newMode == 0
                ? (VisualElement)ValueToColorLayout
                : ColorToValueLayout;
            incoming.TranslationX = dir * w;
            incoming.Opacity = 0;

            await Task.WhenAll(
                incoming.TranslateTo(0, 0, SlideMs, Easing.CubicOut),
                incoming.FadeTo(1, SlideMs, Easing.CubicOut)
            );

            outgoing.TranslationX = 0;
            outgoing.Opacity = 1;

            _inputAnimBusy = false;
        }

        private void UpdateInputModeTabs()
        {
            bool v = _vm.IsValueMode;
            SetSegTab(InputTab0, InputTabLabel0, v);
            SetSegTab(InputTab1, InputTabLabel1, !v);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void SetSegTab(Border tab, Label lbl, bool active)
        {
            tab.BackgroundColor = active ? Color.FromArgb("#2563EB") : Colors.Transparent;
            lbl.TextColor = active ? Colors.White : Color.FromArgb("#64748B");
            lbl.FontAttributes = active ? FontAttributes.Bold : FontAttributes.None;
        }

        // CHIP BUILDERS

        private void BuildChipRow(
            HorizontalStackLayout host, List<Border> list,
            (Color Bg, string Lbl, Color Fg)[] data,
            int band, int sel, bool pill = false)
        {
            host.Children.Clear();
            list.Clear();
            for (int i = 0; i < data.Length; i++)
            {
                int idx = i;
                var chip = MakeChip(data[i].Bg, data[i].Lbl, data[i].Fg, pill, i == sel);
                chip.GestureRecognizers.Add(Tap(() =>
                {
                    SetSel(list, idx);
                    _vm.OnBandSelected(band, idx);
                }));
                list.Add(chip);
                host.Children.Add(chip);
            }
        }

        private void RebuildToleranceChips() =>
            BuildChipRow(ToleranceChipsLayout, _chipsTol, _vm.CurrentTolData, 4, _vm.SelTol, pill: true);

        private void RebuildTcrChips() =>
            BuildChipRow(TcrChipsLayout, _chipsTcr, ResistorViewModel.TcrData, 5, _vm.SelTcr, pill: true);

        private void RebuildSeriesChips()
        {
            SeriesChipsMain.Children.Clear();
            int[] vals = { 3, 6, 12, 24, 48, 96, 192 };
            foreach (int s in vals)
            {
                int cap = s;
                var chip = MakeSeriesChip(s, s == _vm.SelectedSeries);
                chip.GestureRecognizers.Add(Tap(() =>
                {
                    string fullText = BuildFullText(ResistanceEntry.Text);
                    double rounded = _vm.SetSeries(cap, fullText);

                    if (rounded > 0)
                    {
                        string formatted = ResistorViewModel.FormatR(rounded);
                        int newUnit = 0;
                        string numPart = formatted;

                        if (formatted.EndsWith("G")) { newUnit = 3; numPart = formatted[..^1]; }
                        else if (formatted.EndsWith("M")) { newUnit = 2; numPart = formatted[..^1]; }
                        else if (formatted.EndsWith("k")) { newUnit = 1; numPart = formatted[..^1]; }

                        _suppressTextChanged = true;
                        ResistanceEntry.Text = numPart;
                        _suppressTextChanged = false;

                        SetUnitSilent(newUnit);
                        TriggerRecalc();
                    }

                    RebuildSeriesChips();
                }));
                SeriesChipsMain.Children.Add(chip);
            }
        }

        // -- Language chip helpers
        private void RebuildPrimaryLangChips()
        {
            _langExpanded = false;
            _secondaryChipsBuilt = false;
            LangChipsSecondary.Children.Clear();

            MoreLangsRow.IsVisible = false;
            MoreLangsRow.Opacity = 1;
            MoreLangsRow.TranslationY = 0;

            UpdateMoreToggleLabel();

            LangChipsLayout.Children.Clear();
            foreach (var (code, flag, shortName) in ResistorViewModel.PrimaryLanguages)
                LangChipsLayout.Children.Add(MakeLangChip(code, flag, shortName));
        }

        private void EnsureSecondaryChipsBuilt()
        {
            if (_secondaryChipsBuilt) return;
            _secondaryChipsBuilt = true;

            LangChipsSecondary.Children.Clear();
            foreach (var (code, flag, shortName) in ResistorViewModel.SecondaryLanguages)
                LangChipsSecondary.Children.Add(MakeLangChip(code, flag, shortName));

            _ = Task.Run(() =>
            {
                foreach (var (code, _, _) in ResistorViewModel.SecondaryLanguages)
                    ResistorViewModel.PreloadLang(code);
            });
        }

        private Border MakeLangChip(string code, string flag, string shortName)
        {
            string cap = code;
            bool sel = code == _vm.Lang;
            bool hasEmoji = !string.IsNullOrWhiteSpace(flag);

            View chipContent;
            if (hasEmoji)
            {
                chipContent = new Label
                {
                    Text = $"{flag}  {shortName}",
                    TextColor = Colors.White,
                    FontSize = 13,
                    FontAttributes = sel ? FontAttributes.Bold : FontAttributes.None,
                    HorizontalTextAlignment = TextAlignment.Center,
                    VerticalTextAlignment = TextAlignment.Center,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center,
                };
            }
            else
            {
                chipContent = new HorizontalStackLayout
                {
                    Spacing = 5,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center,
                    Children =
                    {
                        new Image
                        {
                            Source        = ImageSource.FromFile($"flag_{code}.png"),
                            HeightRequest = 14, WidthRequest = 22,
                            Aspect        = Aspect.AspectFit,
                            VerticalOptions = LayoutOptions.Center,
                        },
                        new Label
                        {
                            Text           = shortName,
                            TextColor      = Colors.White,
                            FontSize       = 13,
                            FontAttributes = sel ? FontAttributes.Bold : FontAttributes.None,
                            VerticalTextAlignment = TextAlignment.Center,
                            VerticalOptions       = LayoutOptions.Center,
                        }
                    }
                };
            }

            var chip = new Border
            {
                StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(14) },
                BackgroundColor = sel ? Color.FromArgb("#2563EB") : Color.FromArgb("#1F2937"),
                Stroke = sel ? Colors.White : Colors.Transparent,
                StrokeThickness = sel ? 1.5 : 0,
                Padding = new Thickness(12, 8),
                HeightRequest = 44,
                Content = chipContent
            };

            chip.GestureRecognizers.Add(Tap(() =>
            {
                bool wasExpanded = _langExpanded;
                _vm.Lang = cap;
                RebuildPrimaryLangChips();

                if (wasExpanded)
                {
                    _langExpanded = true;
                    EnsureSecondaryChipsBuilt();
                    MoreLangsRow.IsVisible = true;
                    MoreLangsRow.Opacity = 1;
                    MoreLangsRow.TranslationY = 0;
                    UpdateMoreToggleLabel();
                }
            }));
            return chip;
        }

        private async void OnMoreLangsTapped(object sender, TappedEventArgs e)
        {
            _langExpanded = !_langExpanded;
            UpdateMoreToggleLabel();

            if (_langExpanded)
            {
                EnsureSecondaryChipsBuilt();
                MoreLangsRow.Opacity = 0;
                MoreLangsRow.TranslationY = -12;
                MoreLangsRow.IsVisible = true;
                await Task.WhenAll(
                    MoreLangsRow.FadeTo(1, 230, Easing.CubicOut),
                    MoreLangsRow.TranslateTo(0, 0, 230, Easing.CubicOut)
                );
            }
            else
            {
                await Task.WhenAll(
                    MoreLangsRow.FadeTo(0, 180, Easing.CubicIn),
                    MoreLangsRow.TranslateTo(0, -12, 180, Easing.CubicIn)
                );
                MoreLangsRow.IsVisible = false;
                MoreLangsRow.TranslationY = 0;
            }
        }

        private void UpdateMoreToggleLabel()
        {
            string more = string.IsNullOrEmpty(_vm.MoreLangsText) || _vm.MoreLangsText == "moreLangs"
                          ? "More languages" : _vm.MoreLangsText;
            string less = string.IsNullOrEmpty(_vm.LessLangsText) || _vm.LessLangsText == "lessLangs"
                          ? "Less languages" : _vm.LessLangsText;
            MoreLangsLabel.Text = _langExpanded ? $"▲  {less}" : $"▼  {more}";
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void SetSel(List<Border> list, int idx)
        {
            for (int i = 0; i < list.Count; i++)
            {
                bool on = i == idx;
                list[i].Stroke = on ? Colors.White : Colors.Transparent;
                list[i].StrokeThickness = on ? 2.5 : 0;
            }
        }

        private static Border MakeChip(Color bg, string lbl, Color fg, bool pill, bool sel)
        {
            bool empty = bg == Colors.Transparent || bg == ResistorViewModel.CNone;
            var chip = new Border
            {
                BackgroundColor = empty ? Color.FromArgb("#2A2A2A") : bg,
                StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(pill ? 10 : 18) },
                Stroke = sel ? Colors.White : Colors.Transparent,
                StrokeThickness = sel ? 2.5 : 0,
                HeightRequest = 36,
                Padding = pill ? new Thickness(12, 0) : new Thickness(0),
                Content = new Label
                {
                    Text = lbl,
                    TextColor = fg,
                    FontSize = pill ? 11 : 13,
                    FontAttributes = FontAttributes.Bold,
                    HorizontalTextAlignment = TextAlignment.Center,
                    VerticalTextAlignment = TextAlignment.Center,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center,
                }
            };
            if (!pill) chip.WidthRequest = 36;
            return chip;
        }

        private static Border MakeSeriesChip(int s, bool sel) => new Border
        {
            StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(12) },
            BackgroundColor = sel ? Color.FromArgb("#2563EB") : Color.FromArgb("#1F2937"),
            Stroke = sel ? Colors.White : Colors.Transparent,
            StrokeThickness = sel ? 1.5 : 0,
            Padding = new Thickness(14, 8),
            HeightRequest = 38,
            Content = new Label
            {
                Text = $"E{s}",
                TextColor = Colors.White,
                FontSize = 13,
                FontAttributes = sel ? FontAttributes.Bold : FontAttributes.None,
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
            }
        };

        private static TapGestureRecognizer Tap(Action a)
        {
            var t = new TapGestureRecognizer();
            t.Tapped += (_, _) => a();
            return t;
        }

        // TEXT ENTRY  (debounced 80 ms)

        private void OnResistanceTextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_vm.IsValueMode || _suppressTextChanged) return;

            // -- Auto-detect a trailing unit suffix typed by the user
            string raw = e.NewTextValue ?? "";
            string upper = raw.TrimEnd().ToUpperInvariant();

            int detectedUnit = -1;
            if (upper.EndsWith("G")) detectedUnit = 3;
            else if (upper.EndsWith("M")) detectedUnit = 2;
            else if (upper.EndsWith("K")) detectedUnit = 1;
            else if (upper.EndsWith("Ω") || upper.EndsWith("O")) detectedUnit = 0;

            if (detectedUnit >= 0)
            {
                _suppressTextChanged = true;
                ResistanceEntry.Text = raw[..^1];
                _suppressTextChanged = false;
                SetUnitSilent(detectedUnit);
            }

            // Close dropdown if open (user started typing)
            if (_unitDropdownOpen) _ = CloseUnitDropdown();

            TriggerRecalc();
        }

        private void TriggerRecalc()
        {
            _textCts?.Cancel();
            _textCts = new CancellationTokenSource();
            var token = _textCts.Token;

            Task.Delay(80, token).ContinueWith(_ =>
            {
                if (token.IsCancellationRequested) return;
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    string fullText = BuildFullText(ResistanceEntry.Text);
                    _vm.RecalcFromText(fullText);
                    if (_vm.ResultText == "OUT_OF_RANGE")
                        DisplayAlert(_vm.WarningTitle, _vm.OutOfRangeText, "OK");
                });
            }, TaskScheduler.Default);
        }

        private string BuildFullText(string? entryText)
        {
            string num = entryText?.Trim() ?? "";
            return string.IsNullOrEmpty(num) ? num : num + UnitSuffixes[_unitIdx];
        }

        // SETTINGS PANEL

        private async void OnOpenSettings(object sender, EventArgs e)
        {
            if (_settingsOpen) return;
            _settingsOpen = true;

            if (_unitDropdownOpen) await CloseUnitDropdown();
            RebuildPrimaryLangChips();

            SettingsOverlay.IsVisible = true;
            SettingsOverlay.InputTransparent = false;

            await Task.WhenAll(
                SettingsOverlay.FadeTo(1, 220, Easing.CubicOut),
                SettingsPanel.TranslateTo(0, 0, 260, Easing.CubicOut)
            );
        }

        private async void OnCloseSettings(object sender, EventArgs e)
        {
            if (!_settingsOpen) return;
            SettingsOverlay.InputTransparent = true;

            await Task.WhenAll(
                SettingsOverlay.FadeTo(0, 200, Easing.CubicIn),
                SettingsPanel.TranslateTo(0, 2000, 240, Easing.CubicIn)
            );

            SettingsOverlay.IsVisible = false;
            _settingsOpen = false;
        }

        private void OnSettingsBackdropTapped(object sender, TappedEventArgs e)
            => OnCloseSettings(sender, e);
        private void OnSettingsPanelTapped(object sender, TappedEventArgs e) { /* consume */ }

        // RESISTOR DRAWABLE

        public sealed class ResistorDrawable : IDrawable
        {
            private readonly List<Color> _rings;
            private readonly Func<Color> _body;

            public ResistorDrawable(List<Color> rings, Func<Color> body)
            { _rings = rings; _body = body; }

            public void Draw(ICanvas canvas, RectF rect)
            {
                const float Bw = 210f, Bh = 56f, Rw = 13f, Gap = 25f;
                float cx = rect.Width / 2, cy = rect.Height / 2;
                float bx = cx - Bw / 2, by = cy - Bh / 2;

                canvas.StrokeColor = Colors.Silver;
                canvas.StrokeSize = 5;
                canvas.StrokeLineCap = LineCap.Round;
                canvas.DrawLine(bx - 65, cy, bx, cy);
                canvas.DrawLine(bx + Bw, cy, bx + Bw + 65, cy);

                canvas.FillColor = Colors.Black.WithAlpha(0.22f);
                canvas.FillRoundedRectangle(bx, by + 5, Bw, Bh, 28);

                canvas.FillColor = _body();
                canvas.FillRoundedRectangle(bx, by, Bw, Bh, 28);

                canvas.FillColor = Colors.White.WithAlpha(0.12f);
                canvas.FillRoundedRectangle(bx + 10, by + 7, Bw - 20, Bh / 3f, 18);

                float x = bx + 30;
                foreach (var c in _rings)
                {
                    if (!ResistorViewModel.IsInvisibleRing(c))
                    {
                        canvas.FillColor = c;
                        canvas.FillRectangle(x, by, Rw, Bh);
                    }
                    x += Gap;
                }
            }
        }
    }
}