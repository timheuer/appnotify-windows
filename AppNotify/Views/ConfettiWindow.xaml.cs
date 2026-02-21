using AppNotify.Helpers;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Windows.Graphics;

namespace AppNotify.Views;

public sealed partial class ConfettiWindow : Window
{
    private ConfettiHelper? _confetti;
    private static ConfettiWindow? _current;

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(nint hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(nint hWnd, int nIndex);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowPos(nint hWnd, nint hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_TRANSPARENT = 0x00000020;
    private const int WS_EX_TOOLWINDOW = 0x00000080;
    private const int WS_EX_LAYERED = 0x00080000;
    private static readonly nint HWND_TOPMOST = new(-1);

    public ConfettiWindow()
    {
        InitializeComponent();

        var presenter = AppWindow.Presenter as OverlappedPresenter;
        if (presenter is not null)
        {
            presenter.IsResizable = false;
            presenter.IsMaximizable = false;
            presenter.IsMinimizable = false;
            presenter.SetBorderAndTitleBar(false, false);
            presenter.IsAlwaysOnTop = true;
        }

        // Cover entire primary display
        var display = DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Primary);
        var bounds = display.OuterBounds;
        AppWindow.MoveAndResize(new RectInt32(bounds.X, bounds.Y, bounds.Width, bounds.Height));

        // Make window click-through and hide from taskbar
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        int exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
        SetWindowLong(hwnd, GWL_EXSTYLE, exStyle | WS_EX_TRANSPARENT | WS_EX_LAYERED | WS_EX_TOOLWINDOW);
        SetWindowPos(hwnd, HWND_TOPMOST, 0, 0, 0, 0, 0x0001 | 0x0002); // SWP_NOSIZE | SWP_NOMOVE

        Debug.WriteLine($"[ConfettiWindow] Fullscreen overlay: {bounds.Width}x{bounds.Height}");
    }

    public static void FireFullscreen()
    {
        if (_current is not null)
        {
            _current._confetti?.Fire();
            return;
        }

        _current = new ConfettiWindow();
        _current.Closed += (_, _) => _current = null;
        _current.Activate();

        _current._confetti = new ConfettiHelper(_current.ConfettiCanvas);
        _current._confetti.Completed += () =>
        {
            Debug.WriteLine("[ConfettiWindow] Animation complete, closing");
            _current.Close();
            _current = null;
        };
        _current._confetti.Fire();
    }
}
