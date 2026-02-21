namespace AppNotify.Models;

public class AppInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string BundleId { get; set; } = string.Empty;
    public AppVersionInfo? LatestVersion { get; set; }
}

public class AppVersionInfo
{
    public string VersionString { get; set; } = "—";
    public AppStatus Status { get; set; } = AppStatus.Unknown;
    public DateTimeOffset? CreatedDate { get; set; }
}
