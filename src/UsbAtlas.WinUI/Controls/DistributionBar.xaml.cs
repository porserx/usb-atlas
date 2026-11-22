using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using UsbAtlas.WinUI.ViewModels;

namespace UsbAtlas.WinUI.Controls;

public sealed partial class DistributionBar : UserControl
{
    public static readonly DependencyProperty SegmentsProperty = DependencyProperty.Register(
        nameof(Segments), typeof(IReadOnlyList<VersionSegmentItem>), typeof(DistributionBar),
        new PropertyMetadata(null, OnSegmentsChanged));

    public IReadOnlyList<VersionSegmentItem>? Segments
    {
        get => (IReadOnlyList<VersionSegmentItem>?)GetValue(SegmentsProperty);
        set => SetValue(SegmentsProperty, value);
    }

    public DistributionBar()
    {
        InitializeComponent();
        Loaded += (_, _) => Rebuild();
    }

    private static void OnSegmentsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => (d as DistributionBar)?.Rebuild();

    private void Rebuild()
    {
        if (BarRoot is null)
            return;

        BarRoot.Children.Clear();
        BarRoot.ColumnDefinitions.Clear();

        var segments = Segments?.Where(s => s.Weight > 0).ToList();
        if (segments is null || segments.Count == 0)
            return;

        for (int i = 0; i < segments.Count; i++)
        {
            var segment = segments[i];
            BarRoot.ColumnDefinitions.Add(new ColumnDefinition
            {
                Width = new GridLength(segment.Weight, GridUnitType.Star)
            });

            var bar = new Border
            {
                Height = 10,
                CornerRadius = new CornerRadius(5),
                Background = segment.Brush,
                Margin = new Thickness(i == 0 ? 0 : 2, 0, i == segments.Count - 1 ? 0 : 2, 0)
            };
            ToolTipService.SetToolTip(bar, $"{segment.Label} · {segment.Count}");
            Grid.SetColumn(bar, i);
            BarRoot.Children.Add(bar);
        }
    }
}
