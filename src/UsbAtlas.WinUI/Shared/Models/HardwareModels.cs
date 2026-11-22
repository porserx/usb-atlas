namespace UsbAtlas.Models;

public sealed class BoardInfo
{
    public string Manufacturer { get; set; } = "未知主板";
    public string Product { get; set; } = "未知型号";
    public string Version { get; set; } = "";
    public string BiosVendor { get; set; } = "";
    public string BiosVersion { get; set; } = "";
    public string BiosDate { get; set; } = "";
    public string CpuName { get; set; } = "";
    public string OsName { get; set; } = "";
    public string DisplayName => string.IsNullOrWhiteSpace(Manufacturer) ? Product : $"{Manufacturer} {Product}".Trim();
}

public sealed class HardwareSnapshot
{
    public BoardInfo Board { get; set; } = new();
    public BoardProfile? Profile { get; set; }
    public List<ControllerInfo> Controllers { get; set; } = [];
    public List<PortGroupInfo> Groups { get; set; } = [];
    public List<ConnectedDeviceInfo> Devices { get; set; } = [];
    public DateTime ScannedAt { get; set; } = DateTime.Now;
    public string ScanDurationLabel { get; set; } = "";
    public string? ScanWarning { get; set; }

    public int RootLaneCount => Controllers.Sum(c => c.RootHubPortCount);
    public int UserGroupCount => Groups.Count(g => g.IsUserConnectable);
    public int InternalGroupCount => Groups.Count(g => !g.IsUserConnectable);
    public int ConnectedGroupCount => Groups.Count(g => g.IsConnected);
    public int ControllerCount => Controllers.Count;

    public int PhysicalPortCount => Profile?.TotalPorts ?? UserGroupCount;
    public bool HasProfile => Profile is not null;
    public string BoardDisplayName => Profile?.DisplayName ?? Board.DisplayName;

    public int PhysicalUsb3Count => Profile is not null
        ? Profile.Rear.Concat(Profile.Front).Where(p => p.VersionKey.StartsWith("usb32", StringComparison.Ordinal)).Sum(p => p.Count)
        : Groups.Count(g => g.IsUserConnectable && g.HasUsb3);

    public int PhysicalUsb2Count => Profile is not null
        ? Profile.Rear.Concat(Profile.Front).Where(p => p.VersionKey == "usb2").Sum(p => p.Count)
        : Groups.Count(g => g.IsUserConnectable && !g.HasUsb3);
}

public sealed class ControllerInfo
{
    public string Name { get; set; } = "USB 主机控制器";
    public string Vendor { get; set; } = "";
    public string PciAddress { get; set; } = "";
    public string DeviceId { get; set; } = "";
    public string RawDescription { get; set; } = "";
    public string RootHubInstance { get; set; } = "";
    public string RootHubPath { get; set; } = "";
    public int RootHubPortCount { get; set; }
    public int Usb2LaneCount { get; set; }
    public int Usb3LaneCount { get; set; }
    public string AccentKey { get; set; } = "blue";
    public string Initials { get; set; } = "USB";
    public string SpeedHint { get; set; } = "USB 3.x xHCI";
    public List<PortGroupInfo> Groups { get; set; } = [];

    public int UserGroupCount => Groups.Count(g => g.IsUserConnectable);
    public int ConnectedCount => Groups.Count(g => g.IsConnected);
    public string PortsSummary => $"{RootHubPortCount} 个根端口 · {Usb2LaneCount} USB 2.0 / {Usb3LaneCount} USB 3.x 通道";
    public string ControllerId => $"PCIe {PciAddress} · {DeviceId}";
}

public sealed class PortGroupInfo
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public List<int> Ports { get; set; } = [];
    public bool HasUsb2 { get; set; }
    public bool HasUsb3 { get; set; }
    public bool HasUsb110 { get; set; }
    public bool IsUserConnectable { get; set; }
    public bool IsTypeC { get; set; }
    public bool IsDebugCapable { get; set; }
    public bool IsConnected { get; set; }
    public ConnectedDeviceInfo? Device { get; set; }
    public string ControllerName { get; set; } = "";
    public string ControllerVendor { get; set; } = "";
    public string ControllerAccentKey { get; set; } = "blue";

    public int PrimaryPort => Ports.Count == 0 ? 0 : Ports.Min();
    public string PortsLabel => Ports.Count switch
    {
        0 => "—",
        1 => $"端口 {Ports[0]:D2}",
        _ => $"端口 {Ports.Min():D2} + {Ports.Max():D2}"
    };
    public string VersionLabel => HasUsb3 ? "USB 3.x" : HasUsb2 ? "USB 2.0" : "USB 1.1";
    public string VersionKey => HasUsb3 ? "usb3" : "usb2";
    public string SpeedLabel => HasUsb3 ? "5–10 Gbps" : HasUsb2 ? "480 Mbps" : "12 Mbps";
    public string VisibilityLabel => IsUserConnectable ? "用户可连接" : "内置 / 非外露";
    public bool IsInternal => !IsUserConnectable;
    public string ConnectorLabel => IsTypeC ? "Type-C" : "Type-A";
    public string LaneLabel => Ports.Count > 1 ? $"{Ports.Count} 条通道" : "单通道";
}

public sealed class ConnectedDeviceInfo
{
    public string Name { get; set; } = "USB 设备";
    public string Manufacturer { get; set; } = "";
    public string ClassLabel { get; set; } = "USB 设备";
    public string ControllerName { get; set; } = "";
    public int PortNumber { get; set; }
    public string PortLabel { get; set; } = "";
    public string SpeedLabel { get; set; } = "";
    public string VersionKey { get; set; } = "usb2";
    public string VersionLabel { get; set; } = "USB 2.0";
    public string VidPid { get; set; } = "";
    public int Address { get; set; }
    public bool IsHub { get; set; }
    public bool IsInternal { get; set; }
    public string DeviceTypeLabel => IsHub ? "集线器" : ClassLabel;
    public string PortAndController => $"{ControllerName} · 端口 {PortNumber:D2}";
    public string NameWithManufacturer => string.IsNullOrWhiteSpace(Manufacturer) || Name.Contains(Manufacturer, StringComparison.OrdinalIgnoreCase)
        ? Name
        : $"{Manufacturer} {Name}";
}
