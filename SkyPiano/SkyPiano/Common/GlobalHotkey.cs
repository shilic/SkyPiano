using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace SkyPiano.Common;

/// <summary>
/// 全局热键管理器（单例）。<br/>
/// 用法：<c>GlobalHotkey.Instance.Register(1, ModifierKeys.Control | ModifierKeys.Shift, Key.P, callback);</c><br/>
/// 窗口关闭时自动注销所有热键。
/// </summary>
public class GlobalHotkey {
    public static readonly GlobalHotkey Instance = new();
    private GlobalHotkey() { }

    private HwndSource? _source;
    private readonly Dictionary<int, (uint mod, uint vk, Action cb)> _hotkeys = new();

    private const int WM_HOTKEY = 0x0312;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    /// <summary> 初始化，绑定到目标窗口。只需在 Loaded 中调用一次。</summary>
    public void Initialize(Window window) {
        var hwnd = new WindowInteropHelper(window).Handle;
        _source = HwndSource.FromHwnd(hwnd);
        _source!.AddHook(WndProc);
        window.Closed += (_, _) => {
            foreach (var id in _hotkeys.Keys) UnregisterHotKey(hwnd, id);
            _source.RemoveHook(WndProc);
        };
    }

    /// <summary>注册全局热键。</summary>
    /// <param name="id">自定义 ID（用于区分不同热键）。</param>
    /// <param name="modifiers">修饰键（Ctrl/Shift/Alt 等）。</param>
    /// <param name="key">触发按键。</param>
    /// <param name="callback">按键触发时执行的回调。</param>
    public void Register(int id, ModifierKeys modifiers, Key key, Action callback) {
        uint mod = (uint)modifiers;
        uint vk = (uint)KeyInterop.VirtualKeyFromKey(key);
        if (RegisterHotKey(_source!.Handle, id, mod, vk))
            _hotkeys[id] = (mod, vk, callback);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled) {
        if (msg == WM_HOTKEY && _hotkeys.TryGetValue(wParam.ToInt32(), out var entry)) {
            entry.cb();
            handled = true;
        }
        return IntPtr.Zero;
    }
}
