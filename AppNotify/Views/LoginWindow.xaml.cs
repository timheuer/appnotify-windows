using AppNotify.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage.Pickers;
using System.Diagnostics;

namespace AppNotify.Views;

public sealed partial class LoginWindow : Window
{
    private readonly AppStateViewModel _viewModel;
    private string? _privateKey;

    public LoginWindow(AppStateViewModel viewModel)
    {
        Debug.WriteLine("[LoginWindow] Constructor");
        _viewModel = viewModel;
        InitializeComponent();
        Title = "App Notify — Setup";
        SystemBackdrop = new Microsoft.UI.Xaml.Media.DesktopAcrylicBackdrop();
        AppWindow.Resize(new Windows.Graphics.SizeInt32(420, 520));

        IssuerIdBox.TextChanged += (_, _) => UpdateConnectEnabled();
        KeyIdBox.TextChanged += (_, _) => UpdateConnectEnabled();
    }

    private void UpdateConnectEnabled() =>
        ConnectBtn.IsEnabled = !string.IsNullOrWhiteSpace(IssuerIdBox.Text)
            && !string.IsNullOrWhiteSpace(KeyIdBox.Text)
            && _privateKey is not null;

    private async void ChooseFileClick(object sender, RoutedEventArgs e)
    {
        var picker = new FileOpenPicker();
        picker.FileTypeFilter.Add(".p8");
        picker.FileTypeFilter.Add("*");

        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

        var file = await picker.PickSingleFileAsync();
        if (file is null) return;

        _privateKey = await Windows.Storage.FileIO.ReadTextAsync(file);
        ChooseFileBtn.Visibility = Visibility.Collapsed;
        KeyLoadedPanel.Visibility = Visibility.Visible;
        UpdateConnectEnabled();
    }

    private void ClearKeyClick(object sender, RoutedEventArgs e)
    {
        _privateKey = null;
        ChooseFileBtn.Visibility = Visibility.Visible;
        KeyLoadedPanel.Visibility = Visibility.Collapsed;
        UpdateConnectEnabled();
    }

    private void ConnectClick(object sender, RoutedEventArgs e)
    {
        Debug.WriteLine("[LoginWindow] Connect clicked");
        try
        {
            _viewModel.Login(
                IssuerIdBox.Text.Trim(),
                KeyIdBox.Text.Trim(),
                _privateKey!);
            Debug.WriteLine("[LoginWindow] Login succeeded, closing window");
            Close();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[LoginWindow] Login error: {ex}");
            ErrorBar.Message = ex.Message;
            ErrorBar.IsOpen = true;
        }
    }
}
