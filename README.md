# App Notify (Windows)

Windows system tray app for monitoring App Store Connect app status changes.

## Overview

App Notify is a WinUI 3 tray-only application that:

- Authenticates to App Store Connect using JWT credentials (Issuer ID, Key ID, `.p8` private key)
- Fetches your apps and current App Store version states
- Groups apps by status in a popup UI
- Polls periodically for status changes and sends notifications

## Tech Stack

- .NET 10 (preview)
- WinUI 3 (Windows App SDK)
- CommunityToolkit.Mvvm
- H.NotifyIcon (tray integration)

## Prerequisites

- Windows 10/11
- .NET 10 preview SDK

## Build

```powershell
cd AppNotify
dotnet build
```

## Run

```powershell
cd AppNotify
dotnet run
```

## First Launch

1. Enter App Store Connect JWT details in the setup window.
2. After saving, use the tray icon to open the popup.
3. Open Settings from the tray menu for polling and developer options.

## Repository Layout

- `AppNotify/` - WinUI 3 application source
- `.github/copilot-instructions.md` - Copilot guidance for this repository
- `docs/` - supporting documentation
