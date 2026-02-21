using AppNotify.ViewModels;
using AppNotify.Views;
using H.NotifyIcon;
using H.NotifyIcon.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Diagnostics;

namespace AppNotify;

public partial class App : Application
{
    private TaskbarIcon? _trayIcon;
    private AppStateViewModel? _viewModel;
    private Microsoft.UI.Dispatching.DispatcherQueue? _dispatcherQueue;
    private Window? _settingsWindow;
    private Window? _loginWindow;
    private PopupWindow? _popupWindow;

    public App()
    {
        InitializeComponent();
        UnhandledException += (s, e) =>
        {
            Debug.WriteLine($"[App] UNHANDLED EXCEPTION: {e.Exception}");
            e.Handled = true;
        };
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        Debug.WriteLine("[App] OnLaunched");

        _dispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
        _viewModel = new AppStateViewModel();
        _viewModel.SetDispatcherQueue(_dispatcherQueue);
        _viewModel.CheckAuthentication();
        Debug.WriteLine($"[App] IsAuthenticated: {_viewModel.IsAuthenticated}");

        var iconPath = System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "app-icon.ico");
        Debug.WriteLine($"[App] Icon path: {iconPath}, exists: {System.IO.File.Exists(iconPath)}");

        _trayIcon = new TaskbarIcon
        {
            ToolTipText = "App Store Notify",
            Icon = new System.Drawing.Icon(iconPath),
            MenuActivation = PopupActivationMode.RightClick,
        };

        _trayIcon.LeftClickCommand = new CommunityToolkit.Mvvm.Input.RelayCommand(() => RunOnUi(TogglePopup));

        var contextMenu = new MenuFlyout();
        var settingsItem = new MenuFlyoutItem
        {
            Text = "Settings...",
            Icon = new FontIcon { Glyph = "\xE713" },
            Command = new CommunityToolkit.Mvvm.Input.RelayCommand(() => RunOnUi(ShowSettingsWindow))
        };
        contextMenu.Items.Add(settingsItem);
        contextMenu.Items.Add(new MenuFlyoutSeparator());
        var quitItem = new MenuFlyoutItem
        {
            Text = "Quit",
            Command = new CommunityToolkit.Mvvm.Input.RelayCommand(() => RunOnUi(QuitApp))
        };
        contextMenu.Items.Add(quitItem);
        _trayIcon.ContextFlyout = contextMenu;

        _trayIcon.ForceCreate();
        Debug.WriteLine("[App] TrayIcon created");

        if (!_viewModel.IsAuthenticated)
        {
            Debug.WriteLine("[App] Not authenticated, showing login window");
            ShowLoginWindow();
        }
    }

    private void TogglePopup()
    {
        Debug.WriteLine("[App] TogglePopup called");
        if (_popupWindow is not null)
        {
            if (_popupWindow.AppWindow.IsVisible)
            {
                Debug.WriteLine("[App] Hiding popup");
                _popupWindow.AppWindow.Hide();
            }
            else
            {
                Debug.WriteLine("[App] Showing popup");
                _popupWindow.AppWindow.Show();
                _popupWindow.Activate();
            }
            return;
        }

        _popupWindow = new PopupWindow(_viewModel!);
        _popupWindow.Closed += (_, _) =>
        {
            Debug.WriteLine("[App] Popup closed");
            _popupWindow = null;
        };
        _popupWindow.Activate();
        Debug.WriteLine("[App] Popup activated");
    }

    private void ShowLoginWindow()
    {
        if (_loginWindow is not null)
        {
            _loginWindow.Activate();
            return;
        }
        _loginWindow = new LoginWindow(_viewModel!);
        _loginWindow.Closed += (_, _) =>
        {
            Debug.WriteLine("[App] Login window closed");
            _loginWindow = null;
        };
        _loginWindow.Activate();
    }

    private void ShowSettingsWindow()
    {
        Debug.WriteLine("[App] Settings menu clicked");
        if (_viewModel is null) return;

        if (_settingsWindow is not null)
        {
            if (!_settingsWindow.AppWindow.IsVisible)
                _settingsWindow.AppWindow.Show();
            _settingsWindow.Activate();
            return;
        }
        _settingsWindow = new SettingsWindow(_viewModel);
        _settingsWindow.Closed += (_, _) =>
        {
            Debug.WriteLine("[App] Settings window closed");
            _settingsWindow = null;
        };
        _settingsWindow.Activate();
        Debug.WriteLine("[App] Settings window activated");
    }

    private void QuitApp()
    {
        Debug.WriteLine("[App] Quit clicked");
        _trayIcon?.Dispose();
        _popupWindow?.Close();
        _settingsWindow?.Close();
        _loginWindow?.Close();
        Environment.Exit(0);
    }

    internal void ShowSettingsFromFlyout() => RunOnUi(ShowSettingsWindow);

    internal void QuitFromFlyout() => RunOnUi(QuitApp);

    private void RunOnUi(Action action)
    {
        if (_dispatcherQueue is null || _dispatcherQueue.HasThreadAccess)
        {
            action();
            return;
        }

        _dispatcherQueue.TryEnqueue(() => action());
    }

    public static AppStateViewModel? ViewModel =>
        (Current as App)?._viewModel;
}
