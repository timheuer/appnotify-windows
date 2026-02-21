using System.Net.Http.Json;
using System.Diagnostics;
using AppNotify.Models;

namespace AppNotify.Services;

public sealed class AppStoreConnectApi
{
    private readonly JwtGenerator _jwtGenerator;
    private readonly HttpClient _httpClient;
    private const string BaseUrl = "https://api.appstoreconnect.apple.com/v1";

    public AppStoreConnectApi(JwtGenerator jwtGenerator)
    {
        _jwtGenerator = jwtGenerator;
        _httpClient = new HttpClient();
    }

    public async Task<List<AppInfo>> FetchAllAppsAsync()
    {
        Debug.WriteLine("[API] Fetching all apps...");
        var response = await RequestAsync<AppsResponse>(
            "/apps?fields[apps]=name,bundleId&limit=200");

        Debug.WriteLine($"[API] Found {response.Data.Count} apps");
        var apps = new List<AppInfo>();
        foreach (var appData in response.Data)
        {
            Debug.WriteLine($"[API] Fetching versions for {appData.Attributes.Name} ({appData.Id})");
            var versionInfo = await FetchLatestVersionAsync(appData.Id);
            apps.Add(new AppInfo
            {
                Id = appData.Id,
                Name = appData.Attributes.Name,
                BundleId = appData.Attributes.BundleId,
                LatestVersion = versionInfo
            });
            Debug.WriteLine($"[API]   -> {appData.Attributes.Name}: {versionInfo?.Status} v{versionInfo?.VersionString}");
        }

        apps.Sort((a, b) =>
            string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
        return apps;
    }

    private async Task<AppVersionInfo?> FetchLatestVersionAsync(string appId)
    {
        var endpoint = $"/apps/{appId}/appStoreVersions?limit=200&fields[appStoreVersions]=versionString,appVersionState,createdDate";
        var response = await RequestAsync<AppStoreVersionsResponse>(endpoint);

        if (response.Data.Count == 0) return null;

        var sorted = response.Data
            .OrderByDescending(v =>
                DateTimeOffset.TryParse(v.Attributes.CreatedDate, out var d) ? d : DateTimeOffset.MinValue)
            .First();

        DateTimeOffset.TryParse(sorted.Attributes.CreatedDate, out var createdDate);

        return new AppVersionInfo
        {
            VersionString = sorted.Attributes.VersionString ?? "—",
            Status = AppStatusExtensions.FromApiString(sorted.Attributes.AppVersionState),
            CreatedDate = createdDate == default ? null : createdDate
        };
    }

    private async Task<T> RequestAsync<T>(string endpoint) where T : class
    {
        var token = _jwtGenerator.GenerateToken();
        Debug.WriteLine($"[API] GET {BaseUrl}{endpoint}");
        using var request = new HttpRequestMessage(HttpMethod.Get, BaseUrl + endpoint);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var response = await _httpClient.SendAsync(request);
        Debug.WriteLine($"[API] HTTP {(int)response.StatusCode} for {endpoint}");

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            Debug.WriteLine($"[API] ERROR body: {body[..Math.Min(body.Length, 500)]}");
            throw new HttpRequestException(
                $"HTTP {(int)response.StatusCode}: {body[..Math.Min(body.Length, 200)]}");
        }

        return await response.Content.ReadFromJsonAsync<T>()
            ?? throw new InvalidOperationException("Failed to deserialize response");
    }
}
