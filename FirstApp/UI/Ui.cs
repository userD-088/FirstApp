using FirstApp.Core;

namespace FirstApp.UI;

/// <summary>Small factory for the views that are created in code (chips, rows, labels).</summary>
public static class Ui
{
    private static double PillRadius => Math.Min(10, Theme.Current.ChipRadius);
    private static double DotRadius => Theme.Current.ChipRadius <= 4 ? 4 : 18;

    /// <summary>Colour chip for a band option. Round for digits, pill for labels like "×1k".</summary>
    public static Border ColorChip(BandOption opt, bool pill, bool selected, string description)
    {
        var c = opt.Color;
        var chip = new Border
        {
            BackgroundColor = c.IsNone ? Theme.Current.ChipEmpty : c.Bg,
            StrokeShape = Theme.Round(pill ? PillRadius : DotRadius),
            HeightRequest = 36,
            Padding = pill ? new Thickness(12, 0) : new Thickness(0),
            Content = new Label
            {
                Text = opt.Label,
                TextColor = c.IsNone ? Theme.Current.TextSecondary : c.Fg,
                FontSize = pill ? 11 : 13,
                FontAttributes = FontAttributes.Bold,
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
            }
        };
        if (!pill) chip.WidthRequest = 36;
        SemanticProperties.SetDescription(chip, description);
        SetChipSelected(chip, selected);
        return chip;
    }

    public static void SetChipSelected(Border chip, bool selected)
    {
        var t = Theme.Current;
        bool outline = t.ChipOutline.Alpha > 0;
        chip.Stroke = selected ? t.Selection : t.ChipOutline;
        chip.StrokeThickness = selected ? 2.5 : outline ? 1 : 0;
    }

    /// <summary>Accent-coloured text chip (E-series, language).</summary>
    public static Border TextChip(string text, bool selected, double height = 38, View? content = null)
    {
        var chip = new Border
        {
            StrokeShape = Theme.Round(Theme.Current.ChipRadius),
            StrokeThickness = 0,
            Padding = new Thickness(14, 0),
            HeightRequest = height,
            Content = content ?? new Label
            {
                Text = text,
                FontSize = 13,
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
            }
        };
        SetTextChipSelected(chip, selected);
        return chip;
    }

    public static void SetTextChipSelected(Border chip, bool selected)
    {
        var t = Theme.Current;
        chip.Background = selected ? Theme.AccentFill : new SolidColorBrush(t.Chip);
        var color = selected ? t.OnAccent : t.TextPrimary;
        foreach (var l in Labels(chip.Content))
        {
            l.TextColor = color;
            l.FontAttributes = selected ? FontAttributes.Bold : FontAttributes.None;
        }
    }

    /// <summary>Segment of a segmented control.</summary>
    public static void SetSegTab(Border tab, Label lbl, bool active)
    {
        var t = Theme.Current;
        tab.Background = active ? Theme.AccentFill : new SolidColorBrush(Colors.Transparent);
        tab.StrokeShape = Theme.Round(Math.Max(0, t.ControlRadius - 2));
        lbl.TextColor = active ? t.OnAccent : t.TextMuted;
        lbl.FontAttributes = active ? FontAttributes.Bold : FontAttributes.None;
    }

    public static Label Section(string text) => new()
    {
        Text = text,
        FontSize = 13,
        FontAttributes = FontAttributes.Bold,
        TextColor = Theme.Current.TextSecondary,
        Margin = new Thickness(2, 0, 0, 0),
    };

    public static BoxView Divider(double top = 0, double bottom = 0) => new()
    {
        HeightRequest = 1,
        Color = Theme.Current.Divider,
        Margin = new Thickness(0, top, 0, bottom),
    };

    public static T OnTap<T>(this T view, Action action) where T : View
    {
        var t = new TapGestureRecognizer();
        t.Tapped += (_, _) => action();
        view.GestureRecognizers.Add(t);
        return view;
    }

    public static Border Card(View content, Thickness? padding = null) => new()
    {
        BackgroundColor = Theme.Current.Panel,
        Stroke = Theme.Current.Divider,
        StrokeThickness = 1,
        StrokeShape = Theme.Round(Math.Min(16, Theme.Current.CardRadius)),
        Padding = padding ?? new Thickness(16, 12),
        Content = content,
    };

    private static IEnumerable<Label> Labels(IView? v)
    {
        switch (v)
        {
            case Label l: yield return l; break;
            case Layout layout:
                foreach (var child in layout.Children)
                    foreach (var l in Labels(child)) yield return l;
                break;
        }
    }
}
