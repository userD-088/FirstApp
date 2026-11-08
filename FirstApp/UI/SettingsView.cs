using FirstApp.Core;

namespace FirstApp.UI;

/// <summary>
/// Settings sheet content. Built in code on first open (keeps app start lean) and
/// rebuilt when language or theme change.
/// </summary>
public sealed class SettingsView : ContentView
{
    private readonly MainPage _host;
    private readonly ResistorViewModel _vm;
    private bool _moreLangs;
    private bool _aboutExpanded;

    private const string SupportEmail = "neutron.mail216@proton.me";

    public SettingsView(MainPage host, ResistorViewModel vm)
    {
        _host = host;
        _vm = vm;
    }

    private string T(string key) => _vm[key];

    public void Rebuild()
    {
        var t = Theme.Current;
        var root = new VerticalStackLayout { Spacing = 14, Padding = new Thickness(0, 0, 0, 8) };

        // ── Design ──────────────────────────────────────────────────
        root.Children.Add(Ui.Section(T("designLabelText")));
        root.Children.Add(ThemePicker());

        root.Children.Add(Ui.Divider(4));

        // ── Language ────────────────────────────────────────────────
        root.Children.Add(Ui.Section(T("language")));
        root.Children.Add(LanguageGrid(Loc.PrimaryLanguages));

        bool currentIsSecondary = Array.Exists(Loc.SecondaryLanguages, l => l.Code == _vm.Lang);
        bool showMore = _moreLangs || currentIsSecondary;
        var moreBtn = new Border
        {
            BackgroundColor = t.Surface,
            StrokeShape = Theme.Round(t.ControlRadius),
            Stroke = t.AccentBorder,
            StrokeThickness = 1,
            Padding = new Thickness(14, 10),
            HorizontalOptions = LayoutOptions.Start,
            Content = new Label
            {
                Text = showMore ? $"▲  {T("lessLangs")}" : $"▼  {T("moreLangs")}  ({Loc.SecondaryLanguages.Length})",
                TextColor = t.TextSecondary,
                FontSize = 13,
            },
        }.OnTap(() =>
        {
            _host.Tick();
            _moreLangs = !showMore;
            Rebuild();
        });
        root.Children.Add(moreBtn);

        if (showMore)
        {
            root.Children.Add(LanguageGrid(Loc.SecondaryLanguages));
            _ = Task.Run(() => { foreach (var l in Loc.SecondaryLanguages) Loc.Preload(l.Code); });
        }

        root.Children.Add(Ui.Divider(4));

        // ── General ─────────────────────────────────────────────────
        root.Children.Add(Ui.Section(T("general")));
        var toggles = new VerticalStackLayout { Spacing = 0 };
        toggles.Children.Add(ToggleRow("✨", T("animations"), T("animationsDesc"), _host.Animations, v => _host.Animations = v));
        toggles.Children.Add(Ui.Divider());
        toggles.Children.Add(ToggleRow("📳", T("haptics"), T("hapticsDesc"), _host.Haptics, v => _host.Haptics = v));
        toggles.Children.Add(Ui.Divider());
        toggles.Children.Add(ToggleRow("⛶", T("fullscreen"), T("fullscreenDesc"), _host.Fullscreen, v => _host.Fullscreen = v));
        toggles.Children.Add(Ui.Divider());
        toggles.Children.Add(ToggleRow("↔", T("showRange"), T("showRangeDesc"), _host.ShowRange, v => _host.ShowRange = v));
        toggles.Children.Add(Ui.Divider());
        toggles.Children.Add(ToggleRow("💾", T("rememberState"), T("rememberStateDesc"), _host.RememberState, v => _host.RememberState = v));
        root.Children.Add(Ui.Card(toggles, new Thickness(14, 2)));

        root.Children.Add(new Border
        {
            BackgroundColor = Colors.Transparent,
            Stroke = t.Error.WithAlpha(0.55f),
            StrokeThickness = 1,
            StrokeShape = Theme.Round(t.ControlRadius),
            Padding = new Thickness(14, 12),
            Content = new Label
            {
                Text = $"↺  {T("resetSettings")}",
                TextColor = t.Error,
                FontSize = 14,
                HorizontalTextAlignment = TextAlignment.Center,
            },
        }.OnTap(() => _ = _host.ResetSettings()));

        root.Children.Add(Ui.Divider(4));

        // ── About ───────────────────────────────────────────────────
        root.Children.Add(AboutCard());

        // ── Close ───────────────────────────────────────────────────
        root.Children.Add(new Border
        {
            Background = Theme.AccentFill,
            StrokeThickness = 0,
            StrokeShape = Theme.Round(Math.Min(16, t.CardRadius)),
            HeightRequest = 50,
            Content = new Label
            {
                Text = T("close"),
                FontSize = 16,
                FontAttributes = FontAttributes.Bold,
                TextColor = t.OnAccent,
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center,
            },
        }.OnTap(_host.CloseSheetFromChild));

        Content = root;
    }

    // ──────────────────────────────────────────────────────────────
    // DESIGN PICKER: live mini previews of every theme
    // ──────────────────────────────────────────────────────────────
    private View ThemePicker()
    {
        const int cols = 3;
        var grid = new Grid { ColumnSpacing = 10, RowSpacing = 12 };
        for (int c = 0; c < cols; c++) grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        int rows = (Theme.DisplayOrder.Length + cols - 1) / cols;
        for (int r = 0; r < rows; r++) grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));

        for (int i = 0; i < Theme.DisplayOrder.Length; i++)
        {
            int idx = Theme.DisplayOrder[i];
            var tile = ThemeTile(Theme.All[idx], idx == Theme.Index);
            tile.OnTap(() => { _host.Tick(); _host.SelectTheme(idx); });
            grid.Add(tile, i % cols, i / cols);
        }
        return grid;
    }

    private View ThemeTile(ThemeProfile p, bool selected)
    {
        var cur = Theme.Current;
        string name = T(p.Key);
        if (name == p.Key) name = p.Fallback;

        // Mini app: page background → card → accent tab, text lines, resistor bands
        var bands = new HorizontalStackLayout { Spacing = 3, HorizontalOptions = LayoutOptions.Center };
        foreach (var bc in new[] { ColorCodes.Yellow, ColorCodes.Violet, ColorCodes.Red, ColorCodes.Gold })
            bands.Children.Add(new BoxView { Color = bc.Bg, WidthRequest = 5, HeightRequest = 14 });
        var resistor = new Border
        {
            BackgroundColor = Color.FromArgb("#6FA8C7"),
            StrokeThickness = 0,
            StrokeShape = Theme.Round(7),
            HeightRequest = 14, WidthRequest = 46,
            HorizontalOptions = LayoutOptions.Center,
            Content = bands,
        };

        var miniCard = new Border
        {
            Background = Theme.Gradient(p.CardBg, diagonal: true),
            Stroke = p.CardStroke ?? new SolidColorBrush(p.Accent),
            StrokeThickness = p.CardStrokeThickness > 0 ? 1 : 0,
            StrokeShape = Theme.Round(Math.Min(10, p.CardRadius / 2.5)),
            Padding = new Thickness(7, 6),
            Margin = new Thickness(8, 8, 8, 0),
            Content = new VerticalStackLayout
            {
                Spacing = 5,
                Children =
                {
                    new Border
                    {
                        Background = p.AccentGradient ?? new SolidColorBrush(p.Accent),
                        StrokeThickness = 0, StrokeShape = Theme.Round(Math.Min(4, p.ControlRadius)),
                        HeightRequest = 7, WidthRequest = 30, HorizontalOptions = LayoutOptions.Start,
                    },
                    resistor,
                    new BoxView { Color = p.Result, HeightRequest = 4, WidthRequest = 34, CornerRadius = 2, HorizontalOptions = LayoutOptions.Center },
                    new BoxView { Color = p.TextMuted, HeightRequest = 3, WidthRequest = 24, CornerRadius = 1.5, HorizontalOptions = LayoutOptions.Center },
                },
            },
        };

        var preview = new Border
        {
            Background = Theme.Gradient(p.PageBg, diagonal: true),
            Stroke = selected ? cur.Accent : cur.Divider,
            StrokeThickness = selected ? 2.5 : 1,
            StrokeShape = Theme.Round(14),
            HeightRequest = 92,
            Padding = 0,
            Content = miniCard,
        };

        var label = new Label
        {
            Text = selected ? $"✓ {name}" : name,
            FontSize = 12,
            FontFamily = p.FontFamily ?? "",
            FontAttributes = selected ? FontAttributes.Bold : FontAttributes.None,
            TextColor = selected ? cur.TextPrimary : cur.TextSecondary,
            HorizontalTextAlignment = TextAlignment.Center,
            MaxLines = 1,
            LineBreakMode = LineBreakMode.TailTruncation,
        };

        var stack = new VerticalStackLayout { Spacing = 6, Children = { preview, label } };
        SemanticProperties.SetDescription(stack, name);
        return stack;
    }

    // ──────────────────────────────────────────────────────────────
    // LANGUAGES: flag + native name, current one highlighted
    // ──────────────────────────────────────────────────────────────
    private View LanguageGrid((string Code, string Flag, string Name)[] langs)
    {
        var flex = new FlexLayout
        {
            Wrap = Microsoft.Maui.Layouts.FlexWrap.Wrap,
            Direction = Microsoft.Maui.Layouts.FlexDirection.Row,
            JustifyContent = Microsoft.Maui.Layouts.FlexJustify.Start,
        };
        foreach (var (code, flag, name) in langs)
        {
            bool sel = code == _vm.Lang;
            var nameLabel = new Label { Text = name, FontSize = 13, VerticalOptions = LayoutOptions.Center };
            View flagView = string.IsNullOrEmpty(flag)
                ? new Image { Source = $"flag_{code}.png", HeightRequest = 13, WidthRequest = 19, VerticalOptions = LayoutOptions.Center }
                : new Label { Text = flag, FontSize = 15, FontFamily = "", VerticalOptions = LayoutOptions.Center };
            var content = new HorizontalStackLayout
            {
                Spacing = 7,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
                Children = { flagView, nameLabel },
            };

            var chip = Ui.TextChip("", sel, 40, content);
            chip.Padding = new Thickness(12, 0);
            chip.Margin = new Thickness(0, 0, 8, 8);
            // The flag emoji keeps its own colours
            if (flagView is Label fl) fl.TextColor = Colors.White;
            SemanticProperties.SetDescription(chip, name);

            string c = code;
            chip.OnTap(() =>
            {
                if (c == _vm.Lang) return;
                _host.Tick();
                _host.SelectLanguage(c);
            });
            flex.Children.Add(chip);
        }
        return flex;
    }

    // ──────────────────────────────────────────────────────────────
    private View ToggleRow(string icon, string title, string desc, bool value, Action<bool> onChange)
    {
        var t = Theme.Current;
        var sw = new Switch
        {
            IsToggled = value,
            OnColor = t.Accent,
            ThumbColor = Colors.White,
            VerticalOptions = LayoutOptions.Center,
        };
        sw.Toggled += (_, e) => onChange(e.Value);

        var row = new Grid
        {
            ColumnDefinitions = { new ColumnDefinition(28), new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) },
            ColumnSpacing = 8,
            Padding = new Thickness(0, 10),
        };
        row.Add(new Label { Text = icon, FontSize = 16, FontFamily = "", TextColor = t.TextSecondary, VerticalOptions = LayoutOptions.Center, HorizontalTextAlignment = TextAlignment.Center }, 0);
        row.Add(new VerticalStackLayout
        {
            Spacing = 1,
            VerticalOptions = LayoutOptions.Center,
            Children =
            {
                new Label { Text = title, FontSize = 14 },
                new Label { Text = desc, FontSize = 11.5, TextColor = t.TextMuted },
            },
        }, 1);
        row.Add(sw, 2);
        row.OnTap(() => sw.IsToggled = !sw.IsToggled);
        return row;
    }

    private View AboutCard()
    {
        var t = Theme.Current;
        var header = new Grid { ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(20) } };
        header.Add(new VerticalStackLayout
        {
            Spacing = 3,
            Children =
            {
                new Label { Text = T("about"), FontSize = 14, FontAttributes = FontAttributes.Bold },
                new Label { Text = _vm.AppVersionText, FontSize = 13, TextColor = t.TextSecondary },
                new Label { Text = T("aboutDesc"), FontSize = 12, TextColor = t.TextMuted },
            },
        }, 0);
        header.Add(new Label
        {
            Text = _aboutExpanded ? "⌄" : "›", FontSize = 18, TextColor = t.TextDim,
            VerticalTextAlignment = TextAlignment.Center, HorizontalTextAlignment = TextAlignment.End,
        }, 1);

        var stack = new VerticalStackLayout { Spacing = 8, Children = { header } };
        if (_aboutExpanded)
        {
            stack.Children.Add(Ui.Divider(6, 2));
            stack.Children.Add(LinkRow("✉️", T("support"), OpenSupportMail));
            stack.Children.Add(LinkRow("🔒", T("privacyPolicy"), ShowUnavailable));
            stack.Children.Add(LinkRow("🎁", T("aboutDev"), ShowUnavailable));
        }
        stack.Children.Add(Ui.Divider(6, 2));
        stack.Children.Add(new Label { Text = T("copyright"), FontSize = 11, TextColor = t.TextDim });

        var card = Ui.Card(stack, new Thickness(16, 14));
        card.OnTap(() => { _aboutExpanded = !_aboutExpanded; Rebuild(); });
        return card;
    }

    private View LinkRow(string icon, string text, Action action)
    {
        var t = Theme.Current;
        var grid = new Grid { ColumnDefinitions = { new ColumnDefinition(24), new ColumnDefinition(GridLength.Star) } };
        grid.Add(new Label { Text = icon, FontSize = 14, FontFamily = "", VerticalTextAlignment = TextAlignment.Center }, 0);
        grid.Add(new Label { Text = text, FontSize = 13, VerticalTextAlignment = TextAlignment.Center, Margin = new Thickness(6, 0, 0, 0) }, 1);
        return new Border
        {
            BackgroundColor = t.Surface,
            StrokeShape = Theme.Round(Math.Min(12, t.ControlRadius)),
            Stroke = t.AccentBorder,
            StrokeThickness = 1,
            Padding = new Thickness(12, 10),
            Content = grid,
        }.OnTap(action);
    }

    private async void OpenSupportMail()
    {
        try
        {
            await Launcher.Default.OpenAsync($"mailto:{SupportEmail}?subject=Resistor%20Calculator%20Feedback");
        }
        catch
        {
            await _host.DisplayAlert(T("support"), SupportEmail, "OK");
        }
    }

    private async void ShowUnavailable()
        => await _host.DisplayAlert(T("warning"), T("unavailable"), "OK");
}
