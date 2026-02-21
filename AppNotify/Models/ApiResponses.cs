using System.Text.Json.Serialization;

namespace AppNotify.Models;

public class AppsResponse
{
    [JsonPropertyName("data")]
    public List<AppData> Data { get; set; } = [];
}

public class AppData
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("attributes")]
    public AppAttributes Attributes { get; set; } = new();
}

public class AppAttributes
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("bundleId")]
    public string BundleId { get; set; } = string.Empty;
}

public class AppStoreVersionsResponse
{
    [JsonPropertyName("data")]
    public List<AppStoreVersionData> Data { get; set; } = [];
}

public class AppStoreVersionData
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("attributes")]
    public AppStoreVersionAttributes Attributes { get; set; } = new();
}

public class AppStoreVersionAttributes
{
    [JsonPropertyName("versionString")]
    public string? VersionString { get; set; }

    [JsonPropertyName("appVersionState")]
    public string? AppVersionState { get; set; }

    [JsonPropertyName("createdDate")]
    public string? CreatedDate { get; set; }
}
