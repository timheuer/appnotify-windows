using AppNotify.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Diagnostics;

namespace AppNotify.Views;

public sealed partial class MainFlyout : UserControl
{
    private AppStateViewModel ViewModel => (AppStateViewModel)DataContext;

    public MainFlyout()
    {
        Debug.WriteLine("[MainFlyout] Constructor");
        InitializeComponent();
        Loaded += (_, _) => Debug.WriteLine("[MainFlyout] Loaded");
        DataContextChanged += (_, _) => Debug.WriteLine($"[MainFlyout] DataContextChanged: {DataContext?.GetType().Name}");
    }

    private async void RefreshClick(object sender, RoutedEventArgs e)
    {
        Debug.WriteLine("[MainFlyout] Refresh clicked");
        await ViewModel.RefreshAsync();
    }

    private void ShowAllClick(object sender, RoutedEventArgs e)
    {
        Debug.WriteLine("[MainFlyout] ShowAll clicked");
        ViewModel.ShowingAll = !ViewModel.ShowingAll;
        ShowAllBtn.Content = ViewModel.ShowingAll ? "Hide Hidden" : "Show All";
    }

    private void SettingsClick(object sender, RoutedEventArgs e)
    {
        Debug.WriteLine("[MainFlyout] Settings clicked");
        if (Application.Current is App app)
            app.ShowSettingsFromFlyout();
    }

    private void QuitClick(object sender, RoutedEventArgs e)
    {
        Debug.WriteLine("[MainFlyout] Quit clicked");
        if (Application.Current is App app)
            app.QuitFromFlyout();
    }

    private void HideAppClick(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem item && item.Tag is string appId)
        {
            Debug.WriteLine($"[MainFlyout] Hide app: {appId}");
            ViewModel.HideApp(appId);
        }
    }
}
