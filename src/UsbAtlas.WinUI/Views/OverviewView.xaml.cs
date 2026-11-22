using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace UsbAtlas.WinUI.Views;

public sealed partial class OverviewView : UserControl
{
    public OverviewView()
    {
        InitializeComponent();
    }

    private void ViewAllDevices_Click(object sender, RoutedEventArgs e)
    {
        if (App.Shell is { } window)
            window.ShowDevices();
    }
}
