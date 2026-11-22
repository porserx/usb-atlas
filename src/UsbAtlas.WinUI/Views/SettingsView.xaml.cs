using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using UsbAtlas.WinUI.Models;

namespace UsbAtlas.WinUI.Views;

public sealed partial class SettingsView : UserControl
{
    private bool _initializing;

    public SettingsView()
    {
        InitializeComponent();
        DataContext = App.ViewModel;
        Loaded += SettingsView_Loaded;
    }

    private void SettingsView_Loaded(object sender, RoutedEventArgs e)
    {
        _initializing = true;
        ThemeOptions.SelectedIndex = App.Settings.Theme switch
        {
            AppThemeMode.Light => 1,
            AppThemeMode.Dark => 2,
            _ => 0
        };
        ShowInternalToggle.IsOn = App.Settings.ShowInternalDevices;
        BackdropOptions.SelectedIndex = App.Settings.Backdrop == BackdropMode.MicaAlt ? 1 : 0;
        SettingsPathText.Text = App.SettingsStore.SettingsPath;
        _initializing = false;
    }

    private void ThemeOptions_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_initializing)
            return;

        App.Settings.Theme = ThemeOptions.SelectedIndex switch
        {
            1 => AppThemeMode.Light,
            2 => AppThemeMode.Dark,
            _ => AppThemeMode.System
        };
        App.SaveSettings();
        App.Shell?.ApplyTheme(App.Settings.Theme);
    }

    private void BackdropOptions_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_initializing)
            return;

        App.Settings.Backdrop = BackdropOptions.SelectedIndex == 1 ? BackdropMode.MicaAlt : BackdropMode.Mica;
        App.SaveSettings();
        App.Shell?.ApplyBackdrop(App.Settings.Backdrop);
    }

    private void ShowInternalToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (_initializing)
            return;

        App.Settings.ShowInternalDevices = ShowInternalToggle.IsOn;
        App.ViewModel.ApplyDeviceFilter(ShowInternalToggle.IsOn);
        App.SaveSettings();
    }

    private async void Rescan_Click(object sender, RoutedEventArgs e)
    {
        RescanButton.IsEnabled = false;
        try
        {
            await App.ViewModel.RefreshAsync();
        }
        finally
        {
            RescanButton.IsEnabled = true;
        }
    }

    private void CopyReport_Click(object sender, RoutedEventArgs e)
        => App.Shell?.CopyReportToClipboard();

    private async void SpecLink_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(App.ViewModel.ProfileSpecUrl))
            return;

        try
        {
            await Windows.System.Launcher.LaunchUriAsync(new Uri(App.ViewModel.ProfileSpecUrl));
        }
        catch
        {
            // Ignore browser launch failures; the URL remains visible in the app.
        }
    }
}
