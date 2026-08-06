using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using SkyPiano.ViewModel;

namespace SkyPiano;

/// <summary>
/// 主窗口。注册全局快捷键（窗口失焦时也生效），关闭时释放资源。
/// </summary>
public partial class MainWindow : Window
{
    private const int WM_HOTKEY = 0x0312;
    private const int HOTKEY_PLAYPAUSE = 1;
    private const int HOTKEY_PREV = 2;
    private const int HOTKEY_NEXT = 3;
    private HwndSource? _hwndSource;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    public MainWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Closing += OnClosing;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        _hwndSource = HwndSource.FromHwnd(hwnd);
        _hwndSource!.AddHook(HwndHook);

        // 注册三个全局热键（使用 Ctrl+Shift+Alt 修饰键避免与普通按键冲突）
        RegisterHotKey(hwnd, HOTKEY_PLAYPAUSE, (uint)(ModifierKeys.Control | ModifierKeys.Shift), (uint)KeyInterop.VirtualKeyFromKey(Key.P));
        RegisterHotKey(hwnd, HOTKEY_PREV, (uint)(ModifierKeys.Control | ModifierKeys.Shift), (uint)KeyInterop.VirtualKeyFromKey(Key.Left));
        RegisterHotKey(hwnd, HOTKEY_NEXT, (uint)(ModifierKeys.Control | ModifierKeys.Shift), (uint)KeyInterop.VirtualKeyFromKey(Key.Right));
    }

    private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg != WM_HOTKEY) return IntPtr.Zero;

        var vm = (MainViewModel)DataContext;
        switch (wParam.ToInt32())
        {
            case HOTKEY_PLAYPAUSE:
                if (vm.PlayPauseCommand.CanExecute(null))
                    vm.PlayPauseCommand.Execute(null);
                handled = true;
                break;
            case HOTKEY_PREV:
                if (vm.PrevCommand.CanExecute(null))
                    vm.PrevCommand.Execute(null);
                handled = true;
                break;
            case HOTKEY_NEXT:
                if (vm.NextCommand.CanExecute(null))
                    vm.NextCommand.Execute(null);
                handled = true;
                break;
        }
        return IntPtr.Zero;
    }

    private void OnClosing(object? sender, EventArgs e)
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        UnregisterHotKey(hwnd, HOTKEY_PLAYPAUSE);
        UnregisterHotKey(hwnd, HOTKEY_PREV);
        UnregisterHotKey(hwnd, HOTKEY_NEXT);
        _hwndSource?.RemoveHook(HwndHook);
        ((MainViewModel)DataContext).Dispose();
    }
}
