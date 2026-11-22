namespace UsbAtlas.WinUI.ViewModels;

public static class UsbSpeed
{
    public static string TheoreticalLabel(string key) => key switch
    {
        "usb32_20" => "理论 2.42 GB/s",
        "usb32_10" => "理论 1.21 GB/s",
        "usb32_5" => "理论 500 MB/s",
        "usb2" => "理论 60 MB/s",
        "usb3" => "理论 500 MB/s–1.21 GB/s",
        "usb1" => "理论 1.5 MB/s",
        _ => ""
    };

    public static string ShortLabel(string key) => key switch
    {
        "usb32_20" => "≈ 2.42 GB/s",
        "usb32_10" => "≈ 1.21 GB/s",
        "usb32_5" => "≈ 500 MB/s",
        "usb2" => "≈ 60 MB/s",
        "usb3" => "≈ 500M–1.21G/s",
        "usb1" => "≈ 1.5 MB/s",
        _ => ""
    };

    public static string TheoreticalDetail(string key) => key switch
    {
        "usb32_20" => "20 Gbps × 128b/132b ≈ 19.4 Gbps ≈ 2.42 GB/s（不含协议开销）",
        "usb32_10" => "10 Gbps × 128b/132b ≈ 9.7 Gbps ≈ 1.21 GB/s（不含协议开销）",
        "usb32_5" => "5 Gbps × 8b/10b = 4.0 Gbps ≈ 500 MB/s（不含协议开销）",
        "usb2" => "480 Mbps 信号速率 ≈ 60 MB/s（USB 2.0 无 8b/10b 编码损耗）",
        "usb3" => "USB 3.x 编码折算后约 500 MB/s–1.21 GB/s；无法逐口区分 Gen 1 / Gen 2",
        "usb1" => "12 Mbps 信号速率 ≈ 1.5 MB/s",
        _ => ""
    };
}
