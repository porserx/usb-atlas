using System.IO;
using System.Runtime.InteropServices;
using IOPath = System.IO.Path;
using Microsoft.UI;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using UsbAtlas.WinUI.Models;
using UsbAtlas.WinUI.ViewModels;
using Windows.ApplicationModel.DataTransfer;
using Windows.Graphics;
using Windows.UI;

namespace UsbAtlas.WinUI;

public sealed partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel = App.ViewModel;
    private string _startView = "overview";
    private int _startScroll;
    private DispatcherQueueTimer? _actionTimer;

    public MainWindow()
    {
        InitializeComponent();
        Root.DataContext = _viewModel;
        ConfigureWindow();
        ApplyTheme(App.Settings.Theme);
        Root.ActualThemeChanged += (_, _) => UpdateTitleBarTheme();
        Root.Loaded += Root_Loaded;
    }

    private void ConfigureWindow()
    {
        ApplyBackdrop(App.Settings.Backdrop);
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        try
        {
            AppWindow.Title = "USB Atlas";
            AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;
            AppWindow.TitleBar.ButtonBackgroundColor = Colors.Transparent;
            AppWindow.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;

            var iconPath = IOPath.Combine(AppContext.BaseDirectory, "Assets", "usb-atlas.ico");
            if (File.Exists(iconPath))
                AppWindow.SetIcon(iconPath);

            var area = DisplayArea.Primary;
            var work = area.WorkArea;
            double scale = GetDpiForWindow(WinRT.Interop.WindowNative.GetWindowHandle(this)) / 96.0;
            if (scale <= 0 || double.IsNaN(scale))
                scale = 1.0;

            int desiredWidth = (int)(1460 * scale);
            int desiredHeight = (int)(860 * scale);
            int width = Math.Min(desiredWidth, Math.Max((int)(1000 * scale), work.Width - 60));
            int height = Math.Min(desiredHeight, Math.Max((int)(680 * scale), work.Height - 30));
            int x = work.X + (work.Width - width) / 2;
            int y = work.Y + (work.Height - height) / 2;
            AppWindow.MoveAndResize(new RectInt32(x, y, width, height));
        }
        catch
        {
            // Keep system defaults if title-bar customization is unavailable.
        }

        UpdateTitleBarTheme();
    }

    [DllImport("user32.dll")]
    private static extern int GetDpiForWindow(IntPtr hwnd);

    public void ApplyBackdrop(BackdropMode mode)
    {
        try
        {
            SystemBackdrop = new MicaBackdrop
            {
                Kind = mode == BackdropMode.MicaAlt ? MicaKind.BaseAlt : MicaKind.Base
            };
        }
        catch
        {
            try
            {
                SystemBackdrop = new DesktopAcrylicBackdrop();
            }
            catch
            {
                // The opaque root background is a complete fallback.
            }
        }
    }

    private async void Root_Loaded(object sender, RoutedEventArgs e)
    {
        ParseStartView();
        ShowView(_startView);
        SelectNavItem(_startView);
        await _viewModel.RefreshAsync();

        await Task.Delay(150);
        ScrollActiveView(_startScroll);

        // WinUI can move focus into a page after the first layout pass, which
        // sometimes leaves the overview scrolled to the middle. Re-assert the
        // requested start position once the visual tree has settled.
        if (_startScroll == 0)
        {
            await Task.Delay(600);
            ScrollActiveView(0);
        }
    }

    public void ApplyTheme(AppThemeMode mode)
    {
        Root.RequestedTheme = mode switch
        {
            AppThemeMode.Light => ElementTheme.Light,
            AppThemeMode.Dark => ElementTheme.Dark,
            _ => ElementTheme.Default
        };
        UpdateTitleBarTheme();
    }

    private void UpdateTitleBarTheme()
    {
        bool isDark = Root.ActualTheme == ElementTheme.Dark;
        var titleBar = AppWindow.TitleBar;
        titleBar.ButtonBackgroundColor = Colors.Transparent;
        titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
        titleBar.ButtonForegroundColor = isDark ? Color.FromArgb(255, 242, 246, 255) : Color.FromArgb(255, 26, 28, 32);
        titleBar.ButtonInactiveForegroundColor = isDark ? Color.FromArgb(255, 125, 140, 160) : Color.FromArgb(255, 130, 132, 138);
        titleBar.ButtonHoverBackgroundColor = isDark ? Color.FromArgb(36, 255, 255, 255) : Color.FromArgb(18, 0, 0, 0);
        titleBar.ButtonPressedBackgroundColor = isDark ? Color.FromArgb(58, 255, 255, 255) : Color.FromArgb(32, 0, 0, 0);
        titleBar.ButtonHoverForegroundColor = isDark ? Colors.White : Colors.Black;
        titleBar.ButtonPressedForegroundColor = isDark ? Colors.White : Colors.Black;
    }

    private void ParseStartView()
    {
        var args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i].Equals("--view", StringComparison.OrdinalIgnoreCase))
            {
                string candidate = args[i + 1].ToLowerInvariant();
                if (candidate is "overview" or "ports" or "devices" or "about" or "credits" or "settings")
                    _startView = candidate;
                continue;
            }

            if (args[i].Equals("--scroll", StringComparison.OrdinalIgnoreCase) && int.TryParse(args[i + 1], out int scroll))
                _startScroll = scroll;
        }
    }

    private void Nav_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItemContainer is NavigationViewItem item && item.Tag is string tag)
            ShowView(tag);
    }

    private void ShowView(string key)
    {
        OverviewView.Visibility = key == "overview" ? Visibility.Visible : Visibility.Collapsed;
        PortMapView.Visibility = key == "ports" ? Visibility.Visible : Visibility.Collapsed;
        DevicesView.Visibility = key == "devices" ? Visibility.Visible : Visibility.Collapsed;
        AboutView.Visibility = key == "about" ? Visibility.Visible : Visibility.Collapsed;
        CreditsView.Visibility = key == "credits" ? Visibility.Visible : Visibility.Collapsed;
        SettingsView.Visibility = key == "settings" ? Visibility.Visible : Visibility.Collapsed;

        (string breadcrumb, string title) = key switch
        {
            "ports" => ("接口图谱", "物理接口图谱"),
            "devices" => ("实时设备", "USB 拓扑与设备"),
            "about" => ("检测说明", "数据与方法"),
            "credits" => ("关于", "关于 USB Atlas"),
            "settings" => ("设置", "个性化与选项"),
            _ => ("总览", "主板 USB 接口图鉴")
        };

        Breadcrumb.ItemsSource = new[] { "USB Atlas", breadcrumb };
        PageTitleText.Text = title;
    }

    private void SelectNavItem(string key)
    {
        NavigationViewItem? item = key switch
        {
            "ports" => NavPortsItem,
            "devices" => NavDevicesItem,
            "about" => NavAboutItem,
            "credits" => NavCreditsItem,
            "settings" => NavSettingsItem,
            _ => NavOverviewItem
        };
        if (item is not null)
            Nav.SelectedItem = item;
    }

    public void ShowDevices()
    {
        Nav.SelectedItem = NavDevicesItem;
        ShowView("devices");
    }

    public void ShowSettings()
    {
        Nav.SelectedItem = NavSettingsItem;
        ShowView("settings");
    }

    private async void Refresh_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel.IsScanning)
            return;
        await _viewModel.RefreshAsync();
    }

    private void CopyReport_Click(object sender, RoutedEventArgs e)
        => CopyReportToClipboard();

    public void CopyReportToClipboard()
    {
        try
        {
            var package = new DataPackage();
            package.SetText(_viewModel.BuildReportJson());
            Clipboard.SetContent(package);
            Clipboard.Flush();
            ShowActionMessage("诊断报告已复制为 JSON。");
        }
        catch (Exception ex)
        {
            ShowActionMessage($"复制失败：{ex.Message}");
        }
    }

    private void Settings_Click(object sender, RoutedEventArgs e)
        => ShowSettings();

    private void ShowActionMessage(string message)
    {
        ActionInfoBar.Message = message;
        ActionInfoBar.IsOpen = true;

        if (_actionTimer is null)
        {
            _actionTimer = DispatcherQueue.CreateTimer();
            _actionTimer.Interval = TimeSpan.FromSeconds(3);
            _actionTimer.IsRepeating = false;
            _actionTimer.Tick += (_, _) => ActionInfoBar.IsOpen = false;
        }

        _actionTimer.Stop();
        _actionTimer.Start();
    }

    private void RefreshAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        args.Handled = true;
        if (!_viewModel.IsScanning)
            Refresh_Click(this, new RoutedEventArgs());
    }

    private void ScrollActiveView(int offset)
    {
        UserControl? active = _startView switch
        {
            "ports" => PortMapView,
            "devices" => DevicesView,
            "about" => AboutView,
            "credits" => CreditsView,
            "settings" => SettingsView,
            _ => OverviewView
        };

        if (active?.Content is ScrollViewer scrollViewer)
            scrollViewer.ChangeView(null, offset, null, true);
    }
}
