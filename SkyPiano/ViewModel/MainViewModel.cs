using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SkyPiano.Core.Player.Imp;

namespace SkyPiano.ViewModel;

/// <summary>
/// 主窗口的视图模型。只依赖 <see cref="KeyPianoPlayer"/>，不再直接持有 PlaylistManager。
/// </summary>
public partial class MainViewModel : ObservableObject, IDisposable
{
    /// <summary>键盘钢琴播放器，内部集成调度器和播放列表。</summary>
    private readonly KeyPianoPlayer _player;

    /// <summary>默认 MIDI 文件夹路径。</summary>
    private static readonly string DefaultMidiFolder = System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "SkyPiano", "MIDI");

    /// <summary>播放列表中的所有曲目项，供 UI 的 ListBox 绑定。</summary>
    public ObservableCollection<TrackItemViewModel> PlaylistItems { get; } = new();

    // ---- ObservableProperty ----

    [ObservableProperty] private string _currentTrackName = "";
    [ObservableProperty] private string _trackIndex = "";
    [ObservableProperty] private bool _isPlaying;
    [ObservableProperty] private int _selectedTrackIndex = -1;
    [ObservableProperty] private double _progress;
    [ObservableProperty] private string _currentTimeDisplay = "";
    [ObservableProperty] private string _durationDisplay = "";

    partial void OnSelectedTrackIndexChanged(int value) {
        if (value >= 0 && value < _player.TrackCount && value != _player.CurrentTrackIndex)
            _player.SelectTrack(value);
    }

    // ---- 构造 ----

    public MainViewModel() {
        _player = new KeyPianoPlayer();

        _player.TrackChanged += OnTrackChanged;
        _player.StateChanged += () => Application.Current.Dispatcher.Invoke(RefreshState);
        _player.ProgressUpdated += (progress, time) => Application.Current.Dispatcher.Invoke(() => {
            Progress = progress;
            CurrentTimeDisplay = $"{(int)time.TotalMinutes:D2}:{time.Seconds:D2}";
        });
    }

    // ---- RelayCommand ----

    [RelayCommand] private void PlayPause() => _player.咋瓦鲁多();

    [RelayCommand] private void Next() { 
        _player.墓志铭(); 
        //_player.RequestPlay(); 
    }

    [RelayCommand] private void Prev() { 
        _player.男人领域(); 
        //_player.RequestPlay(); 
    }

    [RelayCommand] private void Rewind() => _player.败者食尘();

    [RelayCommand] private void FastForward() => _player.天堂制造();

    [RelayCommand] private void SwitchFolder() => OpenFolder();

    // ---- 内部方法 ----

    private void OnTrackChanged(string? path) {
        if (path == null) return;

        if (PlaylistItems.Count != _player.TrackCount ||
            (PlaylistItems.Count > 0 && PlaylistItems[0].FilePath != _player.Tracks[0])) {
            PlaylistItems.Clear();
            foreach (var trackPath in _player.Tracks)
                PlaylistItems.Add(new TrackItemViewModel(System.IO.Path.GetFileNameWithoutExtension(trackPath), trackPath));
        }

        CurrentTrackName = System.IO.Path.GetFileNameWithoutExtension(path);
        RefreshState();
    }

    private void RefreshState()
    {
        IsPlaying = _player.IsPlaying;
        TrackIndex = _player.TrackCount > 0 ? $"{_player.CurrentTrackIndex + 1}/{_player.TrackCount}" : "";
        SelectedTrackIndex = _player.CurrentTrackIndex;
        DurationDisplay = $"{(int)_player.Duration.TotalMinutes:D2}:{_player.Duration.Seconds:D2}";

        var current = _player.CurrentTrackIndex;
        for (var i = 0; i < PlaylistItems.Count; i++)
            PlaylistItems[i].IsPlaying = i == current;
    }

    private void OpenFolder()
    {
        var dlg = new Microsoft.Win32.OpenFolderDialog { Title = "选择 MIDI 文件夹", InitialDirectory = DefaultMidiFolder };
        if (dlg.ShowDialog() != true) return;
        _player.恶行易施(dlg.FolderName);
    }

    /// <summary>加载默认 MIDI 文件夹，不存在则自动创建。</summary>
    public void LoadDefaultFolder()
    {
        if (!System.IO.Directory.Exists(DefaultMidiFolder))
            System.IO.Directory.CreateDirectory(DefaultMidiFolder);
        _player.恶行易施(DefaultMidiFolder);
    }

    public void Dispose() => _player.Dispose();
}
