namespace UsbAtlas.Models;

public sealed class BoardProfileFile
{
    public int SchemaVersion { get; set; } = 1;
    public List<BoardProfile> Profiles { get; set; } = [];
}

public sealed class BoardProfile
{
    public List<string> ManufacturerAliases { get; set; } = [];
    public string Model { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string FormFactor { get; set; } = "";
    public string Chipset { get; set; } = "";
    public string SpecUrl { get; set; } = "";
    public string SourceLabel { get; set; } = "官方规格";
    public string VerifiedOn { get; set; } = "";
    public List<BoardPortGroup> Rear { get; set; } = [];
    public List<BoardPortGroup> Front { get; set; } = [];

    public bool Matches(string manufacturer, string model)
    {
        if (!string.Equals(Normalize(Model), Normalize(model), StringComparison.OrdinalIgnoreCase))
            return false;

        var normalizedManufacturer = Normalize(manufacturer);
        return ManufacturerAliases.Any(alias =>
        {
            var normalizedAlias = Normalize(alias);
            return normalizedManufacturer.Contains(normalizedAlias, StringComparison.OrdinalIgnoreCase)
                   || normalizedAlias.Contains(normalizedManufacturer, StringComparison.OrdinalIgnoreCase);
        });
    }

    public int TotalPorts => Rear.Sum(p => p.Count) + Front.Sum(p => p.Count);

    public static string Normalize(string value) =>
        string.Join(' ', value.Split([' ', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries)).Trim();
}

public sealed class BoardPortGroup
{
    public string Type { get; set; } = "USB-A";
    public string Version { get; set; } = "USB 2.0";
    public string VersionKey { get; set; } = "usb2";
    public double SpeedGbps { get; set; } = 0.48;
    public int Count { get; set; } = 1;
    public string? Note { get; set; }
    public string? Header { get; set; }

    public string SpeedLabel => FormatSpeed(SpeedGbps);

    public static string FormatSpeed(double gbps) => gbps switch
    {
        >= 20 => "20 Gbps",
        >= 10 => "10 Gbps",
        >= 5 => "5 Gbps",
        _ => "480 Mbps"
    };
}
