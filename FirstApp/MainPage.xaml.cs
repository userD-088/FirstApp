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
    /// View only – no business logic.
    /// Handles: JIT pre-warm, animations, chip building, gesture recognition.
    /// </summary>
    public partial class MainPage : ContentPage
    {
        private readonly ResistorViewModel _vm;

        // ─── Chip lists (built once, selection toggled in-place) ───────────
        private readonly List<Border> _chips1 = new();
        private readonly List<Border> _chips2 = new();
        private readonly List<Border> _chips3 = new();
        private readonly List<Border> _chipsMul = new();
        private readonly List<Border> _chipsTol = new();
        private readonly List<Border> _chipsTcr = new();

        // ─── Gesture state ─────────────────────────────────────────────────
        private bool _resBusy, _inpBusy;
        private double _resDrag, _resLast;
        private double _inpDrag, _inpLast;
        private long _resPanTick, _inpPanTick;      // Throttle-Timestamps
        private const long PanFrameMs = 14;          // ~70fps cap

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

            // Build chip rows – digit + multiplier never change between modes
            BuildChipRow(Band1ChipsLayout, _chips1, ResistorViewModel.DigitData, 0, _vm.Sel1);
            BuildChipRow(Band2ChipsLayout, _chips2, ResistorViewModel.DigitData, 1, _vm.Sel2);
            BuildChipRow(Band3ChipsLayout, _chips3, ResistorViewModel.DigitData, 2, _vm.Sel3);
            BuildChipRow(MultiplierChipsLayout, _chipsMul, ResistorViewModel.MultData, 3, _vm.SelMul, pill: true);

            RebuildToleranceChips();
            RebuildSeriesChips();
            RebuildLangChips();

            ResistorView.Drawable = new ResistorDrawable(_vm.Rings, () => _vm.BodyColor);

            // Trigger initial ring population
            _vm.SetMode(_vm.ResistorMode);
        }

        // ── JIT pre-warm: runs BEFORE the user can touch anything ──────────
        // .NET MAUI JIT-compiles TranslateTo / FadeTo on first call.
        // Running zero-length invisible animations in OnAppearing forces the
        // compiler to do its work now, so real animations are instant later.
        protected override void OnAppearing()
        {
            base.OnAppearing();
            _ = PreWarmJit();
        }

        private async Task PreWarmJit()
        {
            // Wait one frame so the page has rendered before we begin
            await Task.Yield();

            // Animate views that will be used in gesture swipes later.
            // Duration = 1ms, delta = 0 → invisible to the user.
            var t1 = ResistorView.TranslateTo(0, 0, 1);
            var t2 = InputModeTitleContainer.TranslateTo(0, 0, 1);
            var t3 = SettingsPanel.TranslateTo(0, 0, 1);
            var t4 = SettingsOverlay.FadeTo(0, 1);
            await Task.WhenAll(t1, t2, t3, t4);
        }

        // ══════════════════════════════════════════════════════════════════
        // CHIP BUILDERS
        // ══════════════════════════════════════════════════════════════════

        private void BuildChipRow(
            HorizontalStackLayout host,
            List<Border> list,
            (Color Bg, string Lbl, Color Fg)[] data,
            int band,
            int sel,
            bool pill = false)
        {
            host.Children.Clear();
            list.Clear();
            for (int i = 0; i < data.Length; i++)
            {
                int idx = i;
                var chip = MakeChip(data[i].Bg, data[i].Lbl, data[i].Fg, pill, i == sel);
                // FIX: Invalidate() entfernt – RingsChanged-Event im ViewModel macht das bereits
                chip.GestureRecognizers.Add(Tap(() =>
                {
                    SetSel(list, idx);
                    _vm.OnBandSelected(band, idx);
                }));
                list.Add(chip);
                host.Children.Add(chip);
            }
        }

        private void RebuildToleranceChips()
        {
            BuildChipRow(ToleranceChipsLayout, _chipsTol,
                         _vm.CurrentTolData, 4, _vm.SelTol, pill: true);
        }

        private void RebuildTcrChips()
        {
            BuildChipRow(TcrChipsLayout, _chipsTcr,
                         ResistorViewModel.TcrData, 5, _vm.SelTcr, pill: true);
        }

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

        // ─── Chip selection helper ────────────────────────────────────────

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

        // ─── Chip factories ───────────────────────────────────────────────

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
        // SWIPE – RESISTOR TYPE
        // ══════════════════════════════════════════════════════════════════

        private void OnResistorPan(object sender, PanUpdatedEventArgs e)
        {
            if (ResistorView is null) return;
            switch (e.StatusType)
            {
                case GestureStatus.Started:
                    if (_resBusy) return;
                    _resDrag = 0; _resLast = 0; _resPanTick = 0;
                    break;

                case GestureStatus.Running:
                    if (_resBusy) return;
                    _resLast = e.TotalX;
                    _resDrag = Math.Max(_resDrag, Math.Abs(e.TotalX));
                    // FIX: Throttle – TranslationX nur ~70× pro Sekunde setzen
                    long nowR = Environment.TickCount64;
                    if (nowR - _resPanTick < PanFrameMs) return;
                    _resPanTick = nowR;
                    ResistorView.TranslationX = e.TotalX * 0.4;
                    break;

                case GestureStatus.Completed:
                case GestureStatus.Canceled:
                    if (_resBusy) { ResistorView.TranslationX = 0; return; }
                    double w = Math.Max(ResistorView.Width, 200);
                    // FIX: _resLast (Endposition) statt _resDrag (max absolut)
                    // → Zurückwischen ohne Richtungswechsel löst keinen Swipe mehr aus
                    if (Math.Abs(_resLast) >= w * 0.18)
                        _ = AnimateResistorSwipe(_resLast > 0 ? 1 : -1, w);
                    else
                        ResistorView.TranslationX = 0;
                    break;
            }
        }

        private async Task AnimateResistorSwipe(int dir, double w)
        {
            _resBusy = true;
            await ResistorView.TranslateTo(dir * w * 1.1, 0, 130, Easing.CubicIn);

            int newMode = (_vm.ResistorMode + (dir == 1 ? -1 : 1) + 3) % 3;
            _vm.SetMode(newMode);
            RebuildToleranceChips();
            if (newMode == 2) RebuildTcrChips();

            ResistorView.TranslationX = -dir * w * 1.1;
            await ResistorView.TranslateTo(0, 0, 200, Easing.CubicOut);
            _resBusy = false;
        }

        // ══════════════════════════════════════════════════════════════════
        // SWIPE – INPUT MODE
        // ══════════════════════════════════════════════════════════════════

        private void OnInputPan(object sender, PanUpdatedEventArgs e)
        {
            if (InputModeTitleContainer is null) return;
            switch (e.StatusType)
            {
                case GestureStatus.Started:
                    if (_inpBusy) return;
                    _inpDrag = 0; _inpLast = 0; _inpPanTick = 0;
                    break;

                case GestureStatus.Running:
                    if (_inpBusy) return;
                    _inpLast = e.TotalX;
                    _inpDrag = Math.Max(_inpDrag, Math.Abs(e.TotalX));
                    // FIX: Throttle – TranslationX nur ~70× pro Sekunde setzen
                    long nowI = Environment.TickCount64;
                    if (nowI - _inpPanTick < PanFrameMs) return;
                    _inpPanTick = nowI;
                    InputModeTitleContainer.TranslationX = e.TotalX * 0.4;
                    break;

                case GestureStatus.Completed:
                case GestureStatus.Canceled:
                    if (_inpBusy) { InputModeTitleContainer.TranslationX = 0; return; }
                    double w = Math.Max(InputModeTitleContainer.Width, 200);
                    // FIX: _inpLast (Endposition) statt _inpDrag (max absolut)
                    if (Math.Abs(_inpLast) >= w * 0.18)
                        _ = AnimateInputSwipe(_inpLast > 0 ? 1 : -1, w);
                    else
                        InputModeTitleContainer.TranslationX = 0;
                    break;
            }
        }

        private async Task AnimateInputSwipe(int dir, double w)
        {
            _inpBusy = true;
            await InputModeTitleContainer.TranslateTo(dir * w * 1.1, 0, 130, Easing.CubicIn);
            _vm.SetInputMode(_vm.InputMode == 0 ? 1 : 0);
            InputModeTitleContainer.TranslationX = -dir * w * 1.1;
            await InputModeTitleContainer.TranslateTo(0, 0, 200, Easing.CubicOut);
            _inpBusy = false;
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

                // Lead wires
                canvas.StrokeColor = Colors.Silver;
                canvas.StrokeSize = 5;
                canvas.StrokeLineCap = LineCap.Round;
                canvas.DrawLine(bx - 65, cy, bx, cy);
                canvas.DrawLine(bx + Bw, cy, bx + Bw + 65, cy);

                // Drop shadow
                canvas.FillColor = Colors.Black.WithAlpha(0.22f);
                canvas.FillRoundedRectangle(bx, by + 5, Bw, Bh, 28);

                // Body
                canvas.FillColor = _body();
                canvas.FillRoundedRectangle(bx, by, Bw, Bh, 28);

                // Specular highlight
                canvas.FillColor = Colors.White.WithAlpha(0.12f);
                canvas.FillRoundedRectangle(bx + 10, by + 7, Bw - 20, Bh / 3f, 18);

                // Color rings
                // Rings with CNone sentinel are skipped → ±20% shows no ring
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