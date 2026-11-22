using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace UsbAtlas.Services;

internal sealed class NativeScanResult
{
    public List<NativeHub> Hubs { get; } = [];
    public List<string> Warnings { get; } = [];
}

internal sealed class NativeHub
{
    public string DevicePath { get; set; } = "";
    public string RegistryKey { get; set; } = "";
    public string InstanceId { get; set; } = "";
    public bool IsRootHub { get; set; }
    public int HubType { get; set; }
    public int PortCount { get; set; }
    public string HardwareId { get; set; } = "";
    public string ParentIdPrefix { get; set; } = "";
    public string ControllerVendorId { get; set; } = "";
    public string ControllerDeviceId { get; set; } = "";
    public string Error { get; set; } = "";
    public List<NativePort> Ports { get; } = [];

    public bool HasUsb3Ports => Ports.Any(p => p.HasUsb3);
}

internal sealed class NativePort
{
    public int Number { get; set; }
    public uint? Protocols { get; set; }
    public bool UserConnectable { get; set; }
    public bool IsTypeC { get; set; }
    public bool IsDebugCapable { get; set; }
    public bool HasMultipleCompanions { get; set; }
    public bool HasConnectorProperties { get; set; }
    public int CompanionPort { get; set; }
    public uint V2Flags { get; set; }
    public int ConnectionStatus { get; set; }
    public int Speed { get; set; }
    public bool IsHub { get; set; }
    public int Address { get; set; }
    public int Vid { get; set; }
    public int Pid { get; set; }
    public int BcdUsb { get; set; }
    public int DeviceClass { get; set; }
    public string? Product { get; set; }
    public string? Manufacturer { get; set; }
    public string? Error { get; set; }

    public bool IsConnected => ConnectionStatus == 1;
    public bool HasUsb3 => Protocols is { } p && (p & 4) != 0;
    public bool HasUsb2 => Protocols is { } p && (p & 2) != 0;
    public bool HasUsb110 => Protocols is { } p && (p & 1) != 0;
    public bool IsOperatingSuperSpeed => (V2Flags & 1) != 0;
    public bool IsOperatingSuperSpeedPlus => (V2Flags & 4) != 0;

    public string PortLabel => $"端口 {Number:D2}";
    public string VidPid => $"{Vid:X4}:{Pid:X4}";
}

internal sealed class NativeConnectorProps
{
    public int CompanionPort { get; set; }
    public bool UserConnectable { get; set; }
    public bool IsTypeC { get; set; }
    public bool IsDebugCapable { get; set; }
    public bool HasMultipleCompanions { get; set; }
}

internal sealed class NativeConnectionInfo
{
    public int Status { get; set; }
    public int Speed { get; set; }
    public bool IsHub { get; set; }
    public int Address { get; set; }
    public int Vid { get; set; }
    public int Pid { get; set; }
    public int BcdUsb { get; set; }
    public int DeviceClass { get; set; }
    public int IProduct { get; set; }
    public int IManufacturer { get; set; }
}

internal static class NativeUsb
{
    private const int DIGCF_PRESENT = 0x02;
    private const int DIGCF_DEVICEINTERFACE = 0x10;
    private const uint GENERIC_WRITE = 0x40000000;
    private const uint GENERIC_READ = 0x80000000;
    private const uint FILE_SHARE_READ = 1;
    private const uint FILE_SHARE_WRITE = 2;
    private const uint OPEN_EXISTING = 3;
    private const uint IOCTL_USB_GET_NODE_INFORMATION = 0x00220408;
    private const uint IOCTL_USB_GET_DESCRIPTOR_FROM_NODE_CONNECTION = 0x00220410;
    private const uint IOCTL_USB_GET_NODE_CONNECTION_INFORMATION_EX = 0x00220448;
    private const uint IOCTL_USB_GET_HUB_INFORMATION_EX = 0x00220454;
    private const uint IOCTL_USB_GET_PORT_CONNECTOR_PROPERTIES = 0x00220458;
    private const uint IOCTL_USB_GET_NODE_CONNECTION_INFORMATION_EX_V2 = 0x0022045C;
    private const int MaxPorts = 64;

    private static readonly Guid GUID_DEVINTERFACE_USB_HUB = new("f18a0e88-c30c-11d0-8815-00a0c906bed8");
    private static readonly Regex VidPidRegex = new(@"VID([0-9A-Fa-f]{4})&PID([0-9A-Fa-f]{4})", RegexOptions.Compiled);
    private static readonly Regex PathVidPidRegex = new(@"vid_([0-9A-Fa-f]{4})&pid_([0-9A-Fa-f]{4})", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    [StructLayout(LayoutKind.Sequential)]
    private struct SP_DEVICE_INTERFACE_DATA
    {
        public int CbSize;
        public Guid InterfaceClassGuid;
        public int Flags;
        public IntPtr Reserved;
    }

    [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr SetupDiGetClassDevs(ref Guid classGuid, IntPtr enumerator, IntPtr hwndParent, int flags);

    [DllImport("setupapi.dll", SetLastError = true)]
    private static extern bool SetupDiEnumDeviceInterfaces(IntPtr deviceInfoSet, IntPtr deviceInfoData, ref Guid interfaceClassGuid, int memberIndex, ref SP_DEVICE_INTERFACE_DATA deviceInterfaceData);

    [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool SetupDiGetDeviceInterfaceDetail(IntPtr deviceInfoSet, ref SP_DEVICE_INTERFACE_DATA deviceInterfaceData, IntPtr deviceInterfaceDetailData, int deviceInterfaceDetailDataSize, out int requiredSize, IntPtr deviceInfoData);

    [DllImport("setupapi.dll", SetLastError = true)]
    private static extern bool SetupDiDestroyDeviceInfoList(IntPtr deviceInfoSet);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr CreateFile(string fileName, uint desiredAccess, uint shareMode, IntPtr securityAttributes, uint creationDisposition, uint flagsAndAttributes, IntPtr templateFile);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr handle);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool DeviceIoControl(IntPtr device, uint ioControlCode, IntPtr inBuffer, int inBufferSize, IntPtr outBuffer, int outBufferSize, out int bytesReturned, IntPtr overlapped);

    public static NativeScanResult Scan()
    {
        var result = new NativeScanResult();
        foreach (var path in EnumerateHubPaths(result.Warnings))
        {
            var hub = ProbeHub(path, result.Warnings);
            if (hub is not null)
                result.Hubs.Add(hub);
        }

        return result;
    }

    private static List<string> EnumerateHubPaths(List<string> warnings)
    {
        var paths = new List<string>();
        Guid hubGuid = GUID_DEVINTERFACE_USB_HUB;
        IntPtr set = SetupDiGetClassDevs(ref hubGuid, IntPtr.Zero, IntPtr.Zero, DIGCF_PRESENT | DIGCF_DEVICEINTERFACE);
        if (set == new IntPtr(-1))
        {
            warnings.Add($"无法枚举 USB 集线器（SetupAPI {Marshal.GetLastWin32Error()}）");
            return paths;
        }

        try
        {
            var did = new SP_DEVICE_INTERFACE_DATA { CbSize = Marshal.SizeOf<SP_DEVICE_INTERFACE_DATA>() };
            for (int i = 0; ; i++)
            {
                Guid ifaceGuid = GUID_DEVINTERFACE_USB_HUB;
                if (!SetupDiEnumDeviceInterfaces(set, IntPtr.Zero, ref ifaceGuid, i, ref did))
                    break;

                if (!SetupDiGetDeviceInterfaceDetail(set, ref did, IntPtr.Zero, 0, out int required, IntPtr.Zero)
                    && Marshal.GetLastWin32Error() != 122)
                    continue;
                if (required <= 0)
                    continue;

                IntPtr buffer = Marshal.AllocHGlobal(required);
                try
                {
                    Marshal.WriteInt32(buffer, IntPtr.Size == 8 ? 8 : 6);
                    if (!SetupDiGetDeviceInterfaceDetail(set, ref did, buffer, required, out required, IntPtr.Zero))
                        continue;

                    string? path = Marshal.PtrToStringUni(IntPtr.Add(buffer, 4));
                    if (!string.IsNullOrWhiteSpace(path) && !paths.Contains(path, StringComparer.OrdinalIgnoreCase))
                        paths.Add(path);
                }
                finally
                {
                    Marshal.FreeHGlobal(buffer);
                }
            }
        }
        finally
        {
            SetupDiDestroyDeviceInfoList(set);
        }

        return paths;
    }

    private static NativeHub? ProbeHub(string path, List<string> warnings)
    {
        var hub = new NativeHub { DevicePath = path };
        PopulateIdentity(hub);

        IntPtr handle = OpenHub(path);
        if (handle == new IntPtr(-1))
        {
            hub.Error = $"无法打开集线器（错误 {Marshal.GetLastWin32Error()}）";
            if (hub.IsRootHub)
                warnings.Add($"{hub.Error}：{hub.InstanceId}");
            return hub;
        }

        try
        {
            IntPtr buffer = Marshal.AllocHGlobal(4096);
            try
            {
                int nodePorts = 0;
                if (DeviceIoControl(handle, IOCTL_USB_GET_NODE_INFORMATION, IntPtr.Zero, 0, buffer, 4096, out _, IntPtr.Zero))
                    nodePorts = Marshal.ReadByte(buffer, 6);
                else
                    hub.Error = $"读取集线器信息失败（错误 {Marshal.GetLastWin32Error()}）";

                int hubType = 0;
                int highestPort = 0;
                Zero(buffer, 64);
                if (DeviceIoControl(handle, IOCTL_USB_GET_HUB_INFORMATION_EX, IntPtr.Zero, 0, buffer, 4096, out _, IntPtr.Zero))
                {
                    hubType = Marshal.ReadInt32(buffer, 0);
                    highestPort = Marshal.ReadInt16(buffer, 4);
                }

                hub.HubType = hubType;
                hub.PortCount = Math.Clamp(Math.Max(nodePorts, highestPort), 0, MaxPorts);
                if (hub.PortCount == 0)
                {
                    hub.Error = string.IsNullOrEmpty(hub.Error) ? "未发现可用端口" : hub.Error;
                    return hub;
                }

                for (int portNumber = 1; portNumber <= hub.PortCount; portNumber++)
                {
                    var port = ProbePort(handle, portNumber);
                    if (port is not null)
                        hub.Ports.Add(port);
                }
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }
        finally
        {
            CloseHandle(handle);
        }

        return hub;
    }

    private static void PopulateIdentity(NativeHub hub)
    {
        string[] parts = hub.DevicePath.Replace(@"\\?\", "", StringComparison.Ordinal).Split('#');
        if (parts.Length >= 3)
        {
            hub.RegistryKey = parts[1].ToUpperInvariant();
            hub.InstanceId = parts[2];
        }

        hub.IsRootHub = hub.RegistryKey.StartsWith("ROOT_HUB", StringComparison.OrdinalIgnoreCase);

        if (!string.IsNullOrEmpty(hub.RegistryKey) && !string.IsNullOrEmpty(hub.InstanceId))
        {
            using var key = Registry.LocalMachine.OpenSubKey($@"SYSTEM\CurrentControlSet\Enum\USB\{hub.RegistryKey}\{hub.InstanceId}");
            if (key is not null)
            {
                hub.HardwareId = ReadFirstString(key.GetValue("HardwareID"));
                hub.ParentIdPrefix = key.GetValue("ParentIdPrefix") as string ?? "";
            }
        }

        string vidPidSource = hub.HardwareId;
        if (string.IsNullOrWhiteSpace(vidPidSource))
            vidPidSource = hub.DevicePath;

        Match match = VidPidRegex.Match(vidPidSource);
        if (!match.Success)
            match = PathVidPidRegex.Match(vidPidSource);

        if (match.Success)
        {
            hub.ControllerVendorId = match.Groups[1].Value.ToUpperInvariant();
            hub.ControllerDeviceId = match.Groups[2].Value.ToUpperInvariant();
        }

        if (hub.IsRootHub && string.IsNullOrEmpty(hub.RegistryKey))
            hub.RegistryKey = "ROOT_HUB30";
    }

    private static NativePort? ProbePort(IntPtr hubHandle, int portNumber)
    {
        var port = new NativePort { Number = portNumber };
        IntPtr buffer = Marshal.AllocHGlobal(4096);
        try
        {
            // USB 3.x protocol capability (input must advertise understood protocols).
            Zero(buffer, 32);
            Marshal.WriteInt32(buffer, 0, portNumber);
            Marshal.WriteInt32(buffer, 4, 16);
            Marshal.WriteInt32(buffer, 8, 7);
            if (DeviceIoControl(hubHandle, IOCTL_USB_GET_NODE_CONNECTION_INFORMATION_EX_V2, buffer, 16, buffer, 32, out _, IntPtr.Zero))
            {
                port.Protocols = (uint)Marshal.ReadInt32(buffer, 8);
                port.V2Flags = (uint)Marshal.ReadInt32(buffer, 12);
            }

            // Physical connector properties. Packed struct is 18 bytes.
            Zero(buffer, 18);
            Marshal.WriteInt32(buffer, 0, portNumber);
            if (DeviceIoControl(hubHandle, IOCTL_USB_GET_PORT_CONNECTOR_PROPERTIES, buffer, 18, buffer, 2048, out _, IntPtr.Zero))
            {
                port.HasConnectorProperties = true;
                uint properties = (uint)Marshal.ReadInt32(buffer, 8);
                port.UserConnectable = (properties & 0x1) != 0;
                port.IsDebugCapable = (properties & 0x2) != 0;
                port.HasMultipleCompanions = (properties & 0x4) != 0;
                port.IsTypeC = (properties & 0x8) != 0;
                port.CompanionPort = Marshal.ReadInt16(buffer, 14);
            }

            // Live connection state and device descriptor. The IOCTL structures are packed (1).
            Zero(buffer, 64);
            Marshal.WriteInt32(buffer, 0, portNumber);
            if (!DeviceIoControl(hubHandle, IOCTL_USB_GET_NODE_CONNECTION_INFORMATION_EX, buffer, 35, buffer, 2048, out _, IntPtr.Zero))
            {
                port.Error = $"读取端口状态失败（{Marshal.GetLastWin32Error()}）";
                return port;
            }

            port.ConnectionStatus = Marshal.ReadInt32(buffer, 31);
            port.Speed = Marshal.ReadByte(buffer, 23);
            port.IsHub = Marshal.ReadByte(buffer, 24) != 0;
            port.Address = Marshal.ReadInt16(buffer, 25);
            port.BcdUsb = Marshal.ReadInt16(buffer, 6);
            port.DeviceClass = Marshal.ReadByte(buffer, 8);
            port.Vid = (ushort)Marshal.ReadInt16(buffer, 12);
            port.Pid = (ushort)Marshal.ReadInt16(buffer, 14);
            int iProduct = Marshal.ReadByte(buffer, 19);
            int iManufacturer = Marshal.ReadByte(buffer, 18);

            if (port.IsConnected)
            {
                port.Product = GetStringDescriptor(hubHandle, portNumber, iProduct);
                port.Manufacturer = GetStringDescriptor(hubHandle, portNumber, iManufacturer);
            }

            return port;
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private static string? GetStringDescriptor(IntPtr hubHandle, int portNumber, int index)
    {
        if (index <= 0)
            return null;

        foreach (int languageId in new[] { 0x0409, 0x0804 })
        {
            IntPtr buffer = Marshal.AllocHGlobal(12 + 255);
            try
            {
                Zero(buffer, 12 + 255);
                Marshal.WriteInt32(buffer, 0, portNumber);
                Marshal.WriteByte(buffer, 4, 0x80);
                Marshal.WriteByte(buffer, 5, 0x06);
                Marshal.WriteInt16(buffer, 6, (short)((3 << 8) | (index & 0xFF)));
                Marshal.WriteInt16(buffer, 8, (short)languageId);
                Marshal.WriteInt16(buffer, 10, 255);

                if (!DeviceIoControl(hubHandle, IOCTL_USB_GET_DESCRIPTOR_FROM_NODE_CONNECTION, buffer, 12, buffer, 12 + 255, out _, IntPtr.Zero))
                    continue;

                int length = Marshal.ReadByte(buffer, 12);
                if (length < 2 || Marshal.ReadByte(buffer, 13) != 3)
                    continue;

                string value = Marshal.PtrToStringUni(IntPtr.Add(buffer, 14), (length - 2) / 2)?.TrimEnd('\0').Trim() ?? "";
                if (!string.IsNullOrWhiteSpace(value))
                    return value;
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        return null;
    }

    private static IntPtr OpenHub(string path)
    {
        IntPtr handle = CreateFile(path, GENERIC_WRITE, FILE_SHARE_READ | FILE_SHARE_WRITE, IntPtr.Zero, OPEN_EXISTING, 0, IntPtr.Zero);
        if (handle != new IntPtr(-1))
            return handle;

        handle = CreateFile(path, GENERIC_READ, FILE_SHARE_READ | FILE_SHARE_WRITE, IntPtr.Zero, OPEN_EXISTING, 0, IntPtr.Zero);
        if (handle != new IntPtr(-1))
            return handle;

        return CreateFile(path, 0, FILE_SHARE_READ | FILE_SHARE_WRITE, IntPtr.Zero, OPEN_EXISTING, 0, IntPtr.Zero);
    }

    private static string ReadFirstString(object? value) => value switch
    {
        string[] values when values.Length > 0 => values[0],
        string text => text,
        _ => ""
    };

    private static void Zero(IntPtr buffer, int count)
    {
        for (int i = 0; i < count; i++)
            Marshal.WriteByte(buffer, i, 0);
    }
}
