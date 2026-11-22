using System.Diagnostics;
using UsbAtlas.Models;

namespace UsbAtlas.Services;

internal static class HardwareCollector
{
    public static HardwareSnapshot Collect()
    {
        var stopwatch = Stopwatch.StartNew();
        var board = BoardInfoReader.Read();
        var native = NativeUsb.Scan();
        var resolver = new UsbNameResolver();
        var snapshot = new HardwareSnapshot
        {
            Board = board,
            Profile = BoardProfileStore.Find(board)
        };

        var rootHubs = native.Hubs
            .Where(h => h.IsRootHub)
            .OrderBy(h => h.ControllerVendorId)
            .ThenBy(h => h.DevicePath, StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var rootHub in rootHubs)
        {
            var meta = PciControllerLookup.Find(rootHub.ControllerVendorId, rootHub.ControllerDeviceId);
            var controller = new ControllerInfo
            {
                Name = meta.Name,
                Vendor = meta.Vendor,
                PciAddress = meta.PciAddress,
                DeviceId = $"VEN_{rootHub.ControllerVendorId}&DEV_{rootHub.ControllerDeviceId}",
                RawDescription = meta.RawDescription,
                RootHubInstance = rootHub.InstanceId,
                RootHubPath = rootHub.DevicePath,
                RootHubPortCount = rootHub.PortCount,
                Usb2LaneCount = rootHub.Ports.Count(p => p.HasUsb2),
                Usb3LaneCount = rootHub.Ports.Count(p => p.HasUsb3),
                AccentKey = meta.AccentKey,
                Initials = meta.Initials,
                SpeedHint = meta.SpeedHint
            };

            controller.Groups = BuildGroups(rootHub, controller, resolver);
            snapshot.Controllers.Add(controller);
        }

        snapshot.Controllers = snapshot.Controllers
            .OrderBy(c => c.PciAddress, StringComparer.OrdinalIgnoreCase)
            .ThenBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        snapshot.Groups = snapshot.Controllers.SelectMany(c => c.Groups).ToList();
        snapshot.Devices = snapshot.Groups
            .Where(g => g.IsConnected && g.Device is not null)
            .Select(g => g.Device!)
            .OrderBy(d => d.ControllerName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(d => d.PortNumber)
            .ToList();

        var warnings = new List<string>(native.Warnings);
        if (snapshot.Controllers.Count == 0)
            warnings.Add("未发现 USB 根集线器。请确认系统 USB 驱动正常，或改用普通用户桌面重新运行。");
        foreach (var controller in snapshot.Controllers.Where(c => c.RootHubPortCount == 0))
            warnings.Add($"{controller.Name} 未能读取端口信息。");

        snapshot.ScanWarning = warnings.Count == 0 ? null : string.Join("；", warnings.Distinct());
        stopwatch.Stop();
        snapshot.ScanDurationLabel = $"{stopwatch.ElapsedMilliseconds} ms";
        snapshot.ScannedAt = DateTime.Now;
        return snapshot;
    }

    private static List<PortGroupInfo> BuildGroups(NativeHub rootHub, ControllerInfo controller, UsbNameResolver resolver)
    {
        var ports = rootHub.Ports.OrderBy(p => p.Number).ToList();
        if (ports.Count == 0)
            return [];

        int maxPort = Math.Max(rootHub.PortCount, ports.Max(p => Math.Max(p.Number, p.CompanionPort)));
        var union = new UnionFind(maxPort);
        foreach (var port in ports)
        {
            if (port.CompanionPort > 0 && port.CompanionPort <= maxPort && port.CompanionPort != port.Number)
                union.Union(port.Number, port.CompanionPort);
        }

        bool connectorPropertiesAvailable = ports.Any(p => p.HasConnectorProperties);
        var groups = new List<PortGroupInfo>();

        foreach (var members in ports.GroupBy(p => union.Find(p.Number)).OrderBy(g => g.Min(p => p.Number)))
        {
            var lanes = members.OrderBy(p => p.Number).ToList();
            var connectedLane = lanes
                .Where(p => p.IsConnected)
                .OrderByDescending(p => p.HasUsb3)
                .ThenBy(p => p.Number)
                .FirstOrDefault();

            bool hasUsb3 = lanes.Any(p => p.HasUsb3);
            bool hasUsb2 = lanes.Any(p => p.HasUsb2 || (!p.HasUsb3 && p.Protocols is null));
            bool userConnectable = connectorPropertiesAvailable
                ? lanes.Any(p => p.UserConnectable)
                : true;

            var group = new PortGroupInfo
            {
                Id = $"{rootHub.InstanceId}:{string.Join("-", lanes.Select(p => p.Number))}",
                Ports = lanes.Select(p => p.Number).ToList(),
                HasUsb3 = hasUsb3,
                HasUsb2 = hasUsb2,
                HasUsb110 = lanes.Any(p => p.HasUsb110),
                IsUserConnectable = userConnectable,
                IsTypeC = lanes.Any(p => p.IsTypeC),
                IsDebugCapable = lanes.Any(p => p.IsDebugCapable),
                IsConnected = connectedLane is not null,
                ControllerName = controller.Name,
                ControllerVendor = controller.Vendor,
                ControllerAccentKey = controller.AccentKey
            };

            if (connectedLane is not null)
                group.Device = BuildDevice(connectedLane, group, controller, resolver);

            groups.Add(group);
        }

        return groups;
    }

    private static ConnectedDeviceInfo BuildDevice(NativePort port, PortGroupInfo group, ControllerInfo controller, UsbNameResolver resolver)
    {
        var registry = resolver.Resolve(port.Vid, port.Pid, port.Address);
        bool isHub = port.IsHub || port.DeviceClass == 9;
        string product = BoardInfoReader.Clean(port.Product) ?? "";
        string registryName = registry?.Name ?? "";

        bool genericName = string.IsNullOrWhiteSpace(product)
                           || product.Contains("Composite Device", StringComparison.OrdinalIgnoreCase)
                           || product.Contains("无线设备", StringComparison.OrdinalIgnoreCase)
                           || product.Contains("Wireless Device", StringComparison.OrdinalIgnoreCase)
                           || product.Contains("USB 设备", StringComparison.OrdinalIgnoreCase)
                           || product.Contains("复合设备", StringComparison.OrdinalIgnoreCase)
                           || product.Length <= 2;

        if (genericName && !string.IsNullOrWhiteSpace(registryName))
            product = registryName;

        if (string.IsNullOrWhiteSpace(product))
            product = isHub ? "USB 集线器" : ClassName(port.DeviceClass);

        if (isHub
            && !product.Contains("Hub", StringComparison.OrdinalIgnoreCase)
            && !product.Contains("集线器", StringComparison.OrdinalIgnoreCase))
            product = $"{product} 集线器";

        string manufacturer = BoardInfoReader.Clean(port.Manufacturer) ?? "";
        if (string.IsNullOrWhiteSpace(manufacturer))
            manufacturer = registry?.Manufacturer ?? "";
        if (string.IsNullOrWhiteSpace(manufacturer))
            manufacturer = VendorNameForVid(port.Vid);

        bool superSpeedPlus = port.IsOperatingSuperSpeedPlus || port.Speed >= 4;
        bool superSpeed = port.IsOperatingSuperSpeed || port.Speed >= 3 || superSpeedPlus;

        string versionKey = superSpeed ? "usb3" : "usb2";
        string versionLabel = superSpeedPlus
            ? "USB 3.2 Gen 2+"
            : superSpeed
                ? "USB 3.2 Gen 1+"
                : port.Speed switch
                {
                    2 => "USB 2.0",
                    1 => "USB 1.1",
                    _ => "USB 1.0"
                };

        string speedLabel = superSpeedPlus
            ? "10–20 Gbps"
            : superSpeed
                ? "5 Gbps+"
                : port.Speed switch
                {
                    2 => "480 Mbps",
                    1 => "12 Mbps",
                    _ => "1.5 Mbps"
                };

        string classLabel = isHub ? "USB 集线器" : ClassName(port.DeviceClass);
        if (!isHub && port.DeviceClass == 0 && registry?.Service.Equals("usbccgp", StringComparison.OrdinalIgnoreCase) == true)
            classLabel = "复合设备";

        return new ConnectedDeviceInfo
        {
            Name = product,
            Manufacturer = manufacturer,
            ClassLabel = classLabel,
            ControllerName = controller.Name,
            PortNumber = port.Number,
            PortLabel = $"端口 {port.Number:D2}",
            SpeedLabel = speedLabel,
            VersionKey = versionKey,
            VersionLabel = versionLabel,
            VidPid = $"{port.Vid:X4}:{port.Pid:X4}",
            Address = port.Address,
            IsHub = isHub,
            IsInternal = !group.IsUserConnectable
        };
    }

    private static string VendorNameForVid(int vid) => vid switch
    {
        0x8087 => "Intel",
        0x0B05 => "ASUS",
        0x174C => "ASMedia",
        _ => ""
    };

    private static string ClassName(int deviceClass) => deviceClass switch
    {
        0 => "复合设备",
        1 => "音频设备",
        2 => "通信设备",
        3 => "HID 设备",
        5 => "物理设备",
        6 => "图像设备",
        7 => "打印机",
        8 => "大容量存储",
        9 => "USB 集线器",
        10 => "CDC 数据设备",
        11 => "智能卡",
        13 => "内容安全设备",
        14 => "视频设备",
        15 => "健康设备",
        16 => "音视频设备",
        17 => "医疗设备",
        0xDC => "诊断设备",
        0xE0 => "无线设备",
        0xEF => "复合设备",
        0xFE => "应用专用设备",
        _ => "USB 设备"
    };

    private sealed class UnionFind
    {
        private readonly int[] _parent;

        public UnionFind(int size)
        {
            _parent = Enumerable.Range(0, size + 1).ToArray();
        }

        public int Find(int value)
        {
            while (_parent[value] != value)
            {
                _parent[value] = _parent[_parent[value]];
                value = _parent[value];
            }
            return value;
        }

        public void Union(int left, int right)
        {
            int a = Find(left);
            int b = Find(right);
            if (a != b)
                _parent[b] = a;
        }
    }
}
