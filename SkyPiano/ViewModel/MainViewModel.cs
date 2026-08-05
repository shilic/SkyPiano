using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SkyPiano.Core.Player.Imp;

namespace SkyPiano.ViewModel;

/// <summary>
/// 主窗口的视图模型。使用 CommunityToolkit.Mvvm 源生成器，
/// [ObservableProperty] 标注属性，[RelayCommand] 标注命令。
/// </summary>
public partial class MainViewModel : ObservableObject, IDisposable
{
    /// <summary>键盘钢琴播放器，将 MIDI 转为键盘按键输出。</summary>
    private readonly KeyPianoPlayer _player;
    /// <summary>播放列表管理器，负责曲目导航和文件列表。</summary>
    private readonly PlaylistManager _playlist;

    /// <summary>默认 MIDI 文件夹路径：用户文档目录下的 "SkyPiano/MIDI"。</summary>
    private static readonly string DefaultMidiFolder = System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "SkyPiano", "MIDI");

    /// <summary>播放列表中的所有曲目项，供 UI 的 ListBox 绑定。</summary>
    public ObservableCollection<TrackItemViewModel> PlaylistItems { get; } = new();

    // ---- ObservableProperty ----
    /// <summary>当前播放曲目的文件名（不含扩展名）。</summary>
    [ObservableProperty]
    private string _currentTrackName = "";
    /// <summary>曲目索引显示字符串，格式为"当前序号/总数"，如"3/12"。</summary>
    [ObservableProperty]
    private string _trackIndex = "";
    /// <summary>当前是否正在播放。用于切换播放/暂停按钮的图标。</summary>
    [ObservableProperty]
    private bool _isPlaying;
    /// <summary>播放列表中当前选中的曲目索引（从 0 开始，-1 为无选中）。</summary>
    [ObservableProperty]
    private int _selectedTrackIndex = -1;
    partial void OnSelectedTrackIndexChanged(int value) {
        if (value >= 0 && value < _playlist.Count && value != _playlist.CurrentIndex)
            _playlist.SelectTrack(value);
    }

    // ---- ObservableProperty ----

    /// <summary>播放进度，0.0（开头）到 1.0（结尾）。</summary>
    [ObservableProperty]
    private double _progress;

    /// <summary>当前播放时间的格式化字符串，格式为"mm:ss"。</summary>
    [ObservableProperty]
    private string _currentTimeDisplay = "";

    /// <summary>曲目总时长的格式化字符串，格式为"mm:ss"。</summary>
    [ObservableProperty]
    private string _durationDisplay = "";

    // ---- 构造 ----

    public MainViewModel() {
        _playlist = new PlaylistManager();
        _player = new KeyPianoPlayer(_playlist);

        _playlist.TrackChanged += OnTrackChanged;
        _player.StateChanged += () => Application.Current.Dispatcher.Invoke(RefreshState);
        _player.ProgressUpdated += (progress, currentTime) => Application.Current.Dispatcher.Invoke(() => {
            _progress = progress;
            _currentTimeDisplay = $"{(int)currentTime.TotalMinutes:D2}:{currentTime.Seconds:D2}";
        });
    }

    // ---- RelayCommand ----

    /// <summary>播放 / 暂停命令。切换播放状态并刷新 UI。</summary>
    [RelayCommand]
    private void PlayPause() {
        _player.咋瓦鲁多();
        RefreshState();
    }

    /// <summary>下一首命令。</summary>
    [RelayCommand]
    private void Next() => _player.墓志铭();

    /// <summary>上一首命令。</summary>
    [RelayCommand]
    private void Prev() => _player.男人领域();

    /// <summary>快退命令（后退 5 秒）。</summary>
    [RelayCommand]
    private void Rewind() => _player.败者食尘();

    /// <summary>快进命令（前进 5 秒）。</summary>
    [RelayCommand]
    private void FastForward() => _player.天堂制造();

    /// <summary>切换 MIDI 文件夹命令。</summary>
    [RelayCommand]
    private void SwitchFolder() => OpenFolder();

    // ---- 内部方法 ----

    /// <summary>播放列表切换曲目时的回调：重建 UI 集合并更新高亮。</summary>
    private void OnTrackChanged(string? path) {
        if (path == null) return;

        if (PlaylistItems.Count != _playlist.Count ||
            (PlaylistItems.Count > 0 && PlaylistItems[0].FilePath != _playlist.Tracks[0]))
        {
            PlaylistItems.Clear();
            foreach (var trackPath in _playlist.Tracks)
            {
                var fileName = System.IO.Path.GetFileNameWithoutExtension(trackPath);
                PlaylistItems.Add(new TrackItemViewModel(fileName, trackPath));
            }
        }

        CurrentTrackName = System.IO.Path.GetFileNameWithoutExtension(path);
        RefreshState();
    }

    /// <summary>刷新播放状态到 UI（IsPlaying、TrackIndex、曲目高亮、选中索引）。</summary>
    private void RefreshState() {
        IsPlaying = _player.IsPlaying;
        TrackIndex = _playlist.Count > 0 ? $"{_playlist.CurrentIndex + 1}/{_playlist.Count}" : "";
        SelectedTrackIndex = _playlist.CurrentIndex;
        DurationDisplay = $"{(int)_player.Duration.TotalMinutes:D2}:{_player.Duration.Seconds:D2}";

        var current = _playlist.CurrentIndex;
        for (var i = 0; i < PlaylistItems.Count; i++)
            PlaylistItems[i].IsPlaying = i == current;
    }

    /// <summary>弹出文件夹选择对话框，切换 MIDI 文件夹。</summary>
    private void OpenFolder()
    {
        var dlg = new Microsoft.Win32.OpenFolderDialog
        {
            Title = "选择 MIDI 文件夹",
            InitialDirectory = DefaultMidiFolder
        };
        if (dlg.ShowDialog() != true) return;
        _player.恶行易施(dlg.FolderName);
    }

    /// <summary>加载默认 MIDI 文件夹（Documents/SkyPiano/MIDI），不存在则自动创建。</summary>
    public void LoadDefaultFolder()
    {
        if (!System.IO.Directory.Exists(DefaultMidiFolder))
            System.IO.Directory.CreateDirectory(DefaultMidiFolder);
        _player.恶行易施(DefaultMidiFolder);
    }

    /// <summary>释放资源：释放播放器。</summary>
    public void Dispose()
    {
        _player.Dispose();
    }
}
