using System.Runtime.InteropServices;
using SkyPiano.Core.Performer.Base;

namespace SkyPiano.Core.Performer.Imp;

/// <summary>
/// 基于 Win32 <c>keybd_event</c> API 的键盘模拟器。
/// 按下和释放键盘按键，适用于将 MIDI 音符转换为游戏内的键盘输入。
/// </summary>
public class KeySimulator : IPerformer {
    /// <summary>keybd_event 的"抬起"标志位。</summary>
    private const uint KEYEVENTF_KEYUP = 0x0002;

    /// <summary>
    /// Win32 keybd_event：模拟键盘事件。
    /// </summary>
    /// <param name="bVk">虚拟键码。</param>
    /// <param name="bScan">硬件扫描码，0 表示使用虚拟键码。</param>
    /// <param name="dwFlags">事件标志（0=按下，KEYEVENTF_KEYUP=抬起）。</param>
    /// <param name="dwExtraInfo">附加信息。</param>
    [DllImport("user32.dll", SetLastError = true)]
    private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

    /// <summary>
    /// Win32 VkKeyScan：将字符映射为虚拟键码。
    /// </summary>
    /// <param name="ch">要转换的字符。</param>
    /// <returns>低字节为虚拟键码，高字节为 Shift 状态。</returns>
    [DllImport("user32.dll")]
    private static extern short VkKeyScan(char ch);

    /// <summary>
    /// 将字符转换为虚拟键码。
    /// </summary>
    /// <param name="key">键盘字符（如 'A'）。</param>
    /// <returns>Windows 虚拟键码。</returns>
    /// <exception cref="ArgumentException">无法识别的字符时抛出此异常。</exception>
    private static byte GetVirtualKey(char key)
    {
        var result = VkKeyScan(char.ToUpperInvariant(key));
        if (result == -1)
            throw new ArgumentException($"无法将字符 '{key}' 转换为虚拟键码。");
        return (byte)(result & 0xFF);
    }

    /// <summary>
    /// 按下指定的键盘按键（通过 Win32 keybd_event 发送按下事件）。
    /// </summary>
    /// <param name="key">键盘字符（如 'A'），不区分大小写。</param>
    public void KeyPress(char key) {
        var vk = GetVirtualKey(key);
        keybd_event(vk, 0, 0, UIntPtr.Zero);
    }

    /// <summary>
    /// 释放指定的键盘按键（通过 Win32 keybd_event 发送抬起事件）。
    /// </summary>
    /// <param name="key">键盘字符（如 'A'），不区分大小写。</param>
    public void KeyRelease(char key) {
        var vk = GetVirtualKey(key);
        keybd_event(vk, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
    }
}
