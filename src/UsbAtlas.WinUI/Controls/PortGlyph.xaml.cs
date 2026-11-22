using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace UsbAtlas.WinUI.Controls;

public sealed partial class PortGlyph : UserControl
{
    public static readonly DependencyProperty PortTypeProperty = DependencyProperty.Register(
        nameof(PortType), typeof(string), typeof(PortGlyph),
        new PropertyMetadata("USB-A", OnVisualChanged));

    public static readonly DependencyProperty AccentBrushProperty = DependencyProperty.Register(
        nameof(AccentBrush), typeof(Brush), typeof(PortGlyph),
        new PropertyMetadata(new SolidColorBrush(Color.FromArgb(255, 34, 211, 238)), OnVisualChanged));

    public static readonly DependencyProperty SoftBrushProperty = DependencyProperty.Register(
        nameof(SoftBrush), typeof(Brush), typeof(PortGlyph),
        new PropertyMetadata(new SolidColorBrush(Color.FromArgb(30, 34, 211, 238)), OnVisualChanged));

    public string PortType
    {
        get => (string)GetValue(PortTypeProperty);
        set => SetValue(PortTypeProperty, value);
    }

    public Brush AccentBrush
    {
        get => (Brush)GetValue(AccentBrushProperty);
        set => SetValue(AccentBrushProperty, value);
    }

    public Brush SoftBrush
    {
        get => (Brush)GetValue(SoftBrushProperty);
        set => SetValue(SoftBrushProperty, value);
    }

    public PortGlyph()
    {
        InitializeComponent();
        UpdateVisuals();
    }

    private static void OnVisualChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => (d as PortGlyph)?.UpdateVisuals();

    private void UpdateVisuals()
    {
        if (TypeA is null || TypeC is null)
            return;

        bool isTypeC = string.Equals(PortType, "USB-C", StringComparison.OrdinalIgnoreCase);
        TypeA.Visibility = isTypeC ? Visibility.Collapsed : Visibility.Visible;
        TypeC.Visibility = isTypeC ? Visibility.Visible : Visibility.Collapsed;

        if (AccentBrush is SolidColorBrush accent)
        {
            TypeABody.BorderBrush = accent;
            TypeCBody.BorderBrush = accent;
            TypeALine.Fill = accent;
            TypeAPin1.Fill = accent;
            TypeAPin2.Fill = accent;
            TypeCLine.Fill = accent;
        }

        if (SoftBrush is SolidColorBrush soft)
        {
            TypeABody.Background = soft;
            TypeCBody.Background = soft;
        }
    }
}
