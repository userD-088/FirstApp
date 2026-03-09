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

        // ─── Chip lists ───────────────────────────────────────────────────
        private readonly List<Border> _chips1 = new();
        private readonly List<Border> _chips2 = new();
        private readonly List<Border> _chips3 = new();
        private readonly List<Border> _chipsMul = new();
        private readonly List<Border> _chipsTol = new();
        private readonly List<Border> _chipsTcr = new();

        // ─── Animation guards ─────────────────────────────────────────────
        private bool _resAnimBusy;
        private bool _inputAnimBusy;
        private bool _settingsOpen;

        // ─── Text debounce ────────────────────────────────────────────────
        private CancellationTokenSource? _textCts;

        // ─── Animation constants ──────────────────────────────────────────
        private const uint SlideMs = 200;
        private const double OutSlide = 0.30;

        // ─── Language section state ───────────────────────────────────────
        private bool _langExpanded = false;
        private bool _secondaryChipsBuilt = false;

        // ══════════════════════════════════════════════════════════════════
        // INIT
        // ══════════════════════════════════════════════════════════════════

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

            UpdateResistorTypeTabs();
            UpdateInputModeTabs();

            ResistorView.Drawable = new ResistorDrawable(_vm.Rings, () => _vm.BodyColor);

            SettingsPanel.TranslationY = 2000;
            SettingsOverlay.Opacity = 0;

            _vm.SetMode(_vm.ResistorMode);
        }

        // ─── Language / locale change ─────────────────────────────────────
        private void OnVmPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName is "" or nameof(ResistorViewModel.Lang))
            {
                UpdateResistorTypeTabs();
                UpdateInputModeTabs();
                UpdateMoreToggleLabel();
            }
        }

        // ─── JIT pre-warm ─────────────────────────────────────────────────
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

        // ══════════════════════════════════════════════════════════════════
        // RESISTOR-TYPE TABS
        // ══════════════════════════════════════════════════════════════════

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

        // ══════════════════════════════════════════════════════════════════
        // INPUT-MODE TABS
        // ══════════════════════════════════════════════════════════════════

        private void OnInputTab0Tapped(object? sender, TappedEventArgs e) => _ = SwitchInputMode(0);
        private void OnInputTab1Tapped(object? sender, TappedEventArgs e) => _ = SwitchInputMode(1);

        private async Task SwitchInputMode(int newMode)
        {
            if (_vm.InputMode == newMode || _inputAnimBusy) return;
            _inputAnimBusy = true;

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

        // ══════════════════════════════════════════════════════════════════
        // CHIP BUILDERS
        // ══════════════════════════════════════════════════════════════════

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
                    double rounded = _vm.SetSeries(cap, ResistanceEntry.Text);
                    if (rounded > 0) ResistanceEntry.Text = ResistorViewModel.FormatR(rounded);
                    RebuildSeriesChips();
                }));
                SeriesChipsMain.Children.Add(chip);
            }
        }

        // ── Language chip helpers ─────────────────────────────────────────

        /// <summary>
        /// Builds the always-visible primary language chips and resets the
        /// "More / Less" toggle to its collapsed state.
        /// Called when the settings panel is opened or the language changes.
        /// </summary>
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

        /// <summary>
        /// Lazily builds secondary language chips the first time they are needed.
        /// Subsequent calls are a no-op (guard: _secondaryChipsBuilt).
        /// Also pre-loads JSON files on a background thread so that switching
        /// to a secondary language has no perceptible delay.
        /// </summary>
        private void EnsureSecondaryChipsBuilt()
        {
            if (_secondaryChipsBuilt) return;
            _secondaryChipsBuilt = true;

            LangChipsSecondary.Children.Clear();
            foreach (var (code, flag, shortName) in ResistorViewModel.SecondaryLanguages)
                LangChipsSecondary.Children.Add(MakeLangChip(code, flag, shortName));

            // Pre-load JSON files in the background so switching feels instant
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

            // Content: emoji or image (png)
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
                // no emoji: load from Resources/Images/flag_XX.png
                chipContent = new HorizontalStackLayout
                {
                    Spacing = 5,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center,
                    Children =
            {
                new Image
                {
                    Source = ImageSource.FromFile($"flag_{code}.png"),
                    HeightRequest = 14,
                    WidthRequest = 22,
                    Aspect = Aspect.AspectFit,
                    VerticalOptions = LayoutOptions.Center,
                },
                new Label
                {
                    Text = shortName,
                    TextColor = Colors.White,
                    FontSize = 13,
                    FontAttributes = sel ? FontAttributes.Bold : FontAttributes.None,
                    VerticalTextAlignment = TextAlignment.Center,
                    VerticalOptions = LayoutOptions.Center,
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

        /// <summary>Toggles the "More / Less languages" section – with slide-fade animation.</summary>
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
            // Fallback text in case the key is not yet in older JSON files
            string more = string.IsNullOrEmpty(_vm.MoreLangsText) || _vm.MoreLangsText == "moreLangs"
                          ? "More languages" : _vm.MoreLangsText;
            string less = string.IsNullOrEmpty(_vm.LessLangsText) || _vm.LessLangsText == "lessLangs"
                          ? "Less languages" : _vm.LessLangsText;

            MoreLangsLabel.Text = _langExpanded ? $"▲  {less}" : $"▼  {more}";
        }

        // ─────────────────────────────────────────────────────────────────

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

        // ══════════════════════════════════════════════════════════════════
        // TEXT ENTRY  (debounced 80 ms)
        // ══════════════════════════════════════════════════════════════════

        private void OnResistanceTextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_vm.IsValueMode) return;
            _textCts?.Cancel();
            _textCts = new CancellationTokenSource();
            var token = _textCts.Token;
            Task.Delay(80, token).ContinueWith(_ =>
            {
                if (token.IsCancellationRequested) return;
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    _vm.RecalcFromText(ResistanceEntry.Text);
                    if (_vm.ResultText == "OUT_OF_RANGE")
                        DisplayAlert(_vm.WarningTitle, _vm.OutOfRangeText, "OK");
                });
            }, TaskScheduler.Default);
        }

        // ══════════════════════════════════════════════════════════════════
        // SETTINGS PANEL
        // ══════════════════════════════════════════════════════════════════

        private async void OnOpenSettings(object sender, EventArgs e)
        {
            if (_settingsOpen) return;
            _settingsOpen = true;

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

        // ══════════════════════════════════════════════════════════════════
        // RESISTOR DRAWABLE
        // ══════════════════════════════════════════════════════════════════

        public sealed class ResistorDrawable : IDrawable
        {
            private readonly List<Color> _rings;
            private readonly Func<Color> _body;

            public ResistorDrawable(List<Color> rings, Func<Color> body)
            { _rings = rings; _body = body; }

            public void Draw(ICanvas canvas, RectF rect)
            {
                const float Bw = 210f, Bh = 56f, Rw = 13f, Gap = 25f;
                float cx = rect.Width / 2;
                float cy = rect.Height / 2;
                float bx = cx - Bw / 2;
                float by = cy - Bh / 2;

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