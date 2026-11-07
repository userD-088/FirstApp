using FirstApp.Core;
using Microsoft.Maui.Controls.Shapes;

namespace FirstApp.UI;

/// <summary>Small factory for the views that are created in code (chips, rows, labels).</summary>
public static class Ui
{
    /// <summary>Colour chip for a band option. Round for digits, pill for labels like "×1k".</summary>
    public static Border ColorChip(BandOption opt, bool pill, bool selected, string description)
    {
        var c = opt.Color;
        var chip = new Border
        {
            BackgroundColor = c.IsNone ? Theme.ChipEmpty : c.Bg,
            StrokeShape = Theme.Round(pill ? 10 : 18),
            HeightRequest = 36,
            Padding = pill ? new Thickness(12, 0) : new Thickness(0),
            Content = new Label
            {
                Text = opt.Label,
                TextColor = c.Fg,
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
        chip.Stroke = selected ? Colors.White : Colors.Transparent;
        chip.StrokeThickness = selected ? 2.5 : 0;
    }

    /// <summary>Accent-coloured text chip (E-series, language, units).</summary>
    public static Border TextChip(string text, bool selected, double height = 38, View? content = null)
    {
        var chip = new Border
        {
            StrokeShape = Theme.Round(12),
            Padding = new Thickness(14, 0),
            HeightRequest = height,
            Content = content ?? new Label
            {
                Text = text,
                TextColor = Colors.White,
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
        chip.BackgroundColor = selected ? t.Accent : t.Chip;
        chip.Stroke = selected ? Colors.White : Colors.Transparent;
        chip.StrokeThickness = selected ? 1.5 : 0;
        if (chip.Content is Label l) l.FontAttributes = selected ? FontAttributes.Bold : FontAttributes.None;
    }

    /// <summary>Segment of a segmented control.</summary>
    public static void SetSegTab(Border tab, Label lbl, bool active)
    {
        tab.BackgroundColor = active ? Theme.Current.Accent : Colors.Transparent;
        lbl.TextColor = active ? (Theme.Current.HasCyberFx ? Colors.Black : Colors.White) : Theme.TextMuted;
        lbl.FontAttributes = active ? FontAttributes.Bold : FontAttributes.None;
    }

    public static Label Section(string text) => new()
    {
        Text = text,
        FontSize = 13,
        FontAttributes = FontAttributes.Bold,
        TextColor = Theme.TextSub,
        Margin = new Thickness(2, 0, 0, 0),
    };

    public static BoxView Divider(double top = 0, double bottom = 0) => new()
    {
        HeightRequest = 1,
        Color = Theme.Divider,
        Margin = new Thickness(0, top, 0, bottom),
    };

    public static TapGestureRecognizer Tap(Action action)
    {
        var t = new TapGestureRecognizer();
        t.Tapped += (_, _) => action();
        return t;
    }

    public static T OnTap<T>(this T view, Action action) where T : View
    {
        view.GestureRecognizers.Add(Tap(action));
        return view;
    }

    public static Border Card(View content, Thickness? padding = null) => new()
    {
        BackgroundColor = Theme.Current.Chip.WithAlpha(0.55f),
        Stroke = Theme.Divider,
        StrokeThickness = 1,
        StrokeShape = Theme.Round(16),
        Padding = padding ?? new Thickness(16, 12),
        Content = content,
    };
}
