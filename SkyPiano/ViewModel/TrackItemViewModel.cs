using System.ComponentModel;

namespace SkyPiano.ViewModel;

/// <summary>
/// 播放列表中单个曲目的视图模型。
/// 用于 ListBox 的列表项，显示文件名并标记当前正在播放的曲目。
/// </summary>
public class TrackItemViewModel : INotifyPropertyChanged
{
    /// <summary>
    /// 曲目显示名称（不含扩展名的文件名）。
    /// </summary>
    public string FileName { get; }

    /// <summary>
    /// 曲目的完整文件路径，用于加载和播放。
    /// </summary>
    public string FilePath { get; }

    /// <summary>当前曲目是否正在播放。</summary>
    private bool _isPlaying;

    /// <summary>
    /// 当前曲目是否正在播放。
    /// 用于在列表中以不同样式高亮当前播放的曲目。
    /// </summary>
    public bool IsPlaying
    {
        get => _isPlaying;
        set
        {
            if (_isPlaying == value) return;
            _isPlaying = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsPlaying)));
        }
    }

    /// <summary>
    /// 构造曲目视图模型。
    /// </summary>
    /// <param name="fileName">显示名称（不含扩展名）。</param>
    /// <param name="filePath">完整文件路径。</param>
    public TrackItemViewModel(string fileName, string filePath)
    {
        FileName = fileName;
        FilePath = filePath;
    }

    /// <summary>
    /// 当属性值发生更改时引发。WPF 绑定系统通过此事件感知数据变化。
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;
}
