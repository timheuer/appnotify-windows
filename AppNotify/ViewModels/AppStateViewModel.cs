using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text.Json;
using AppNotify.Models;
using AppNotify.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AppNotify.ViewModels;

public partial class AppStateViewModel : ObservableObject
{
    [ObservableProperty] private bool _isAuthenticated;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private DateTimeOffset? _lastUpdated;
    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private double _pollingMinutes = 30;
    [ObservableProperty] private bool _showingAll;
    [ObservableProperty] private string _celebrationSound = "Default";

    public int HiddenAppCount => _hiddenAppIds.Count;

    private readonly ObservableCollection<AppInfo> _apps = [];
    private readonly HashSet<string> _hiddenAppIds = [];
    private static readonly string HiddenAppsFilePath =
        Path.Combine(AppContext.BaseDirectory, "hidden-apps.json");
    private AppStoreConnectApi? _apiService;
    private PollingService? _pollingService;
    private readonly NotificationService _notificationService = new();
    private readonly CredentialService _credentialService = new();
    private List<AppInfo> _allApps = [];

    public ObservableCollection<StatusGroup> GroupedApps { get; } = [];

    public void CheckAuthentication()
    {
        Debug.WriteLine("[ViewModel] CheckAuthentication");
        LoadHiddenApps();
        var creds = _credentialService.GetCredentials();
        if (creds is not null)
        {
            Debug.WriteLine("[ViewModel] Credentials found, setting up services");
            IsAuthenticated = true;
            SetupServices();
        }
        else
        {
            Debug.WriteLine("[ViewModel] No credentials found");
        }
    }

    public void Login(string issuerID, string keyID, string privateKey)
    {
        Debug.WriteLine($"[ViewModel] Login called for issuer: {issuerID[..8]}...");
        _credentialService.SaveCredentials(issuerID, keyID, privateKey);
        IsAuthenticated = true;
        SetupServices();
        _ = RefreshAsync();
    }

    [RelayCommand]
    public void Logout()
    {
        Debug.WriteLine("[ViewModel] Logout");
        _credentialService.DeleteCredentials();
        IsAuthenticated = false;
        _allApps.Clear();
        _pollingService?.Stop();
        _pollingService = null;
        _apiService = null;
        RebuildGroups();
    }

    private void SetupServices()
    {
        Debug.WriteLine("[ViewModel] SetupServices");
        var creds = _credentialService.GetCredentials();
        if (creds is null)
        {
            Debug.WriteLine("[ViewModel] SetupServices: no credentials");
            return;
        }

        var jwt = new JwtGenerator(creds.Value.IssuerID, creds.Value.KeyID, creds.Value.PrivateKey);
        _apiService = new AppStoreConnectApi(jwt);
        Debug.WriteLine("[ViewModel] API service created");

        _pollingService = new PollingService(
            TimeSpan.FromMinutes(PollingMinutes),
            async () =>
            {
                Debug.WriteLine("[ViewModel] Polling tick - dispatching refresh");
                if (_dispatcherQueue is not null)
                {
                    var tcs = new TaskCompletionSource();
                    _dispatcherQueue.TryEnqueue(() =>
                    {
                        _ = RefreshAsync().ContinueWith(_ => tcs.SetResult());
                    });
                    await tcs.Task;
                }
                else
                {
                    Debug.WriteLine("[ViewModel] WARNING: No dispatcher queue, running refresh directly");
                    await RefreshAsync();
                }
            });
        _pollingService.Start();
        Debug.WriteLine("[ViewModel] Polling service started");
    }

    private Microsoft.UI.Dispatching.DispatcherQueue? _dispatcherQueue;

    public void SetDispatcherQueue(Microsoft.UI.Dispatching.DispatcherQueue dq)
    {
        _dispatcherQueue = dq;
        Debug.WriteLine("[ViewModel] DispatcherQueue set");
    }

    [RelayCommand]
    public async Task RefreshAsync()
    {
        if (_apiService is null)
        {
            Debug.WriteLine("[ViewModel] RefreshAsync: no API service");
            return;
        }
        Debug.WriteLine("[ViewModel] RefreshAsync: starting");
        IsLoading = true;
        ErrorMessage = null;

        try
        {
            var newApps = await _apiService.FetchAllAppsAsync();
            Debug.WriteLine($"[ViewModel] RefreshAsync: fetched {newApps.Count} apps");
            var oldApps = _allApps;

            foreach (var newApp in newApps)
            {
                var oldApp = oldApps.FirstOrDefault(a => a.Id == newApp.Id);
                if (oldApp?.LatestVersion?.Status != newApp.LatestVersion?.Status && oldApp is not null)
                {
                    var newStatus = newApp.LatestVersion?.Status ?? AppStatus.Unknown;
                    Debug.WriteLine($"[ViewModel] Status change: {newApp.Name} -> {newStatus}");
                    _notificationService.SendStatusChangeNotification(newApp.Name, newStatus);
                }
            }

            _allApps = newApps;
            LastUpdated = DateTimeOffset.Now;
            RebuildGroups();
            Debug.WriteLine($"[ViewModel] RefreshAsync: {GroupedApps.Count} status groups");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ViewModel] RefreshAsync ERROR: {ex}");
            ErrorMessage = ex.Message;
        }

        IsLoading = false;
    }

    public void HideApp(string appId)
    {
        _hiddenAppIds.Add(appId);
        SaveHiddenApps();
        OnPropertyChanged(nameof(HiddenAppCount));
        RebuildGroups();
    }

    public void UnhideApp(string appId)
    {
        _hiddenAppIds.Remove(appId);
        SaveHiddenApps();
        OnPropertyChanged(nameof(HiddenAppCount));
        RebuildGroups();
    }

    public void UnhideAllApps()
    {
        _hiddenAppIds.Clear();
        ShowingAll = false;
        SaveHiddenApps();
        OnPropertyChanged(nameof(HiddenAppCount));
        RebuildGroups();
        Debug.WriteLine("[ViewModel] All apps unhidden");
    }

    public void UpdatePollingInterval(double minutes)
    {
        PollingMinutes = minutes;
        _pollingService?.UpdateInterval(TimeSpan.FromMinutes(minutes));
    }

    partial void OnShowingAllChanged(bool value) => RebuildGroups();

    private void RebuildGroups()
    {
        var visible = ShowingAll
            ? _allApps
            : _allApps.Where(a => !_hiddenAppIds.Contains(a.Id)).ToList();

        var groups = visible
            .GroupBy(a => a.LatestVersion?.Status ?? AppStatus.Unknown)
            .OrderBy(g => g.Key.SortOrder())
            .Select(g => new StatusGroup(g.Key, g.ToList()))
            .ToList();

        GroupedApps.Clear();
        foreach (var g in groups)
            GroupedApps.Add(g);

        Debug.WriteLine($"[ViewModel] RebuildGroups: {visible.Count} visible apps, {groups.Count} groups");
    }

    private void LoadHiddenApps()
    {
        try
        {
            if (File.Exists(HiddenAppsFilePath))
            {
                var json = File.ReadAllText(HiddenAppsFilePath);
                var ids = JsonSerializer.Deserialize<List<string>>(json);
                if (ids is not null)
                {
                    foreach (var id in ids) _hiddenAppIds.Add(id);
                    Debug.WriteLine($"[ViewModel] Loaded {_hiddenAppIds.Count} hidden apps");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ViewModel] Failed to load hidden apps: {ex.Message}");
        }
    }

    private void SaveHiddenApps()
    {
        try
        {
            var json = JsonSerializer.Serialize(_hiddenAppIds.ToList());
            File.WriteAllText(HiddenAppsFilePath, json);
            Debug.WriteLine($"[ViewModel] Saved {_hiddenAppIds.Count} hidden apps");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ViewModel] Failed to save hidden apps: {ex.Message}");
        }
    }
}

public class StatusGroup
{
    public AppStatus Status { get; }
    public string DisplayName => Status.DisplayName();
    public int Count => Apps.Count;
    public List<AppInfo> Apps { get; }

    public StatusGroup(AppStatus status, List<AppInfo> apps)
    {
        Status = status;
        Apps = apps;
    }
}
