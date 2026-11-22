using System.Reflection;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using UsbAtlas.Models;
using UsbAtlas.Services;

namespace UsbAtlas.WinUI.ViewModels;

public sealed class MainViewModel : ObservableObject
{
    private bool _isScanning;
    private List<DeviceItem> _allDevices = [];

    public bool IsScanning
    {
        get => _isScanning;
        private set
        {
            if (SetProperty(ref _isScanning, value))
                OnPropertyChanged(nameof(IsNotScanning));
        }
    }

    public bool IsNotScanning => !IsScanning;

    public string BoardName { get; private set; } = "正在读取主板信息…";
    public string BoardMetaLine { get; private set; } = "准备扫描 USB 拓扑";
    public string BoardManufacturer { get; private set; } = "";
    public string BoardProduct { get; private set; } = "";
    public bool ProfileMatched { get; private set; }
    public string ProfileBadgeText { get; private set; } = "LIVE SCAN";
    public string ProfileSourceLabel { get; private set; } = "Windows 实时识别";
    public string ProfileVerifiedLabel { get; private set; } = "";
    public string ProfileSpecUrl { get; private set; } = "";
    public string HeroNote { get; private set; } = "";
    public string ReconciliationText { get; private set; } = "";
    public string SourceCaption { get; private set; } = "";
    public string FooterNote { get; private set; } = "";

    public int PhysicalPortCount { get; private set; }
    public int Usb3PortCount { get; private set; }
    public int Usb2PortCount { get; private set; }
    public int TypeCCount { get; private set; }
    public int ControllerCount { get; private set; }
    public int RootLaneCount { get; private set; }
    public int UserGroupCount { get; private set; }
    public int InternalGroupCount { get; private set; }
    public int ConnectedDeviceCount { get; private set; }
    public int InternalDeviceCount { get; private set; }
    public int HubDeviceCount { get; private set; }
    public int PortGroupCount { get; private set; }
    public int TotalUsb2Lanes { get; private set; }
    public int TotalUsb3Lanes { get; private set; }
    public string VersionHeadline { get; private set; } = "";
    public string ConnectedHeadline { get; private set; } = "";
    public string LaneHeadline { get; private set; } = "";
    public string LaneComposition { get; private set; } = "";
    public GridLength Usb2LaneWidth => new(TotalUsb2Lanes, GridUnitType.Star);
    public GridLength Usb3LaneWidth => new(TotalUsb3Lanes, GridUnitType.Star);

    public string ScanStatus { get; private set; } = "准备扫描";
    public string ScanStatusDetail { get; private set; } = "";
    public string ScanTimeLabel { get; private set; } = "--:--:--";
    public string ScanDateLabel { get; private set; } = "";
    public string ScanDurationLabel { get; private set; } = "";
    public Brush ScanStatusBrush { get; private set; } = Palette.Brush("amber");
    public string? WarningText { get; private set; }
    public bool HasWarning { get; private set; }
    public string? ErrorMessage { get; private set; }
    public bool HasError { get; private set; }
    public bool HasData { get; private set; }

    public string RearSectionTitle { get; private set; } = "后置 I/O";
    public string RearSectionSubtitle { get; private set; } = "";
    public string FrontSectionTitle { get; private set; } = "前置 / 板载接针";
    public string FrontSectionSubtitle { get; private set; } = "";

    public IReadOnlyList<StatCardItem> StatCards { get; private set; } = [];
    public IReadOnlyList<VersionStatItem> VersionStats { get; private set; } = [];
    public IReadOnlyList<VersionSegmentItem> VersionSegments { get; private set; } = [];
    public IReadOnlyList<PortItem> RearPorts { get; private set; } = [];
    public IReadOnlyList<PortItem> FrontPorts { get; private set; } = [];
    public IReadOnlyList<FrontGroupItem> FrontGroups { get; private set; } = [];
    public IReadOnlyList<ControllerItem> Controllers { get; private set; } = [];
    public IReadOnlyList<DeviceItem> Devices { get; private set; } = [];
    public IReadOnlyList<DeviceItem> PreviewDevices { get; private set; } = [];
    public IReadOnlyList<KeyValueItem> EnvironmentItems { get; private set; } = [];
    public IReadOnlyList<string> DataNotes { get; private set; } = [];
    public string AppVersion { get; private set; } = "1.0.0";
    public string ScanEngineLabel { get; private set; } = "Native USB Hub IOCTL";
    public HardwareSnapshot? Snapshot { get; private set; }
    public bool ShowInternalDevices { get; private set; } = true;
    public int HiddenInternalDeviceCount => _allDevices.Count(d => d.IsInternal);
    public string DeviceFilterLabel => ShowInternalDevices ? "全部根端口设备" : "仅显示外接设备";

    public MainViewModel()
    {
        AppVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.0.0";
        DataNotes =
        [
            "接口数量与 USB Gen 版本来自匹配的主板官方规格档案；实时扫描负责校验连接状态与端口通道。",
            "实时连接状态通过 Windows USB Hub IOCTL 读取，无需管理员权限，不安装驱动。",
            "Windows 只暴露空口的 USB 2.0 / USB 3.x 协议能力，无法逐口区分 Gen 1 / Gen 2，因此实时通道以 USB 3.x 聚合。",
            "用户可连接标志来自端口连接器属性；部分前面板接针会被固件标记为内置，实时数量可能少于官方物理接口总数。",
            "根端口包含蓝牙、灯控、前面板接针等内置链路；物理接口总数不包含外接 USB 集线器扩展出的端口。",
            "版本卡片中的“理论有效速率”已按 USB 3.x 的 8b/10b / 128b/132b 编码折算；实际文件传输还会受到协议开销与设备能力影响。"
        ];
    }

    public async Task RefreshAsync()
    {
        if (IsScanning)
            return;

        IsScanning = true;
        ErrorMessage = null;
        HasError = false;
        ScanStatus = "正在扫描";
        ScanStatusDetail = "读取 USB 根集线器与设备描述符";
        ScanStatusBrush = Palette.Brush("amber");
        OnPropertyChanged(string.Empty);

        try
        {
            var snapshot = await Task.Run(HardwareCollector.Collect);
            Apply(snapshot);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            HasError = true;
            ScanStatus = "扫描失败";
            ScanStatusDetail = "可尝试重新扫描";
            ScanStatusBrush = Palette.Brush("rose");
        }
        finally
        {
            IsScanning = false;
            OnPropertyChanged(string.Empty);
        }
    }

    private void Apply(HardwareSnapshot snapshot)
    {
        Snapshot = snapshot;
        ProfileMatched = snapshot.Profile is not null;
        BoardName = snapshot.BoardDisplayName;
        BoardManufacturer = snapshot.Board.Manufacturer;
        BoardProduct = snapshot.Board.Product;
        BoardMetaLine = snapshot.Profile is not null
            ? string.Join(" · ", new[] { snapshot.Profile.FormFactor, snapshot.Profile.Chipset, $"BIOS {snapshot.Board.BiosVersion}" }
                .Where(s => !string.IsNullOrWhiteSpace(s)))
            : string.Join(" · ", new[] { snapshot.Board.Manufacturer, snapshot.Board.Version, $"BIOS {snapshot.Board.BiosVersion}" }
                .Where(s => !string.IsNullOrWhiteSpace(s)));

        ProfileBadgeText = ProfileMatched ? "OFFICIAL PROFILE" : "LIVE SCAN";
        ProfileSourceLabel = snapshot.Profile?.SourceLabel ?? "Windows 实时识别";
        ProfileVerifiedLabel = string.IsNullOrWhiteSpace(snapshot.Profile?.VerifiedOn)
            ? ""
            : $"规格核对 {snapshot.Profile.VerifiedOn}";
        ProfileSpecUrl = snapshot.Profile?.SpecUrl ?? "";

        PhysicalPortCount = snapshot.PhysicalPortCount;
        Usb3PortCount = snapshot.PhysicalUsb3Count;
        Usb2PortCount = snapshot.PhysicalUsb2Count;
        TypeCCount = snapshot.Profile is not null
            ? snapshot.Profile.Rear.Concat(snapshot.Profile.Front)
                .Where(p => p.Type.Contains("C", StringComparison.OrdinalIgnoreCase))
                .Sum(p => p.Count)
            : snapshot.Groups.Count(g => g.IsUserConnectable && g.IsTypeC);
        ControllerCount = snapshot.ControllerCount;
        RootLaneCount = snapshot.RootLaneCount;
        UserGroupCount = snapshot.UserGroupCount;
        InternalGroupCount = snapshot.InternalGroupCount;
        ConnectedDeviceCount = snapshot.ConnectedGroupCount;
        InternalDeviceCount = snapshot.Devices.Count(d => d.IsInternal);
        HubDeviceCount = snapshot.Devices.Count(d => d.IsHub);
        PortGroupCount = snapshot.Groups.Count;
        TotalUsb2Lanes = snapshot.Controllers.Sum(c => c.Usb2LaneCount);
        TotalUsb3Lanes = snapshot.Controllers.Sum(c => c.Usb3LaneCount);
        VersionHeadline = $"{Usb3PortCount} 个 USB 3.x · {Usb2PortCount} 个 USB 2.0";
        ConnectedHeadline = $"{ConnectedDeviceCount} 台设备 · {InternalDeviceCount} 台内置";
        LaneHeadline = $"{ControllerCount} 个主控 · {RootLaneCount} 条根端口通道";
        LaneComposition = $"USB 2.0 {TotalUsb2Lanes} 条 · USB 3.x {TotalUsb3Lanes} 条";

        HasData = snapshot.Controllers.Count > 0 && snapshot.Groups.Count > 0;
        ScanStatus = HasData ? "扫描完成" : "未发现 USB 拓扑";
        ScanStatusDetail = $"{snapshot.ScanDurationLabel} · {snapshot.Controllers.Count} 个主控";
        ScanTimeLabel = snapshot.ScannedAt.ToString("HH:mm:ss");
        ScanDateLabel = snapshot.ScannedAt.ToString("yyyy.MM.dd");
        ScanDurationLabel = snapshot.ScanDurationLabel;
        ScanStatusBrush = Palette.Brush(HasData ? "green" : "rose");
        WarningText = snapshot.ScanWarning;
        HasWarning = !string.IsNullOrWhiteSpace(WarningText);

        HeroNote = ProfileMatched
            ? "官方接口规格 + Windows 实时拓扑交叉校验 · 数量含前置接针"
            : "Windows ACPI / 端口连接器属性识别的用户可连接接口";
        ReconciliationText = ProfileMatched
            ? $"官方 {PhysicalPortCount} 个物理接口 · Windows 识别 {UserGroupCount} 个用户接口 · {RootLaneCount} 条根端口通道"
            : $"Windows 识别 {UserGroupCount} 个用户接口 · {RootLaneCount} 条根端口通道";
        SourceCaption = ProfileMatched
            ? $"{snapshot.Profile!.SourceLabel} · {snapshot.Profile.FormFactor} · {snapshot.Profile.Chipset}"
            : "Windows 实时识别 · 无主板档案";
        FooterNote = $"根端口共 {RootLaneCount} 条；含 {InternalGroupCount} 个内置/非外露通道。物理接口数量以官方规格为口径。";

        BuildStats();
        BuildVersionStats(snapshot);
        BuildPortLayout(snapshot);
        BuildControllers(snapshot);
        BuildDevices(snapshot);

        EnvironmentItems =
        [
            new KeyValueItem { Key = "主板", Value = snapshot.BoardDisplayName },
            new KeyValueItem { Key = "芯片组 / 板型", Value = snapshot.Profile is not null ? $"{snapshot.Profile.Chipset} · {snapshot.Profile.FormFactor}" : "未匹配档案" },
            new KeyValueItem { Key = "处理器", Value = snapshot.Board.CpuName },
            new KeyValueItem { Key = "操作系统", Value = snapshot.Board.OsName },
            new KeyValueItem { Key = "BIOS", Value = string.Join(" · ", new[] { snapshot.Board.BiosVendor, snapshot.Board.BiosVersion, snapshot.Board.BiosDate }.Where(s => !string.IsNullOrWhiteSpace(s))) },
            new KeyValueItem { Key = "USB 主控", Value = $"{ControllerCount} 个 · {RootLaneCount} 条根端口" },
            new KeyValueItem { Key = "扫描耗时", Value = snapshot.ScanDurationLabel },
            new KeyValueItem { Key = "数据来源", Value = $"{ScanEngineLabel} + 主板规格档案" }
        ];

        ErrorMessage = null;
        HasError = false;
        OnPropertyChanged(string.Empty);
    }

    private void BuildStats()
    {
        StatCards =
        [
            new StatCardItem
            {
                Index = "01",
                Kicker = "PHYSICAL PORTS",
                Label = "物理 USB 接口",
                Value = PhysicalPortCount.ToString(),
                Unit = "个",
                Description = ProfileMatched ? "官方规格 · 含前置接针" : "系统识别 · 用户可连接",
                AccentBrush = Palette.Brush("cyan"),
                SoftBrush = Palette.Soft("cyan")
            },
            new StatCardItem
            {
                Index = "02",
                Kicker = "SUPERSPEED",
                Label = "USB 3.x 接口",
                Value = Usb3PortCount.ToString(),
                Unit = "个",
                Description = "Gen 1 · Gen 2 · Gen 2x2",
                AccentBrush = Palette.Brush("violet"),
                SoftBrush = Palette.Soft("violet")
            },
            new StatCardItem
            {
                Index = "03",
                Kicker = "USB 2.0",
                Label = "USB 2.0 接口",
                Value = Usb2PortCount.ToString(),
                Unit = "个",
                Description = "480 Mbps 兼容通道",
                AccentBrush = Palette.Brush("blue"),
                SoftBrush = Palette.Soft("blue")
            },
            new StatCardItem
            {
                Index = "04",
                Kicker = "ROOT DEVICES",
                Label = "根端口设备",
                Value = ConnectedDeviceCount.ToString(),
                Unit = "台",
                Description = "实时连接 · 含内置设备",
                AccentBrush = Palette.Brush("green"),
                SoftBrush = Palette.Soft("green")
            }
        ];
    }

    private void BuildVersionStats(HardwareSnapshot snapshot)
    {
        if (snapshot.Profile is not null)
        {
            var groups = snapshot.Profile.Rear.Concat(snapshot.Profile.Front)
                .GroupBy(p => p.VersionKey)
                .Select(g => new
                {
                    Key = g.Key,
                    Label = g.First().Version,
                    Speed = g.First().SpeedLabel,
                    Count = g.Sum(p => p.Count)
                })
                .OrderByDescending(g => Palette.SortOrder(g.Key))
                .ToList();

            int total = groups.Sum(g => g.Count);
            VersionStats = groups.Select(g => new VersionStatItem
            {
                Key = g.Key,
                Label = g.Label,
                SpeedLabel = g.Speed,
                Description = Palette.Description(g.Key),
                TheoreticalLabel = UsbSpeed.TheoreticalLabel(g.Key),
                TheoreticalDetail = UsbSpeed.TheoreticalDetail(g.Key),
                Count = g.Count,
                Percent = total == 0 ? 0 : g.Count * 100.0 / total
            }).ToList();
        }
        else
        {
            var userGroups = snapshot.Groups.Where(g => g.IsUserConnectable).ToList();
            int total = userGroups.Count;
            var list = new List<VersionStatItem>();
            int usb3 = userGroups.Count(g => g.HasUsb3);
            int usb2 = userGroups.Count(g => !g.HasUsb3);
            if (usb3 > 0)
                list.Add(new VersionStatItem { Key = "usb3", Label = "USB 3.x", SpeedLabel = "5–10 Gbps", Description = "超高速通道", TheoreticalLabel = UsbSpeed.TheoreticalLabel("usb3"), TheoreticalDetail = UsbSpeed.TheoreticalDetail("usb3"), Count = usb3, Percent = total == 0 ? 0 : usb3 * 100.0 / total });
            if (usb2 > 0)
                list.Add(new VersionStatItem { Key = "usb2", Label = "USB 2.0", SpeedLabel = "480 Mbps", Description = "高速 480M", TheoreticalLabel = UsbSpeed.TheoreticalLabel("usb2"), TheoreticalDetail = UsbSpeed.TheoreticalDetail("usb2"), Count = usb2, Percent = total == 0 ? 0 : usb2 * 100.0 / total });
            VersionStats = list;
        }

        VersionSegments = VersionStats
            .Where(v => v.Count > 0)
            .Select(v => new VersionSegmentItem { Label = v.Label, Count = v.Count, Weight = v.Count, Brush = v.AccentBrush })
            .ToList();
    }

    private void BuildPortLayout(HardwareSnapshot snapshot)
    {
        if (snapshot.Profile is not null)
        {
            RearPorts = BuildProfilePorts(snapshot.Profile.Rear, "REAR");
            FrontPorts = BuildProfilePorts(snapshot.Profile.Front, "FRONT");
            FrontGroups = FrontPorts
                .GroupBy(p => p.Header)
                .Select(g => new FrontGroupItem
                {
                    Header = string.IsNullOrWhiteSpace(g.Key) ? "板载接针" : g.Key,
                    Subtitle = $"{g.Count()} 个接口 · {string.Join(" / ", g.Select(p => p.Version).Distinct())}",
                    CountLabel = $"{g.Count()} 个",
                    Ports = g.ToList(),
                    AccentBrush = Palette.Brush(g.First().VersionKey),
                    SoftBrush = Palette.Soft(g.First().VersionKey)
                })
                .ToList();
            RearSectionTitle = "后置 I/O";
            RearSectionSubtitle = $"{RearPorts.Count} 个接口 · 官方规格";
            FrontSectionTitle = "前置 / 板载接针";
            FrontSectionSubtitle = $"{FrontPorts.Count} 个接口 · 官方规格";
        }
        else
        {
            var live = snapshot.Groups.Where(g => g.IsUserConnectable).ToList();
            RearPorts = BuildLivePorts(live);
            FrontPorts = [];
            FrontGroups = [];
            RearSectionTitle = "系统识别 · 用户可连接接口";
            RearSectionSubtitle = $"{live.Count} 个接口 · ACPI / 连接器属性";
            FrontSectionTitle = "";
            FrontSectionSubtitle = "";
        }
    }

    private static List<PortItem> BuildProfilePorts(IEnumerable<BoardPortGroup> groups, string prefix)
    {
        var cards = new List<PortItem>();
        int index = 1;
        foreach (var group in groups)
        {
            for (int i = 0; i < group.Count; i++)
            {
                cards.Add(new PortItem
                {
                    IndexLabel = $"{prefix} {index:D2}",
                    PortType = group.Type,
                    TypeLabel = group.Type == "USB-C" ? "Type-C" : "Type-A",
                    Version = group.Version,
                    SpeedLabel = group.SpeedLabel,
                    SpeedBadge = SpeedBadge(group.VersionKey),
                    TheoreticalShortLabel = UsbSpeed.ShortLabel(group.VersionKey),
                    TheoreticalDetail = UsbSpeed.TheoreticalDetail(group.VersionKey),
                    VersionKey = group.VersionKey,
                    Note = group.Note ?? "",
                    Header = group.Header ?? ""
                });
                index++;
            }
        }

        return cards;
    }

    private static List<PortItem> BuildLivePorts(IEnumerable<PortGroupInfo> groups)
    {
        var cards = new List<PortItem>();
        int index = 1;
        foreach (var group in groups)
        {
            cards.Add(new PortItem
            {
                IndexLabel = $"LIVE {index:D2}",
                PortType = group.IsTypeC ? "USB-C" : "USB-A",
                TypeLabel = group.ConnectorLabel,
                Version = group.VersionLabel,
                SpeedLabel = group.SpeedLabel,
                SpeedBadge = SpeedBadge(group.VersionKey),
                TheoreticalShortLabel = UsbSpeed.ShortLabel(group.VersionKey),
                TheoreticalDetail = UsbSpeed.TheoreticalDetail(group.VersionKey),
                VersionKey = group.VersionKey,
                Note = group.IsConnected ? $"已连接 · {group.Device?.Name}" : "空闲",
                Header = ""
            });
            index++;
        }

        return cards;
    }

    private void BuildControllers(HardwareSnapshot snapshot)
    {
        Controllers = snapshot.Controllers.Select(controller => new ControllerItem
        {
            Name = controller.Name,
            Vendor = controller.Vendor,
            ControllerId = string.IsNullOrWhiteSpace(controller.PciAddress)
                ? controller.DeviceId
                : $"PCIe {controller.PciAddress} · {controller.DeviceId}",
            Initials = controller.Initials,
            AccentKey = controller.AccentKey,
            RootPorts = controller.RootHubPortCount,
            Usb2Lanes = controller.Usb2LaneCount,
            Usb3Lanes = controller.Usb3LaneCount,
            UserGroups = controller.UserGroupCount,
            ConnectedCount = controller.ConnectedCount,
            PortsSummary = controller.PortsSummary,
            LiveGroups = controller.Groups.Select(group => new LiveGroupItem
            {
                PortsLabel = group.PortsLabel,
                VersionLabel = group.VersionLabel,
                ConnectorLabel = group.ConnectorLabel,
                StatusLabel = group.IsConnected ? "已连接" : "空闲",
                DeviceName = group.Device?.Name ?? "",
                IsConnected = group.IsConnected,
                IsInternal = group.IsInternal,
                VersionKey = group.VersionKey,
                LaneLabel = group.LaneLabel
            }).ToList()
        }).ToList();
    }

    private void BuildDevices(HardwareSnapshot snapshot)
    {
        _allDevices = snapshot.Devices
            .OrderBy(d => d.IsInternal)
            .ThenBy(d => d.PortNumber)
            .Select(device => new DeviceItem
            {
                Name = device.Name,
                ManufacturerLabel = string.IsNullOrWhiteSpace(device.Manufacturer) ? "厂商未提供" : device.Manufacturer,
                TypeLabel = device.DeviceTypeLabel,
                VersionLabel = device.VersionLabel,
                SpeedLabel = device.SpeedLabel,
                VersionKey = device.VersionKey == "usb3"
                    ? "usb3"
                    : device.VersionLabel.Contains("USB 1", StringComparison.OrdinalIgnoreCase) ? "usb1" : "usb2",
                PortLine = $"{device.ControllerName} · {device.PortLabel}",
                VidPid = device.VidPid,
                IsInternal = device.IsInternal,
                IsHub = device.IsHub,
                IconGlyph = IconFor(device)
            })
            .ToList();

        FilterDevices(notify: false);
    }

    public void ApplyDeviceFilter(bool showInternalDevices)
    {
        ShowInternalDevices = showInternalDevices;
        FilterDevices(notify: true);
    }

    private void FilterDevices(bool notify)
    {
        Devices = ShowInternalDevices ? _allDevices : _allDevices.Where(d => !d.IsInternal).ToList();
        PreviewDevices = Devices.Take(6).ToList();

        if (notify)
        {
            OnPropertyChanged(nameof(Devices));
            OnPropertyChanged(nameof(PreviewDevices));
            OnPropertyChanged(nameof(ShowInternalDevices));
            OnPropertyChanged(nameof(HiddenInternalDeviceCount));
            OnPropertyChanged(nameof(DeviceFilterLabel));
            OnPropertyChanged(nameof(ConnectedHeadline));
        }
    }

    public string BuildReportJson()
    {
        if (Snapshot is null)
            return "{}";

        return JsonSerializer.Serialize(Snapshot, new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });
    }

    private static string IconFor(ConnectedDeviceInfo device)
    {
        if (device.IsHub) return "\uE772";
        if (device.DeviceTypeLabel.Contains("无线", StringComparison.OrdinalIgnoreCase)) return "\uE702";
        if (device.DeviceTypeLabel.Contains("音频", StringComparison.OrdinalIgnoreCase)) return "\uE767";
        if (device.DeviceTypeLabel.Contains("HID", StringComparison.OrdinalIgnoreCase)) return "\uE765";
        if (device.DeviceTypeLabel.Contains("存储", StringComparison.OrdinalIgnoreCase)) return "\uE7B8";
        return "\uE88E";
    }

    private static string SpeedBadge(string key) => key switch
    {
        "usb32_20" => "20G",
        "usb32_10" => "10G",
        "usb32_5" => "5G",
        "usb3" => "5G+",
        "usb2" => "480M",
        _ => "USB"
    };
}
