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
    /// <summary>
    /// View only – kein Business-Logic.
    /// Steuerung ausschließlich per Tap-Buttons (Segmented Controls).
    /// Swipe-Gesten wurden vollständig entfernt.
    /// </summary>
    public partial class MainPage : ContentPage
    {
        private readonly ResistorViewModel _vm;

        // ─── Chip lists ────────────────────────────────────────────────────
        private readonly List<Border> _chips1 = new();
        private readonly List<Border> _chips2 = new();
        private readonly List<Border> _chips3 = new();
        private readonly List<Border> _chipsMul = new();
        private readonly List<Border> _chipsTol = new();
        private readonly List<Border> _chipsTcr = new();

        // ─── Animation guard ──────────────────────────────────────────────
        private bool _inputAnimBusy;

        // ─── Text debounce ─────────────────────────────────────────────────
        private CancellationTokenSource? _textCts;

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
            RebuildLangChips();

            UpdateResistorTypeTabs();
            UpdateInputModeTabs();

            ResistorView.Drawable = new ResistorDrawable(_vm.Rings, () => _vm.BodyColor);

            _vm.SetMode(_vm.ResistorMode);
        }

        // ── Sprach-Änderung ────────────────────────────────────────────────
        private void OnVmPropertyChanged(object? sender,
            System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName is "" or nameof(ResistorViewModel.Lang))
            {
                UpdateResistorTypeTabs();
                UpdateInputModeTabs();
            }
        }

        // ── JIT pre-warm ──────────────────────────────────────────────────
        protected override void OnAppearing()
        {
            base.OnAppearing();
            _ = PreWarmJit();
        }

        private async Task PreWarmJit()
        {
            await Task.Yield();
            // Vorab-JIT für Settings-Panel-Animation
            var t1 = SettingsPanel.TranslateTo(0, 0, 1);
            var t2 = SettingsOverlay.FadeTo(0, 1);
            // Vorab-JIT für Inhalt-Animation
            var t3 = ValueToColorLayout.FadeTo(1, 1);
            var t4 = ColorToValueLayout.FadeTo(1, 1);
            await Task.WhenAll(t1, t2, t3, t4);
        }

        // ══════════════════════════════════════════════════════════════════
        // RESISTOR-TYP TABS  (3-Tab Segmented Control)
        // ══════════════════════════════════════════════════════════════════

        private void OnResTab0Tapped(object? sender, TappedEventArgs e) => SelectResistorMode(0);
        private void OnResTab1Tapped(object? sender, TappedEventArgs e) => SelectResistorMode(1);
        private void OnResTab2Tapped(object? sender, TappedEventArgs e) => SelectResistorMode(2);

        private void SelectResistorMode(int mode)
        {
            if (_vm.ResistorMode == mode) return;
            _vm.SetMode(mode);
            RebuildToleranceChips();
            if (mode == 2) RebuildTcrChips();
            UpdateResistorTypeTabs();
            ResistorView?.Invalidate();
        }

        private void UpdateResistorTypeTabs()
        {
            SetResTab(ResTab0, ResTabLabel0, _vm.ResistorMode == 0);
            SetResTab(ResTab1, ResTabLabel1, _vm.ResistorMode == 1);
            SetResTab(ResTab2, ResTabLabel2, _vm.ResistorMode == 2);
        }

        private static void SetResTab(Border tab, Label lbl, bool active)
        {
            tab.BackgroundColor = active
                ? Color.FromArgb("#2563EB")
                : Color.FromArgb("#1A2535");
            lbl.TextColor = active ? Colors.White : Color.FromArgb("#64748B");
            lbl.FontAttributes = active ? FontAttributes.Bold : FontAttributes.None;
        }

        // ══════════════════════════════════════════════════════════════════
        // INPUT-MODE TABS  (2-Tab Segmented Control + Slide-Animation)
        // ══════════════════════════════════════════════════════════════════

        private void OnInputTab0Tapped(object? sender, TappedEventArgs e)
            => _ = SwitchInputMode(0);

        private void OnInputTab1Tapped(object? sender, TappedEventArgs e)
            => _ = SwitchInputMode(1);

        /// <summary>
        /// Wechselt den Eingabe-Modus mit einer Slide+Fade-Animation.
        /// Richtung: links (mode 0 ← mode 1) / rechts (mode 0 → mode 1).
        /// </summary>
        private async Task SwitchInputMode(int newMode)
        {
            if (_vm.InputMode == newMode || _inputAnimBusy) return;
            _inputAnimBusy = true;

            // Richtung: +1 = nach rechts scrollen (gehe zu niedrigerem Index)
            int dir = newMode > _vm.InputMode ? 1 : -1;

            // Ausgehendes Layout bestimmen
            var outgoing = _vm.IsValueMode ? ValueToColorLayout : (VisualElement)ColorToValueLayout;
            double w = InputContentGrid.Width > 0 ? InputContentGrid.Width : 300;

            // Eingehendes Layout vorbereiten: ausgeblendet, von der Seite
            var incoming = newMode == 0 ? ValueToColorLayout : (VisualElement)ColorToValueLayout;
            incoming.TranslationX = dir * w;
            incoming.Opacity = 0;

            // Modus wechseln (IsVisible bindet auf ViewModel)
            _vm.SetInputMode(newMode);
            UpdateInputModeTabs();

            // Parallel animieren
            uint dur = 220;
            await Task.WhenAll(
                outgoing.TranslateTo(-dir * w * 0.35, 0, dur, Easing.CubicIn),
                outgoing.FadeTo(0, dur, Easing.CubicIn),
                incoming.TranslateTo(0, 0, dur, Easing.CubicOut),
                incoming.FadeTo(1, dur, Easing.CubicOut)
            );

            // Sicherstellen dass outgoing sauber bleibt
            outgoing.TranslationX = 0;
            outgoing.Opacity = 1;

            _inputAnimBusy = false;
        }

        private void UpdateInputModeTabs()
        {
            bool v = _vm.IsValueMode;

            InputTab0.BackgroundColor = v ? Color.FromArgb("#2563EB") : Colors.Transparent;
            if (InputTab0.Content is Label l0)
            {
                l0.TextColor = v ? Colors.White : Color.FromArgb("#64748B");
                l0.FontAttributes = v ? FontAttributes.Bold : FontAttributes.None;
            }

            InputTab1.BackgroundColor = !v ? Color.FromArgb("#2563EB") : Colors.Transparent;
            if (InputTab1.Content is Label l1)
            {
                l1.TextColor = !v ? Colors.White : Color.FromArgb("#64748B");
                l1.FontAttributes = !v ? FontAttributes.Bold : FontAttributes.None;
            }
        }

        // ══════════════════════════════════════════════════════════════════
        // CHIP BUILDERS
        // ══════════════════════════════════════════════════════════════════

        private void BuildChipRow(
            HorizontalStackLayout host,
            List<Border> list,
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
            BuildChipRow(ToleranceChipsLayout, _chipsTol,
                         _vm.CurrentTolData, 4, _vm.SelTol, pill: true);

        private void RebuildTcrChips() =>
            BuildChipRow(TcrChipsLayout, _chipsTcr,
                         ResistorViewModel.TcrData, 5, _vm.SelTcr, pill: true);

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
                    if (rounded > 0)
                        ResistanceEntry.Text = ResistorViewModel.FormatR(rounded);
                    RebuildSeriesChips();
                }));
                SeriesChipsMain.Children.Add(chip);
            }
        }

        private void RebuildLangChips()
        {
            LangChipsLayout.Children.Clear();
            foreach (var (code, flag, shortName) in ResistorViewModel.Languages)
            {
                string cap = code;
                bool sel = code == _vm.Lang;
                var chip = new Border
                {
                    StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(14) },
                    BackgroundColor = sel ? Color.FromArgb("#2563EB") : Color.FromArgb("#1F2937"),
                    Stroke = sel ? Colors.White : Colors.Transparent,
                    StrokeThickness = sel ? 1.5 : 0,
                    Padding = new Thickness(12, 8),
                    HeightRequest = 44,
                    Content = new Label
                    {
                        Text = $"{flag}  {shortName}",
                        TextColor = Colors.White,
                        FontSize = 13,
                        FontAttributes = sel ? FontAttributes.Bold : FontAttributes.None,
                        HorizontalTextAlignment = TextAlignment.Center,
                        VerticalTextAlignment = TextAlignment.Center,
                        HorizontalOptions = LayoutOptions.Center,
                        VerticalOptions = LayoutOptions.Center,
                    }
                };
                chip.GestureRecognizers.Add(Tap(() =>
                {
                    _vm.Lang = cap;
                    RebuildLangChips();
                }));
                LangChipsLayout.Children.Add(chip);
            }
        }

        // ─── Chip-Selektion ───────────────────────────────────────────────

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

        // ─── Chip-Factories ───────────────────────────────────────────────

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
            RebuildLangChips();
            SettingsOverlay.IsVisible = true;
            SettingsOverlay.Opacity = 0;
            SettingsPanel.TranslationY = 700;
            await Task.WhenAll(
                SettingsOverlay.FadeTo(1, 200),
                SettingsPanel.TranslateTo(0, 0, 280, Easing.CubicOut));
        }

        private async void OnCloseSettings(object sender, EventArgs e)
        {
            await Task.WhenAll(
                SettingsOverlay.FadeTo(0, 180),
                SettingsPanel.TranslateTo(0, 700, 230, Easing.CubicIn));
            SettingsOverlay.IsVisible = false;
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