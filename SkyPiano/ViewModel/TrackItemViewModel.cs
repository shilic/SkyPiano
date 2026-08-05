using CommunityToolkit.Mvvm.ComponentModel;

namespace SkyPiano.ViewModel;

/// <summary>
/// 播放列表中单个曲目的视图模型。
/// </summary>
public partial class TrackItemViewModel : ObservableObject
{
    public string FileName { get; }
    public string FilePath { get; }

    [ObservableProperty]
    private bool _isPlaying;

    public TrackItemViewModel(string fileName, string filePath)
    {
        FileName = fileName;
        FilePath = filePath;
    }
}
