using FirstApp.Core;

namespace FirstApp.UI;

/// <summary>Reference sheet: reading direction, colour table, E-series overview. Built on demand.</summary>
public static class ReferenceView
{
    public static View Build(ResistorViewModel vm)
    {
        var root = new VerticalStackLayout { Spacing = 16, Padding = new Thickness(0, 0, 0, 12) };
        root.Children.Add(Header(vm["refTip"]));
        root.Children.Add(ReadingTip(vm));
        root.Children.Add(Ui.Divider());
        root.Children.Add(Header(vm["refColors"]));
        root.Children.Add(ColorTable(vm));
        root.Children.Add(Ui.Divider());
        root.Children.Add(Header(vm["refESeries"]));
        root.Children.Add(ESeries(vm));
        return root;
    }

    private static Label Header(string text) => new()
    {
        Text = text,
        FontSize = 14,
        FontAttributes = FontAttributes.Bold,
        TextColor = Theme.TextSub,
    };

    private static Border Panel(View content, Thickness padding) => new()
    {
        BackgroundColor = Theme.PanelDark,
        Stroke = Theme.Current.AccentBorder,
        StrokeThickness = 1,
        StrokeShape = Theme.Round(14),
        Padding = padding,
        Content = content,
    };

    private static View ReadingTip(ResistorViewModel vm) => Panel(new VerticalStackLayout
    {
        Spacing = 7,
        Children =
        {
            new Label { Text = vm["tip4band"], FontSize = 12, TextColor = Theme.TextTan },
            new Label { Text = vm["tip5band"], FontSize = 12, TextColor = Theme.TextSteel },
            new Label { Text = vm["tip6band"], FontSize = 12, TextColor = Theme.TextSteel },
            Ui.Divider(2, 2),
            new Label { Text = vm["tipDir"], FontSize = 11.5, TextColor = Theme.TextYellow, FontAttributes = FontAttributes.Bold },
        },
    }, new Thickness(14, 12));

    private static View ColorTable(ResistorViewModel vm)
    {
        var rows = new (BandColor C, string Digit, string Mult, string Tol, string Tcr)[]
        {
            (ColorCodes.Black,  "0", "×1",    "–",      "250"),
            (ColorCodes.Brown,  "1", "×10",   "±1%",    "100"),
            (ColorCodes.Red,    "2", "×100",  "±2%",    "50"),
            (ColorCodes.Orange, "3", "×1k",   "–",      "15"),
            (ColorCodes.Yellow, "4", "×10k",  "–",      "25"),
            (ColorCodes.Green,  "5", "×100k", "±0.5%",  "20"),
            (ColorCodes.Blue,   "6", "×1M",   "±0.25%", "10"),
            (ColorCodes.Violet, "7", "×10M",  "±0.1%",  "5"),
            (ColorCodes.Gray,   "8", "×100M", "±0.05%", "1"),
            (ColorCodes.White,  "9", "×1G",   "–",      "–"),
            (ColorCodes.Gold,   "–", "×0.1",  "±5%",    "–"),
            (ColorCodes.Silver, "–", "×0.01", "±10%",   "–"),
            (ColorCodes.None,   "–", "–",     "±20%",   "–"),
        };

        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(26), new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(32), new ColumnDefinition(52),
                new ColumnDefinition(50), new ColumnDefinition(34),
            },
            ColumnSpacing = 5,
        };
        grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));

        string[] hdrs = { "", vm["refColor"], vm["refDigit"], vm["refMult"], vm["refTol"], "TCR" };
        for (int c = 0; c < hdrs.Length; c++)
            grid.Add(new Label
            {
                Text = hdrs[c], FontSize = 10, TextColor = Theme.TextMuted, FontAttributes = FontAttributes.Bold,
                HorizontalTextAlignment = TextAlignment.Center, MaxLines = 1, LineBreakMode = LineBreakMode.TailTruncation,
                Margin = new Thickness(0, 0, 0, 4),
            }, c, 0);

        for (int r = 0; r < rows.Length; r++)
        {
            var row = rows[r];
            int ri = r + 1;
            grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));

            var bg = new BoxView { Color = r % 2 == 0 ? Theme.RowDark1 : Theme.RowDark2, CornerRadius = 6, Margin = new Thickness(-2, 1) };
            grid.Add(bg, 0, ri);
            Grid.SetColumnSpan(bg, 6);

            grid.Add(new Border
            {
                WidthRequest = 20, HeightRequest = 20,
                BackgroundColor = row.C.IsNone ? Theme.ChipEmpty : row.C.Bg,
                StrokeShape = Theme.Round(10),
                Stroke = row.C.IsNone ? Theme.BorderNone : Colors.Transparent,
                StrokeThickness = row.C.IsNone ? 1 : 0,
                HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center,
                Margin = new Thickness(0, 3),
            }, 0, ri);

            grid.Add(Cell(vm[row.C.Key], 12.5, Colors.White, false, TextAlignment.Start), 1, ri);
            grid.Add(Cell(row.Digit, 13, Theme.TextLight, true), 2, ri);
            grid.Add(Cell(row.Mult, 11.5, Theme.TextGreen, true), 3, ri);
            grid.Add(Cell(row.Tol, 11, row.Tol == "–" ? Theme.TextDimmer : Theme.TextYellow, false), 4, ri);
            grid.Add(Cell(row.Tcr, 11, row.Tcr == "–" ? Theme.TextDimmer : Theme.TextBlue, false), 5, ri);
        }

        return Panel(grid, new Thickness(10, 8));
    }

    private static Label Cell(string text, double size, Color color, bool bold, TextAlignment align = TextAlignment.Center) => new()
    {
        Text = text,
        FontSize = size,
        TextColor = color,
        FontAttributes = bold ? FontAttributes.Bold : FontAttributes.None,
        HorizontalTextAlignment = align,
        VerticalTextAlignment = TextAlignment.Center,
        Margin = new Thickness(2, 3),
        MaxLines = 1,
    };

    private static View ESeries(ResistorViewModel vm)
    {
        var info = new (int N, string Use)[]
        {
            (3, vm["eUseRough"]), (6, vm["eUseGeneral"]), (12, vm["eUseStd"]), (24, vm["eUseCommon"]),
            (48, vm["eUsePrecise"]), (96, vm["eUseHigh"]), (192, vm["eUseUltra"]),
        };

        var stack = new VerticalStackLayout { Spacing = 6 };
        stack.Children.Add(new Label { Text = vm["eSeriesHint"], FontSize = 11, TextColor = Theme.TextMuted, Margin = new Thickness(0, 0, 0, 4) });

        var t = Theme.Current;
        foreach (var (n, use) in info)
        {
            bool hl = n == vm.Series;
            var vals = ResistorMath.SeriesValues(n);
            string fmt = n <= 24 ? "0.0" : "0.00";
            int show = Math.Min(vals.Length, n <= 12 ? vals.Length : 6);
            var parts = new string[show];
            for (int i = 0; i < show; i++) parts[i] = vals[i].ToString(fmt, System.Globalization.CultureInfo.InvariantCulture);
            string examples = string.Join(" · ", parts) + (show < vals.Length ? " …" : "");

            var inner = new Grid
            {
                ColumnDefinitions = { new ColumnDefinition(44), new ColumnDefinition(GridLength.Star) },
                RowDefinitions = { new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Auto) },
                ColumnSpacing = 10,
            };
            var badge = new Border
            {
                StrokeShape = Theme.Round(8),
                BackgroundColor = hl ? t.Accent : t.ChipAlt,
                StrokeThickness = 0,
                WidthRequest = 44, HeightRequest = 26,
                VerticalOptions = LayoutOptions.Center,
                Content = new Label
                {
                    Text = $"E{n}", FontSize = 11.5, FontAttributes = FontAttributes.Bold,
                    TextColor = hl && t.HasCyberFx ? Colors.Black : Colors.White,
                    HorizontalTextAlignment = TextAlignment.Center, VerticalTextAlignment = TextAlignment.Center,
                },
            };
            inner.Add(badge, 0, 0);
            Grid.SetRowSpan(badge, 2);
            inner.Add(new Label { Text = $"{use}  ·  {n} {vm["perDecade"]}", FontSize = 12, TextColor = hl ? Theme.TextBlue : Theme.TextSub }, 1, 0);
            inner.Add(new Label { Text = examples, FontSize = 10.5, TextColor = Theme.TextDim, LineBreakMode = LineBreakMode.WordWrap }, 1, 1);

            stack.Children.Add(new Border
            {
                StrokeShape = Theme.Round(12),
                BackgroundColor = hl ? t.HighBg : Theme.PanelDark,
                Stroke = hl ? t.Accent : t.AccentBorder,
                StrokeThickness = hl ? 1.5 : 1,
                Padding = new Thickness(12, 9),
                Content = inner,
            });
        }
        return stack;
    }
}
