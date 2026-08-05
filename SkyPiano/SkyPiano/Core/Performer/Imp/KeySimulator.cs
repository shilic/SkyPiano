using System.Runtime.InteropServices;
using SkyPiano.Core.MusicTheory;
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
    /// 按下指定的键盘按键（通过 Win32 keybd_event 发送按下事件）。
    /// </summary>
    /// <param name="note">21 键音符枚举值。</param>
    public void KeyPress(MyNote note) {
        keybd_event(note.ToVirtualKey(), 0, 0, UIntPtr.Zero);
    }

    /// <summary>
    /// 释放指定的键盘按键（通过 Win32 keybd_event 发送抬起事件）。
    /// </summary>
    /// <param name="note">21 键音符枚举值。</param>
    public void KeyRelease(MyNote note) {
        keybd_event(note.ToVirtualKey(), 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
    }
}
