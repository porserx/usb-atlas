using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace UsbAtlas.WinUI.ViewModels;

public sealed class StatCardItem
{
    public string Index { get; init; } = "";
    public string Kicker { get; init; } = "";
    public string Label { get; init; } = "";
    public string Value { get; init; } = "0";
    public string Unit { get; init; } = "";
    public string Description { get; init; } = "";
    public Brush AccentBrush { get; init; } = Palette.Brush("cyan");
    public Brush SoftBrush { get; init; } = Palette.Soft("cyan");
}

public sealed class VersionStatItem
{
    public string Key { get; init; } = "usb2";
    public string Label { get; init; } = "";
    public string SpeedLabel { get; init; } = "";
    public string Description { get; init; } = "";
    public int Count { get; init; }
    public double Percent { get; init; }
    public string PercentLabel => $"{Percent:0.#}%";
    public string SubLabel => $"{SpeedLabel} · {Description}";
    public string TheoreticalLabel { get; init; } = "";
    public string TheoreticalDetail { get; init; } = "";
    public Brush AccentBrush => Palette.Brush(Key);
    public Brush SoftBrush => Palette.Soft(Key);
}

public sealed class VersionSegmentItem
{
    public string Label { get; init; } = "";
    public int Count { get; init; }
    public double Weight { get; init; }
    public Brush Brush { get; init; } = Palette.Brush("cyan");
}

public sealed class PortItem
{
    public string IndexLabel { get; init; } = "";
    public string PortType { get; init; } = "USB-A";
    public string TypeLabel { get; init; } = "Type-A";
    public string Version { get; init; } = "";
    public string SpeedLabel { get; init; } = "";
    public string SpeedBadge { get; init; } = "";
    public string TheoreticalShortLabel { get; init; } = "";
    public string TheoreticalDetail { get; init; } = "";
    public string VersionKey { get; init; } = "usb2";
    public string Note { get; init; } = "";
    public string Header { get; init; } = "";
    public bool IsTypeC => PortType.Equals("USB-C", StringComparison.OrdinalIgnoreCase);
    public Brush AccentBrush => Palette.Brush(VersionKey);
    public Brush SoftBrush => Palette.Soft(VersionKey);
}

public sealed class FrontGroupItem
{
    public string Header { get; init; } = "";
    public string Subtitle { get; init; } = "";
    public string CountLabel { get; init; } = "";
    public IReadOnlyList<PortItem> Ports { get; init; } = [];
    public Brush AccentBrush { get; init; } = Palette.Brush("usb32_5");
    public Brush SoftBrush { get; init; } = Palette.Soft("usb32_5");
}

public sealed class DeviceItem
{
    public string Name { get; init; } = "";
    public string ManufacturerLabel { get; init; } = "厂商未提供";
    public string TypeLabel { get; init; } = "USB 设备";
    public string VersionLabel { get; init; } = "USB 2.0";
    public string SpeedLabel { get; init; } = "";
    public string VersionKey { get; init; } = "usb2";
    public string PortLine { get; init; } = "";
    public string VidPid { get; init; } = "";
    public bool IsInternal { get; init; }
    public bool IsHub { get; init; }
    public string ScopeLabel => IsInternal ? "内置" : "外接";
    public string IconGlyph { get; init; } = "\uE88E";
    public Brush AccentBrush => Palette.Brush(VersionKey);
    public Brush SoftBrush => Palette.Soft(VersionKey);
}

public sealed class LiveGroupItem
{
    public string PortsLabel { get; init; } = "";
    public string VersionLabel { get; init; } = "";
    public string ConnectorLabel { get; init; } = "";
    public string StatusLabel { get; init; } = "";
    public string DeviceName { get; init; } = "";
    public bool IsConnected { get; init; }
    public bool IsInternal { get; init; }
    public string VersionKey { get; init; } = "usb2";
    public string LaneLabel { get; init; } = "";
    public Brush AccentBrush => Palette.Brush(VersionKey);
    public Brush SoftBrush => Palette.Soft(VersionKey);
    public Brush StatusBrush => IsConnected ? Palette.Brush("green") : new SolidColorBrush(Windows.UI.Color.FromArgb(255, 51, 65, 85));
    public double Opacity => IsInternal ? 0.55 : 1.0;
    public string Tooltip => $"{PortsLabel} · {VersionLabel} · {ConnectorLabel} · {StatusLabel}{(string.IsNullOrWhiteSpace(DeviceName) ? "" : $" · {DeviceName}")}";
}

public sealed class ControllerItem
{
    public string Name { get; init; } = "";
    public string Vendor { get; init; } = "";
    public string ControllerId { get; init; } = "";
    public string Initials { get; init; } = "USB";
    public string AccentKey { get; init; } = "blue";
    public int RootPorts { get; init; }
    public int Usb2Lanes { get; init; }
    public int Usb3Lanes { get; init; }
    public int UserGroups { get; init; }
    public int ConnectedCount { get; init; }
    public string PortsSummary { get; init; } = "";
    public IReadOnlyList<LiveGroupItem> LiveGroups { get; init; } = [];
    public Brush AccentBrush => Palette.Brush(AccentKey);
    public Brush SoftBrush => Palette.Soft(AccentKey);
    public GridLength Usb2Width => new(Usb2Lanes, GridUnitType.Star);
    public GridLength Usb3Width => new(Usb3Lanes, GridUnitType.Star);
    public string ConnectedLabel => $"{ConnectedCount} 台已连接";
    public string UserGroupLabel => $"{UserGroups} 个用户接口";
}

public sealed class KeyValueItem
{
    public string Key { get; init; } = "";
    public string Value { get; init; } = "";
}
