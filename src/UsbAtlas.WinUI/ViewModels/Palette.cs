using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace UsbAtlas.WinUI.ViewModels;

public static class Palette
{
    private static readonly Dictionary<string, (Color Color, SolidColorBrush Brush, SolidColorBrush Soft)> Colors = new(StringComparer.OrdinalIgnoreCase)
    {
        ["usb32_20"] = Build("DB2777", "2ADB2777"),
        ["usb32_10"] = Build("7C3AED", "2A7C3AED"),
        ["usb32_5"] = Build("0891B2", "2A0891B2"),
        ["usb2"] = Build("64748B", "2664748B"),
        ["usb3"] = Build("0891B2", "2A0891B2"),
        ["usb1"] = Build("B45309", "2AB45309"),
        ["cyan"] = Build("0891B2", "2A0891B2"),
        ["violet"] = Build("7C3AED", "2A7C3AED"),
        ["rose"] = Build("E11D48", "2AE11D48"),
        ["green"] = Build("059669", "2A059669"),
        ["blue"] = Build("2563EB", "2A2563EB"),
        ["amber"] = Build("B45309", "2AB45309")
    };

    public static SolidColorBrush Brush(string key) => Get(key).Brush;
    public static SolidColorBrush Soft(string key) => Get(key).Soft;
    public static Color AccentColor(string key) => Get(key).Color;

    public static string Label(string key) => key switch
    {
        "usb32_20" => "USB 3.2 Gen 2x2",
        "usb32_10" => "USB 3.2 Gen 2",
        "usb32_5" => "USB 3.2 Gen 1",
        "usb2" => "USB 2.0",
        "usb3" => "USB 3.x",
        "usb1" => "USB 1.1",
        _ => "USB"
    };

    public static string SpeedLabel(string key) => key switch
    {
        "usb32_20" => "20 Gbps",
        "usb32_10" => "10 Gbps",
        "usb32_5" => "5 Gbps",
        "usb2" => "480 Mbps",
        "usb3" => "5–10 Gbps",
        "usb1" => "12 Mbps",
        _ => ""
    };

    public static string Description(string key) => key switch
    {
        "usb32_20" => "双通道超高速",
        "usb32_10" => "超高速 10G",
        "usb32_5" => "超高速 5G",
        "usb2" => "高速 480M",
        "usb3" => "超高速通道",
        "usb1" => "全速 12M",
        _ => "USB 通道"
    };

    public static int SortOrder(string key) => key switch
    {
        "usb32_20" => 4,
        "usb32_10" => 3,
        "usb32_5" => 2,
        "usb3" => 2,
        "usb2" => 1,
        "usb1" => 0,
        _ => 0
    };

    private static (Color Color, SolidColorBrush Brush, SolidColorBrush Soft) Get(string key)
    {
        if (Colors.TryGetValue(key, out var item))
            return item;
        return Colors["blue"];
    }

    private static (Color, SolidColorBrush, SolidColorBrush) Build(string color, string soft)
    {
        Color main = Parse(color);
        Color softColor = Parse(soft);
        return (main, new SolidColorBrush(main), new SolidColorBrush(softColor));
    }

    private static Color Parse(string hex)
    {
        if (hex.Length == 8)
        {
            return Windows.UI.Color.FromArgb(
                byte.Parse(hex[..2], System.Globalization.NumberStyles.HexNumber),
                byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber),
                byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber),
                byte.Parse(hex.Substring(6, 2), System.Globalization.NumberStyles.HexNumber));
        }

        return Windows.UI.Color.FromArgb(
            255,
            byte.Parse(hex[..2], System.Globalization.NumberStyles.HexNumber),
            byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber),
            byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber));
    }
}
