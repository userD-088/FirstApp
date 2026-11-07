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

        // ── Language ────────────────────────────────────────────────
        root.Children.Add(Ui.Section(T("language")));
        root.Children.Add(LanguageGrid(Loc.PrimaryLanguages));

        var moreLbl = new Label
        {
            Text = _moreLangs ? $"▲  {T("lessLangs")}" : $"▼  {T("moreLangs")}",
            TextColor = Theme.TextSub,
            FontSize = 13,
            VerticalTextAlignment = TextAlignment.Center,
        };
        var moreBtn = new Border
        {
            BackgroundColor = t.Chip,
            StrokeShape = Theme.Round(10),
            Stroke = t.AccentBorder,
            StrokeThickness = 1,
            Padding = new Thickness(14, 10),
            HorizontalOptions = LayoutOptions.Start,
            Content = moreLbl,
        }.OnTap(() =>
        {
            _host.Tick();
            _moreLangs = !_moreLangs;
            Rebuild();
        });
        root.Children.Add(moreBtn);

        if (_moreLangs)
        {
            root.Children.Add(LanguageGrid(Loc.SecondaryLanguages));
            _ = Task.Run(() => { foreach (var l in Loc.SecondaryLanguages) Loc.Preload(l.Code); });
        }

        root.Children.Add(Ui.Divider(4));

        // ── Design ──────────────────────────────────────────────────
        root.Children.Add(Ui.Section(T("designLabelText")));
        root.Children.Add(ThemeSelector());

        root.Children.Add(Ui.Divider(4));

        // ── General ─────────────────────────────────────────────────
        root.Children.Add(Ui.Section(T("general")));
        var toggles = new VerticalStackLayout { Spacing = 2 };
        toggles.Children.Add(ToggleRow(T("animations"), T("animationsDesc"), _host.Animations, v => _host.Animations = v));
        toggles.Children.Add(ToggleRow(T("haptics"), T("hapticsDesc"), _host.Haptics, v => _host.Haptics = v));
        toggles.Children.Add(ToggleRow(T("fullscreen"), T("fullscreenDesc"), _host.Fullscreen, v => _host.Fullscreen = v));
        toggles.Children.Add(ToggleRow(T("showRange"), T("showRangeDesc"), _host.ShowRange, v => _host.ShowRange = v));
        toggles.Children.Add(ToggleRow(T("rememberState"), T("rememberStateDesc"), _host.RememberState, v => _host.RememberState = v));
        root.Children.Add(Ui.Card(toggles, new Thickness(14, 6)));

        var reset = new Border
        {
            BackgroundColor = Colors.Transparent,
            Stroke = Theme.Error.WithAlpha(0.6f),
            StrokeThickness = 1,
            StrokeShape = Theme.Round(t.ControlRadius),
            Padding = new Thickness(14, 12),
            Content = new Label
            {
                Text = $"↺  {T("resetSettings")}",
                TextColor = Theme.Error,
                FontSize = 14,
                HorizontalTextAlignment = TextAlignment.Center,
            },
        }.OnTap(() => _ = _host.ResetSettings());
        root.Children.Add(reset);

        root.Children.Add(Ui.Divider(4));

        // ── About ───────────────────────────────────────────────────
        root.Children.Add(AboutCard());

        // ── Close ───────────────────────────────────────────────────
        root.Children.Add(new Border
        {
            BackgroundColor = t.Accent,
            StrokeThickness = 0,
            StrokeShape = Theme.Round(t.HasCyberFx ? 6 : 16),
            HeightRequest = 50,
            Content = new Label
            {
                Text = T("close"),
                FontSize = 16,
                FontAttributes = FontAttributes.Bold,
                TextColor = t.HasCyberFx ? Colors.Black : Colors.White,
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center,
            },
        }.OnTap(_host.CloseSheetFromChild));

        Content = root;
    }

    // ──────────────────────────────────────────────────────────────
    private View LanguageGrid((string Code, string Flag, string Short)[] langs)
    {
        var flex = new FlexLayout
        {
            Wrap = Microsoft.Maui.Layouts.FlexWrap.Wrap,
            Direction = Microsoft.Maui.Layouts.FlexDirection.Row,
            JustifyContent = Microsoft.Maui.Layouts.FlexJustify.Start,
        };
        foreach (var (code, flag, shortName) in langs)
        {
            bool sel = code == _vm.Lang;
            View content;
            if (!string.IsNullOrEmpty(flag))
            {
                content = new Label
                {
                    Text = $"{flag}  {shortName}",
                    TextColor = Colors.White,
                    FontSize = 13,
                    FontAttributes = sel ? FontAttributes.Bold : FontAttributes.None,
                    VerticalTextAlignment = TextAlignment.Center,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center,
                };
            }
            else
            {
                content = new HorizontalStackLayout
                {
                    Spacing = 5,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center,
                    Children =
                    {
                        new Image { Source = $"flag_{code}.png", HeightRequest = 14, WidthRequest = 21, VerticalOptions = LayoutOptions.Center },
                        new Label { Text = shortName, TextColor = Colors.White, FontSize = 13,
                                    FontAttributes = sel ? FontAttributes.Bold : FontAttributes.None,
                                    VerticalOptions = LayoutOptions.Center },
                    },
                };
            }
            var chip = Ui.TextChip("", sel, 40, content);
            chip.Padding = new Thickness(12, 0);
            chip.Margin = new Thickness(0, 0, 8, 8);
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

    private View ThemeSelector()
    {
        var t = Theme.Current;
        var grid = new Grid { ColumnSpacing = 0 };
        for (int i = 0; i < Theme.All.Length; i++)
        {
            grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            var profile = Theme.All[i];
            string name = T(profile.Key);
            if (name == profile.Key) name = profile.Fallback;

            var dot = new BoxView { Color = profile.Accent, WidthRequest = 10, HeightRequest = 10, CornerRadius = 5, VerticalOptions = LayoutOptions.Center };
            var lbl = new Label { Text = name, FontSize = 12, MaxLines = 1, LineBreakMode = LineBreakMode.TailTruncation, VerticalOptions = LayoutOptions.Center };
            var tab = new Border
            {
                StrokeThickness = 0,
                Padding = new Thickness(4, 12),
                Content = new HorizontalStackLayout { Spacing = 6, HorizontalOptions = LayoutOptions.Center, Children = { dot, lbl } },
            };
            Ui.SetSegTab(tab, lbl, i == Theme.Index);
            int idx = i;
            tab.OnTap(() => { _host.Tick(); _host.SelectTheme(idx); });
            Grid.SetColumn(tab, i);
            grid.Children.Add(tab);
        }
        return new Border
        {
            BackgroundColor = Theme.PanelDark,
            Stroke = t.AccentBorder,
            StrokeThickness = 1,
            StrokeShape = Theme.Round(t.ControlRadius),
            Padding = 0,
            Content = grid,
        };
    }

    private View ToggleRow(string title, string desc, bool value, Action<bool> onChange)
    {
        var sw = new Switch
        {
            IsToggled = value,
            OnColor = Theme.Current.Accent,
            ThumbColor = Colors.White,
            VerticalOptions = LayoutOptions.Center,
        };
        sw.Toggled += (_, e) => onChange(e.Value);

        var text = new VerticalStackLayout
        {
            Spacing = 1,
            VerticalOptions = LayoutOptions.Center,
            Children =
            {
                new Label { Text = title, FontSize = 14, TextColor = Theme.TextLight },
                new Label { Text = desc, FontSize = 11.5, TextColor = Theme.TextMuted },
            },
        };
        var row = new Grid
        {
            ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) },
            ColumnSpacing = 10,
            Padding = new Thickness(0, 8),
        };
        row.Add(text, 0);
        row.Add(sw, 1);
        row.OnTap(() => sw.IsToggled = !sw.IsToggled);
        return row;
    }

    private View AboutCard()
    {
        var chevron = new Label { Text = _aboutExpanded ? "⌄" : "›", FontSize = 18, TextColor = Theme.TextDim, VerticalTextAlignment = TextAlignment.Center, HorizontalTextAlignment = TextAlignment.End };
        var header = new Grid
        {
            ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(20) },
        };
        header.Add(new VerticalStackLayout
        {
            Spacing = 3,
            Children =
            {
                new Label { Text = T("about"), FontSize = 14, FontAttributes = FontAttributes.Bold, TextColor = Theme.TextLight },
                new Label { Text = _vm.AppVersionText, FontSize = 13, TextColor = Theme.TextSub },
                new Label { Text = T("aboutDesc"), FontSize = 12, TextColor = Theme.TextMuted },
            },
        }, 0);
        header.Add(chevron, 1);

        var stack = new VerticalStackLayout { Spacing = 8, Children = { header } };
        if (_aboutExpanded)
        {
            stack.Children.Add(Ui.Divider(6, 2));
            stack.Children.Add(LinkRow("✉️", T("support"), OpenSupportMail));
            stack.Children.Add(LinkRow("🔒", T("privacyPolicy"), ShowUnavailable));
            stack.Children.Add(LinkRow("🎁", T("aboutDev"), ShowUnavailable));
        }
        stack.Children.Add(Ui.Divider(6, 2));
        stack.Children.Add(new Label { Text = T("copyright"), FontSize = 11, TextColor = Theme.TextDim });

        var card = Ui.Card(stack, new Thickness(16, 14));
        card.OnTap(() => { _aboutExpanded = !_aboutExpanded; Rebuild(); });
        return card;
    }

    private View LinkRow(string icon, string text, Action action)
    {
        var grid = new Grid { ColumnDefinitions = { new ColumnDefinition(24), new ColumnDefinition(GridLength.Star) } };
        grid.Add(new Label { Text = icon, FontSize = 14, VerticalTextAlignment = TextAlignment.Center }, 0);
        grid.Add(new Label { Text = text, FontSize = 13, TextColor = Theme.TextLight, VerticalTextAlignment = TextAlignment.Center, Margin = new Thickness(6, 0, 0, 0) }, 1);
        return new Border
        {
            BackgroundColor = Theme.PanelDark,
            StrokeShape = Theme.Round(12),
            Stroke = Theme.Current.AccentBorder,
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
