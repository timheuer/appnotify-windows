# Copilot Instructions for AppNotify Windows

## Build

```powershell
cd AppNotify
dotnet build
```

Targets `net10.0-windows10.0.19041.0` (unpackaged WinUI 3). Requires .NET 10 preview SDK and Windows 10 2004+.

No test project or linter exists yet.

## Architecture

This is a **system tray-only WinUI 3 app** that monitors App Store Connect review statuses. There is no main window — all UI is driven from the tray icon.

### Window model

- **App.xaml.cs** — Entry point. Creates the `TaskbarIcon` (H.NotifyIcon) in code-behind, manages window lifecycle by storing references as fields to prevent GC collection.
- **PopupWindow** — Borderless window positioned bottom-right above the taskbar. Shown/hidden on tray left-click. Auto-closes on focus loss via `Activated` event. Contains `MainFlyout` (the actual UI content).
- **SettingsWindow** — NavigationView with General/Account/Developer tabs. Single-instance (reactivated if already open).
- **LoginWindow** — Shown on first launch when no credentials exist.

All windows share a single `AppStateViewModel` instance created in `App.OnLaunched()`.

### Data flow

1. `CredentialService` stores/retrieves JWT credentials via Windows `PasswordVault`
2. `JwtGenerator` creates ES256 tokens from .p8 keys (with caching)
3. `AppStoreConnectApi` fetches apps and their version statuses from App Store Connect REST API
4. `PollingService` runs on a background `PeriodicTimer`, calling `AppStateViewModel.RefreshAsync()`
5. `AppStateViewModel` groups apps by `AppStatus` into `StatusGroup` collections, detects changes, and triggers `NotificationService` for toast notifications

### MVVM

Uses **CommunityToolkit.Mvvm** with `[ObservableProperty]` field attributes (not partial properties — those cause CS9248 with current tooling). Warnings suppressed via `<NoWarn>MVVMTK0045</NoWarn>`.

## Key conventions

### XAML compiler workarounds (.NET 10 preview + WinUI 3)

The WindowsAppSDK XAML compiler has known issues in this configuration:

- **Use `{Binding}` in DataTemplates**, not `x:Bind`. The XAML compiler crashes on pass 2 (XBF generation) with `x:Bind` + `x:DataType` in nested DataTemplates.
- **Do not declare H.NotifyIcon types in XAML**. The `TaskbarIcon` must be created entirely in code-behind — XAML can't resolve H.NotifyIcon types.
- **H.NotifyIcon `TrayPopup` doesn't work** in unpackaged WinUI 3. Use the `PopupWindow` pattern (borderless Window + `LeftClickCommand`) instead.

### Threading

Background polling runs off the UI thread. All ViewModel property updates must be marshalled via `DispatcherQueue.TryEnqueue()`. The `AppStateViewModel.SetDispatcherQueue()` method must be called from `App.OnLaunched()` to capture the UI thread's dispatcher.

### System sounds

`System.Media.SystemSounds` is not available in WinUI 3. Sound playback uses P/Invoke `MessageBeep` from user32.dll via `[DllImport]` (not `[LibraryImport]`, which requires `AllowUnsafeBlocks`).

### AppStatus enum

`AppStatus.cs` defines 17 status values matching App Store Connect's `appVersionState`. Extension methods provide `DisplayName()`, `SortOrder()`, `StatusColor()`, and `FromApiString()`. Status colors are ARGB values used with `SolidColorBrush`.

### Value converters

All converters live in `Helpers/Converters.cs`: `StatusColorConverter`, `StatusDisplayNameConverter`, `BoolToVisibilityConverter`, `RelativeTimeConverter`, `NullToVisibilityConverter`. Declared as `StaticResource` in `MainFlyout.xaml`.
