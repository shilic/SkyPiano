using System.ComponentModel;

namespace SkyPiano.ViewModel;

/// <summary>
/// 单个钢琴键位的视图模型，用于驱动键盘可视化 UI。
/// 每个键位有一个键盘标签（如 "A"、"S"）、一个 MIDI 音符编号，以及一个高亮状态。
/// 当 <see cref="IsActive"/> 变为 <c>true</c> 时，UI 上的对应键位会高亮显示。
/// </summary>
public class KeyNoteViewModel : INotifyPropertyChanged
{
    /// <summary>
    /// 键盘按键标签，取值为 "A"~"J"（中排）、"Q"~"U"（上排）、"Z"~"M"（下排）。
    /// </summary>
    public string Label { get; }

    /// <summary>
    /// MIDI 音符编号（0-127），如 C4=60、D4=62 等。
    /// </summary>
    public int MidiNumber { get; }

    /// <summary>高亮状态后备字段。</summary>
    private bool _isActive;

    /// <summary>
    /// 当前键位是否处于激活（按下）状态。
    /// 设置为 <c>true</c> 时 UI 键位高亮，设置为 <c>false</c> 时恢复默认颜色。
    /// 每次变更都会引发 <see cref="PropertyChanged"/> 事件通知 WPF 绑定系统。
    /// </summary>
    public bool IsActive
    {
        get => _isActive;
        set
        {
            // 仅当值实际发生变化时才触发通知，避免不必要的 UI 重绘
            if (_isActive == value) return;
            _isActive = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsActive)));
        }
    }

    /// <summary>
    /// 构造一个键盘键位视图模型。
    /// </summary>
    /// <param name="label">键盘按键标签，如 "A"。</param>
    /// <param name="midiNumber">对应的 MIDI 音符编号，如 60（C4）。</param>
    public KeyNoteViewModel(string label, int midiNumber)
    {
        Label = label;
        MidiNumber = midiNumber;
    }

    /// <summary>
    /// 当属性值发生更改时引发。WPF 绑定系统通过此事件感知数据变化。
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;
}
