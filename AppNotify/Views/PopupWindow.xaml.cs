using AppNotify.ViewModels;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using System.Diagnostics;
using Windows.Graphics;

namespace AppNotify.Views;

public sealed partial class PopupWindow : Window
{
    public PopupWindow(AppStateViewModel viewModel)
    {
        Debug.WriteLine("[PopupWindow] Constructor");
        InitializeComponent();
        FlyoutContent.DataContext = viewModel;
        SystemBackdrop = new Microsoft.UI.Xaml.Media.DesktopAcrylicBackdrop();

        // Configure as a compact popup-style window
        var presenter = AppWindow.Presenter as OverlappedPresenter;
        if (presenter is not null)
        {
            presenter.IsResizable = false;
            presenter.IsMaximizable = false;
            presenter.IsMinimizable = false;
            presenter.SetBorderAndTitleBar(true, false);
        }

        AppWindow.Resize(new SizeInt32(440, 520));
        PositionNearTray();

        // Hide when focus is lost (don't Close — that can terminate the app)
        Activated += (_, args) =>
        {
            if (args.WindowActivationState == WindowActivationState.Deactivated)
            {
                Debug.WriteLine("[PopupWindow] Lost focus, hiding");
                AppWindow.Hide();
            }
        };
    }

    private void PositionNearTray()
    {
        var displayArea = DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Primary);
        var workArea = displayArea.WorkArea;

        // Position in bottom-right corner, above the taskbar
        int x = workArea.X + workArea.Width - 440 - 12;
        int y = workArea.Y + workArea.Height - 520 - 12;

        AppWindow.Move(new PointInt32(x, y));
        Debug.WriteLine($"[PopupWindow] Positioned at ({x}, {y}), workArea: {workArea.Width}x{workArea.Height}");
    }
}
