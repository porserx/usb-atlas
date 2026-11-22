using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using UsbAtlas.Models;

namespace UsbAtlas.Services;

internal static class BoardInfoReader
{
    public static BoardInfo Read()
    {
        var board = new BoardInfo();
        using (var bios = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\BIOS"))
        {
            if (bios is not null)
            {
                board.Manufacturer = Clean(bios.GetValue("BaseBoardManufacturer") as string)
                                     ?? Clean(bios.GetValue("SystemManufacturer") as string)
                                     ?? "未知主板厂商";
                board.Product = Clean(bios.GetValue("BaseBoardProduct") as string) ?? "未知型号";
                board.Version = Clean(bios.GetValue("BaseBoardVersion") as string) ?? "";
                board.BiosVendor = Clean(bios.GetValue("BIOSVendor") as string) ?? "";
                board.BiosVersion = Clean(bios.GetValue("BIOSVersion") as string) ?? "";
                board.BiosDate = Clean(bios.GetValue("BIOSReleaseDate") as string) ?? "";
            }
        }

        using (var cpu = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\CentralProcessor\0"))
        {
            board.CpuName = Clean(cpu?.GetValue("ProcessorNameString") as string) ?? "";
        }

        board.OsName = ReadOsName();
        return board;
    }

    private static string ReadOsName()
    {
        string productName = "Windows";
        string displayVersion = "";
        string build = Environment.OSVersion.Version.Build.ToString(CultureInfo.InvariantCulture);
        string ubr = "";

        using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
        if (key is not null)
        {
            productName = Clean(key.GetValue("ProductName") as string) ?? productName;
            displayVersion = Clean(key.GetValue("DisplayVersion") as string) ?? "";
            build = Clean(key.GetValue("CurrentBuildNumber") as string) ?? build;
            ubr = key.GetValue("UBR")?.ToString() ?? "";
        }

        if (int.TryParse(build, out int buildNumber) && buildNumber >= 22000)
            productName = productName.Replace("Windows 10", "Windows 11", StringComparison.OrdinalIgnoreCase);

        var parts = new List<string> { productName };
        if (!string.IsNullOrWhiteSpace(displayVersion))
            parts.Add(displayVersion);
        parts.Add(string.IsNullOrWhiteSpace(ubr) ? $"Build {build}" : $"Build {build}.{ubr}");
        return string.Join(" · ", parts);
    }

    internal static string? Clean(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        string result = value.Trim();
        if (result.StartsWith('@'))
        {
            int semicolon = result.LastIndexOf(';');
            if (semicolon >= 0 && semicolon + 1 < result.Length)
                result = result[(semicolon + 1)..].Trim();
        }

        return string.IsNullOrWhiteSpace(result) ? null : result;
    }
}

internal sealed class UsbRegistryEntry
{
    public int Vid { get; init; }
    public int Pid { get; init; }
    public int Address { get; init; }
    public string Name { get; init; } = "";
    public string Manufacturer { get; init; } = "";
    public string Service { get; init; } = "";
    public string Location { get; init; } = "";
}

internal sealed class UsbNameResolver
{
    private static readonly Regex VidPidRegex = new(@"VID_([0-9A-Fa-f]{4})&PID_([0-9A-Fa-f]{4})", RegexOptions.Compiled);
    private static readonly Regex PortRegex = new(@"Port_#(\d+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private readonly List<UsbRegistryEntry> _entries = [];

    public UsbNameResolver()
    {
        using var usb = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Enum\USB");
        if (usb is null)
            return;

        foreach (string deviceKeyName in usb.GetSubKeyNames())
        {
            if (deviceKeyName.StartsWith("ROOT_HUB", StringComparison.OrdinalIgnoreCase))
                continue;

            Match idMatch = VidPidRegex.Match(deviceKeyName);
            if (!idMatch.Success)
                continue;

            int vid = int.Parse(idMatch.Groups[1].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            int pid = int.Parse(idMatch.Groups[2].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture);

            using var deviceKey = usb.OpenSubKey(deviceKeyName);
            if (deviceKey is null)
                continue;

            foreach (string instanceName in deviceKey.GetSubKeyNames())
            {
                using var instance = deviceKey.OpenSubKey(instanceName);
                if (instance is null)
                    continue;

                int address = ReadAddress(instance.GetValue("Address"));
                string location = BoardInfoReader.Clean(instance.GetValue("LocationInformation") as string) ?? "";
                if (address <= 0)
                {
                    Match portMatch = PortRegex.Match(location);
                    if (portMatch.Success && int.TryParse(portMatch.Groups[1].Value, out int port))
                        address = port;
                }

                string name = ReadName(instance);
                if (string.IsNullOrWhiteSpace(name))
                    continue;

                _entries.Add(new UsbRegistryEntry
                {
                    Vid = vid,
                    Pid = pid,
                    Address = address,
                    Name = name,
                    Manufacturer = BoardInfoReader.Clean(instance.GetValue("Manufacturer") as string) ?? "",
                    Service = BoardInfoReader.Clean(instance.GetValue("Service") as string) ?? "",
                    Location = location
                });
            }
        }
    }

    public UsbRegistryEntry? Resolve(int vid, int pid, int address)
    {
        var candidates = _entries.Where(e => e.Vid == vid && e.Pid == pid).ToList();
        if (candidates.Count == 0)
            return null;

        return candidates.FirstOrDefault(e => e.Address == address) ?? candidates[0];
    }

    private static int ReadAddress(object? value)
    {
        if (value is int number)
            return number;
        if (value is string text && int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed))
            return parsed;
        return 0;
    }

    private static string ReadName(RegistryKey instance)
    {
        string? friendly = instance.GetValue("FriendlyName") as string;
        string? description = instance.GetValue("DeviceDesc") as string;
        string cleanedDescription = BoardInfoReader.Clean(description) ?? "";

        if (!string.IsNullOrWhiteSpace(friendly) && !friendly.TrimStart().StartsWith('@'))
        {
            string cleaned = BoardInfoReader.Clean(friendly) ?? "";
            if (!string.IsNullOrWhiteSpace(cleaned) && !cleaned.StartsWith('(') && !IsGenericName(cleaned))
                return cleaned;
        }

        if (!string.IsNullOrWhiteSpace(cleanedDescription))
            return cleanedDescription;

        if (!string.IsNullOrWhiteSpace(friendly))
        {
            string cleaned = BoardInfoReader.Clean(friendly) ?? "";
            if (!string.IsNullOrWhiteSpace(cleaned) && !cleaned.StartsWith('('))
                return cleaned;
        }

        return "";
    }

    private static bool IsGenericName(string name)
        => name.Contains("无线设备", StringComparison.OrdinalIgnoreCase)
           || name.Contains("Wireless Device", StringComparison.OrdinalIgnoreCase)
           || name.Contains("USB 设备", StringComparison.OrdinalIgnoreCase)
           || name.Contains("Composite Device", StringComparison.OrdinalIgnoreCase)
           || name.Contains("复合设备", StringComparison.OrdinalIgnoreCase);
}

internal sealed class PciControllerMeta
{
    public string Vendor { get; init; } = "USB";
    public string Name { get; init; } = "USB xHCI 主机控制器";
    public string SpeedHint { get; init; } = "USB 3.x xHCI";
    public string AccentKey { get; init; } = "blue";
    public string Initials { get; init; } = "USB";
    public string PciAddress { get; init; } = "";
    public string RawDescription { get; init; } = "";
}

internal static class PciControllerLookup
{
    private static readonly Regex LocationRegex = new(@"\((\d+)\s*,\s*(\d+)\s*,\s*(\d+)\)", RegexOptions.Compiled);
    private static readonly Regex InstanceRegex = new(@"[&](\d+)$", RegexOptions.Compiled);

    private static readonly Dictionary<string, (string Vendor, string Name, string Speed, string Accent, string Initials)> Known = new(StringComparer.OrdinalIgnoreCase)
    {
        ["8086:7A60"] = ("Intel", "Intel Alder Lake PCH xHCI", "USB 3.2 xHCI · 20 Gbps 端口", "cyan", "IN"),
        ["1B21:2142"] = ("ASMedia", "ASMedia ASM2142", "USB 3.2 Gen 2 · 10 Gbps", "violet", "AS"),
        ["8086:51ED"] = ("Intel", "Intel Alder Lake PCH xHCI", "USB 3.2 xHCI · 20 Gbps 端口", "cyan", "IN"),
        ["8086:7AE0"] = ("Intel", "Intel Raptor Lake PCH xHCI", "USB 3.2 xHCI · 20 Gbps 端口", "cyan", "IN"),
        ["1022:43D5"] = ("AMD", "AMD Promontory USB 3.1 xHCI", "USB 3.2 Gen 2", "rose", "AMD"),
        ["1022:15E9"] = ("AMD", "AMD 600 Series USB 3.2 xHCI", "USB 3.2 xHCI", "rose", "AMD")
    };

    private static readonly Dictionary<string, string> VendorNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["8086"] = "Intel",
        ["1B21"] = "ASMedia",
        ["1022"] = "AMD",
        ["10DE"] = "NVIDIA",
        ["1912"] = "Renesas",
        ["1033"] = "NEC",
        ["1D6B"] = "Linux Foundation"
    };

    public static PciControllerMeta Find(string vendorId, string deviceId)
    {
        string key = $"{vendorId}:{deviceId}";
        Known.TryGetValue(key, out var known);

        string vendor = known.Vendor
                        ?? (VendorNames.TryGetValue(vendorId, out string? v) ? v : $"0x{vendorId}");
        string accent = known.Accent ?? AccentForVendor(vendorId);
        string initials = known.Initials ?? InitialsFor(vendor);
        string name = known.Name ?? $"{vendor} USB 3.x 主机控制器";
        string speed = known.Speed ?? "USB 3.x xHCI";
        string pciAddress = "";
        string rawDescription = "";

        using var pci = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Enum\PCI");
        if (pci is not null)
        {
            foreach (string deviceKeyName in pci.GetSubKeyNames())
            {
                if (!deviceKeyName.StartsWith($"VEN_{vendorId}&DEV_{deviceId}", StringComparison.OrdinalIgnoreCase))
                    continue;

                using var deviceKey = pci.OpenSubKey(deviceKeyName);
                if (deviceKey is null)
                    continue;

                foreach (string instanceName in deviceKey.GetSubKeyNames())
                {
                    using var instance = deviceKey.OpenSubKey(instanceName);
                    if (instance is null)
                        continue;

                    string service = instance.GetValue("Service") as string ?? "";
                    if (!service.Contains("USB", StringComparison.OrdinalIgnoreCase)
                        && !service.Contains("XHCI", StringComparison.OrdinalIgnoreCase))
                        continue;

                    rawDescription = BoardInfoReader.Clean(instance.GetValue("DeviceDesc") as string) ?? rawDescription;
                    string location = instance.GetValue("LocationInformation") as string ?? "";
                    Match locationMatch = LocationRegex.Match(location);
                    if (locationMatch.Success)
                    {
                        int bus = int.Parse(locationMatch.Groups[1].Value, CultureInfo.InvariantCulture);
                        int device = int.Parse(locationMatch.Groups[2].Value, CultureInfo.InvariantCulture);
                        int function = int.Parse(locationMatch.Groups[3].Value, CultureInfo.InvariantCulture);
                        pciAddress = $"{bus:X2}:{device:X2}.{function:X}";
                    }
                    else
                    {
                        Match instanceMatch = InstanceRegex.Match(instanceName);
                        if (instanceMatch.Success)
                            pciAddress = $"??:??.{instanceMatch.Groups[1].Value}";
                    }

                    break;
                }

                if (!string.IsNullOrEmpty(rawDescription))
                    break;
            }
        }

        return new PciControllerMeta
        {
            Vendor = vendor,
            Name = name,
            SpeedHint = speed,
            AccentKey = accent,
            Initials = initials,
            PciAddress = pciAddress,
            RawDescription = rawDescription
        };
    }

    private static string AccentForVendor(string vendorId) => vendorId.ToUpperInvariant() switch
    {
        "8086" => "cyan",
        "1B21" => "violet",
        "1022" => "rose",
        _ => "blue"
    };

    private static string InitialsFor(string vendor)
    {
        if (vendor.Contains("Intel", StringComparison.OrdinalIgnoreCase)) return "IN";
        if (vendor.Contains("ASMedia", StringComparison.OrdinalIgnoreCase)) return "AS";
        if (vendor.Contains("AMD", StringComparison.OrdinalIgnoreCase)) return "AMD";
        return "USB";
    }
}
