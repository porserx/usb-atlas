using Microsoft.UI.Xaml;
using UsbAtlas.WinUI.Models;
using UsbAtlas.WinUI.Services;
using UsbAtlas.WinUI.ViewModels;

namespace UsbAtlas.WinUI;

public partial class App : Application
{
    public static MainViewModel ViewModel { get; private set; } = null!;
    public static SettingsStore SettingsStore { get; private set; } = null!;
    public static AppSettings Settings { get; private set; } = new();
    public static MainWindow? Shell { get; private set; }

    public App()
    {
        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        SettingsStore = new SettingsStore();
        Settings = SettingsStore.Load();
        ViewModel = new MainViewModel();
        ViewModel.ApplyDeviceFilter(Settings.ShowInternalDevices);

        Shell = new MainWindow();
        Shell.Activate();
    }

    public static void SaveSettings()
    {
        SettingsStore.Save(Settings);
    }
}
