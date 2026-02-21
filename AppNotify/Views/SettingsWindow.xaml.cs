using AppNotify.Models;
using AppNotify.Services;
using AppNotify.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace AppNotify.Views;

public sealed partial class SettingsWindow : Window
{
    private readonly AppStateViewModel _viewModel;

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool MessageBeep(uint uType);

    public SettingsWindow(AppStateViewModel viewModel)
    {
        _viewModel = viewModel;
        InitializeComponent();
        Title = "App Notify — Settings";
        SystemBackdrop = new Microsoft.UI.Xaml.Media.DesktopAcrylicBackdrop();
        AppWindow.Resize(new Windows.Graphics.SizeInt32(500, 500));
        AppWindow.Closing += (_, args) =>
        {
            args.Cancel = true;
            AppWindow.Hide();
        };

        NavView.SelectedItem = NavView.MenuItems[0];

        // Set polling combo to current value
        var mins = _viewModel.PollingMinutes;
        foreach (ComboBoxItem item in PollingCombo.Items)
        {
            if (item.Tag is string tag && double.TryParse(tag, out var val) && val == mins)
            {
                PollingCombo.SelectedItem = item;
                break;
            }
        }

        // Set sound combo to current value
        var sound = _viewModel.CelebrationSound;
        foreach (ComboBoxItem item in SoundCombo.Items)
        {
            if (item.Tag is string tag && tag == sound)
            {
                SoundCombo.SelectedItem = item;
                break;
            }
        }
        if (SoundCombo.SelectedItem is null)
            SoundCombo.SelectedIndex = 0;

        UpdateAccountUI();
        UpdateDeveloperState();
    }

    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is NavigationViewItem item)
        {
            var tag = item.Tag as string;
            GeneralPanel.Visibility = tag == "General" ? Visibility.Visible : Visibility.Collapsed;
            AccountPanel.Visibility = tag == "Account" ? Visibility.Visible : Visibility.Collapsed;
            DeveloperPanel.Visibility = tag == "Developer" ? Visibility.Visible : Visibility.Collapsed;

            if (tag == "Developer")
                UpdateDeveloperState();
        }
    }

    private void PollingChanged(object sender, SelectionChangedEventArgs e)
    {
        if (PollingCombo.SelectedItem is ComboBoxItem item &&
            item.Tag is string tag && double.TryParse(tag, out var minutes))
        {
            _viewModel.UpdatePollingInterval(minutes);
        }
    }

    private void SoundChanged(object sender, SelectionChangedEventArgs e)
    {
        if (SoundCombo.SelectedItem is ComboBoxItem item && item.Tag is string tag)
        {
            _viewModel.CelebrationSound = tag;
            Debug.WriteLine($"[Settings] Sound changed to: {tag}");
        }
    }

    private void PreviewSoundClick(object sender, RoutedEventArgs e)
    {
        var sound = _viewModel.CelebrationSound;
        Debug.WriteLine($"[Settings] Preview sound: {sound}");
        PlaySystemSound(sound);
    }

    private void TriggerCelebrationClick(object sender, RoutedEventArgs e)
    {
        Debug.WriteLine("[Settings] Trigger celebration");
        PlaySystemSound(_viewModel.CelebrationSound);
        ConfettiWindow.FireFullscreen();
        var service = new NotificationService();
        service.SendStatusChangeNotification("Test App", AppStatus.PendingDeveloperRelease);
    }

    private void SimulateNotificationClick(object sender, RoutedEventArgs e)
    {
        Debug.WriteLine("[Settings] Simulate notification");
        var service = new NotificationService();
        service.SendStatusChangeNotification("Test App", AppStatus.InReview);
    }

    private void SignOutClick(object sender, RoutedEventArgs e)
    {
        _viewModel.Logout();
        UpdateAccountUI();
    }

    private void UnhideAllClick(object sender, RoutedEventArgs e)
    {
        _viewModel.UnhideAllApps();
        UpdateDeveloperState();
    }

    private void UpdateAccountUI()
    {
        ConnectedPanel.Visibility = _viewModel.IsAuthenticated ? Visibility.Visible : Visibility.Collapsed;
        NotConnectedText.Visibility = _viewModel.IsAuthenticated ? Visibility.Collapsed : Visibility.Visible;
    }

    private void UpdateDeveloperState()
    {
        AppsCountText.Text = _viewModel.GroupedApps.Sum(g => g.Count).ToString();
        HiddenCountText.Text = _viewModel.HiddenAppCount.ToString();
        PollingText.Text = $"{(int)_viewModel.PollingMinutes} min";
        SoundText.Text = _viewModel.CelebrationSound;
        UnhideAllBtn.Visibility = _viewModel.HiddenAppCount > 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private static void PlaySystemSound(string soundName)
    {
        // MB_OK=0, MB_ICONASTERISK=0x40, MB_ICONEXCLAMATION=0x30, MB_ICONHAND=0x10, MB_ICONQUESTION=0x20
        uint type = soundName switch
        {
            "Asterisk" => 0x40,
            "Exclamation" => 0x30,
            "Hand" => 0x10,
            "Question" => 0x20,
            _ => 0x00,
        };
        MessageBeep(type);
    }
}
