using CommunityToolkit.Mvvm.ComponentModel;

namespace SkyPiano.ViewModel;

/// <summary>
/// 播放列表中单个曲目的视图模型。
/// 用于 ListBox 的列表项绑定，显示文件名并标记当前播放状态。
/// </summary>
/// <param name="fileName">显示名称（不含扩展名）。</param>
/// <param name="filePath">完整文件路径，用于加载和播放。</param>
public partial class TrackItemViewModel(string fileName, string filePath) : ObservableObject {
    /// <summary>曲目显示名称（不含扩展名的文件名）。</summary>
    public string FileName { get; } = fileName;

    /// <summary>曲目的完整文件路径，用于加载和播放。</summary>
    public string FilePath { get; } = filePath;

    /// <summary>当前曲目是否正在播放。用于在列表中高亮显示当前曲目。</summary>
    [ObservableProperty]
    private bool _isPlaying;
}
