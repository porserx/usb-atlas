using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using UsbAtlas.WinUI.ViewModels;

namespace UsbAtlas.WinUI.Views;

public sealed partial class AboutView : UserControl
{
    public AboutView()
    {
        InitializeComponent();
    }

    private async void SpecLink_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel viewModel || string.IsNullOrWhiteSpace(viewModel.ProfileSpecUrl))
            return;

        try
        {
            await Windows.System.Launcher.LaunchUriAsync(new Uri(viewModel.ProfileSpecUrl));
        }
        catch
        {
            // The URL remains visible; opening a browser is a convenience.
        }
    }
}
