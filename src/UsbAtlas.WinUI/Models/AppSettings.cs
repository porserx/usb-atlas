namespace UsbAtlas.WinUI.Models;

public enum AppThemeMode
{
    System,
    Light,
    Dark
}

public enum BackdropMode
{
    Mica,
    MicaAlt
}

public sealed class AppSettings
{
    public AppThemeMode Theme { get; set; } = AppThemeMode.System;
    public BackdropMode Backdrop { get; set; } = BackdropMode.MicaAlt;
    public bool ShowInternalDevices { get; set; } = true;
}
