using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SkyPiano.Core.Player.Imp;

namespace SkyPiano.ViewModel;

/// <summary>
/// 主窗口的视图模型，作为 UI 和播放引擎之间的桥梁。
/// 负责：
/// <list type="bullet">
/// <item>启动时自动加载默认 MIDI 文件夹（不存在则创建）。</item>
/// <item>管理播放列表（曲目集合、当前选中曲目标记）。</item>
/// <item>通过 <see cref="DispatcherTimer"/> 定时轮询播放进度。</item>
/// <item>暴露播放控制命令给 UI 绑定。</item>
/// <item>管理自动切歌：曲目播放完毕后自动切换到下一首。</item>
/// </list>
/// </summary>
public class MainViewModel : INotifyPropertyChanged, IDisposable {
    /// <summary> 键盘钢琴播放器，将 MIDI 转为键盘按键输出。 </summary>
    private readonly KeyPianoPlayer _player;

    /// <summary>播放列表管理器，负责曲目导航和文件列表。</summary>
    private readonly PlaylistManager _playlist;

    /// <summary>UI 定时器，每 100ms 刷新播放进度。</summary>
    private readonly DispatcherTimer _timer;

    /// <summary>  默认 MIDI 文件夹路径：用户文档目录下的 "SkyPiano/MIDI"。  </summary>
    private static readonly string DefaultMidiFolder = System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "SkyPiano", "MIDI");
    /// <summary> 实现 INotifyPropertyChanged 接口  </summary>
    public event PropertyChangedEventHandler? PropertyChanged;
    /// <summary>  播放列表中的所有曲目项，供 UI 的 ListBox 绑定。  </summary>
    public ObservableCollection<TrackItemViewModel> PlaylistItems { get; } = new();

    // ---- 构造函数 ----

    /// <summary>
    /// 构造 MainViewModel，初始化播放器、播放列表和进度轮询定时器。
    /// </summary>
    public MainViewModel() {
        // 播放列表管理器
        _playlist = new PlaylistManager();

        // 键盘钢琴播放器（默认使用 Win32 keybd_event 模拟按键）
        _player = new KeyPianoPlayer(_playlist);

        _playlist.TrackChanged += OnTrackChanged;
        _player.StateChanged += () => Dispatcher.CurrentDispatcher.Invoke(RefreshState);

        // 进度轮询定时器：每 100ms 刷新进度条和时间显示
        _timer = new DispatcherTimer(
            TimeSpan.FromMilliseconds(100),
            DispatcherPriority.Normal,
            (_, _) => {
                OnPropertyChanged(nameof(Progress));
                OnPropertyChanged(nameof(CurrentTimeDisplay));
            },
            Dispatcher.CurrentDispatcher);
        _timer.Start();
    }

    // ---- UI 绑定属性 ----

    /// <summary>
    /// 播放进度，0.0（开头）到 1.0（结尾）。
    /// </summary>
    public double Progress => _player.Progress;

    /// <summary>
    /// 当前播放时间的格式化字符串，格式为 "mm:ss"。
    /// </summary>
    public string CurrentTimeDisplay =>
        $"{(int)_player.CurrentTime.TotalMinutes:D2}:{_player.CurrentTime.Seconds:D2}";

    /// <summary>
    /// 曲目总时长的格式化字符串，格式为 "mm:ss"。
    /// </summary>
    public string DurationDisplay =>
        $"{(int)_player.Duration.TotalMinutes:D2}:{_player.Duration.Seconds:D2}";

    /// <summary>当前曲目名后备字段。</summary>
    private string _currentTrackName = "";

    /// <summary>
    /// 当前播放曲目的文件名（不含扩展名）。
    /// </summary>
    public string CurrentTrackName
    {
        get => _currentTrackName;
        set { _currentTrackName = value; OnPropertyChanged(nameof(CurrentTrackName)); }
    }

    /// <summary>曲目索引后备字段，如 "3/12"。</summary>
    private string _trackIndex = "";

    /// <summary>
    /// 曲目索引显示字符串，格式为 "当前序号/总数"，如 "3/12"。
    /// </summary>
    public string TrackIndex
    {
        get => _trackIndex;
        set { _trackIndex = value; OnPropertyChanged(nameof(TrackIndex)); }
    }

    /// <summary>播放状态后备字段。</summary>
    private bool _isPlaying;

    /// <summary>
    /// 当前是否正在播放。用于切换播放/暂停按钮的图标。
    /// </summary>
    public bool IsPlaying {
        get => _isPlaying;
        set { _isPlaying = value; OnPropertyChanged(nameof(IsPlaying)); }
    }

    /// <summary>ListBox 选中索引的后备字段。</summary>
    private int _selectedTrackIndex = -1;

    /// <summary>
    /// 播放列表中当前选中的曲目索引（从 0 开始，-1 表示无选中）。
    /// 与 ListBox 双向绑定，用户点击列表项时自动切换曲目。
    /// </summary>
    public int SelectedTrackIndex {
        get => _selectedTrackIndex;
        set  {
            if (_selectedTrackIndex == value) return;

            if (value >= 0 && value < _playlist.Count && value != _playlist.CurrentIndex)
                _playlist.SelectTrack(value);

            _selectedTrackIndex = value;
            OnPropertyChanged(nameof(SelectedTrackIndex));
        }
    }

    // ---- 播放控制命令 ----

    //RoutedCommand
    //CommandBinding
    /// <summary>
    /// 播放 / 暂停命令。
    /// </summary>
    public ICommand PlayPauseCommand => new RelayCommand(() => {
        _player.咋瓦鲁多();
        RefreshState();
    });

    /// <summary> 下一首命令。  </summary>
    public ICommand NextCommand => new RelayCommand(() => _player.墓志铭());

    /// <summary> 上一首命令。  </summary>
    public ICommand PrevCommand => new RelayCommand(() => _player.男人领域());

    /// <summary> 快退命令（后退 5 秒）。 </summary>
    public ICommand RewindCommand => new RelayCommand(() => _player.败者食尘());

    /// <summary>  快进命令（前进 5 秒）。 </summary>
    public ICommand FastForwardCommand => new RelayCommand(() => _player.天堂制造());

    /// <summary> 切换 MIDI 文件夹命令。 </summary>
    public ICommand SwitchFolderCommand => new RelayCommand(OpenFolder);

    // ---- 内部方法 ----

    //[RelayCommand]
    /// <summary>
    /// 播放列表切换曲目时的回调。重建播放列表 UI 集合并更新高亮。
    /// </summary>
    /// <param name="path">新曲目文件路径，<c>null</c> 表示列表为空。</param>
    private void OnTrackChanged(string? path) {
        if (path == null) return;

        // 重建 UI 播放列表（仅在首次加载或切换文件夹时重建）
        if (PlaylistItems.Count != _playlist.Count ||
            (PlaylistItems.Count > 0 && PlaylistItems[0].FilePath != _playlist.Tracks[0])) {
            RebuildPlaylistItems();
        }

        // 更新信息栏和高亮
        CurrentTrackName = System.IO.Path.GetFileNameWithoutExtension(path);
        RefreshState();
    }

    /// <summary>
    /// 刷新播放状态到 UI（IsPlaying、TrackIndex、曲目高亮、选中索引）。
    /// </summary>
    private void RefreshState() {
        IsPlaying = _player.IsPlaying;
        TrackIndex = _playlist.Count > 0 ? $"{_playlist.CurrentIndex + 1}/{_playlist.Count}" : "";

        // 同步选中索引和高亮
        //_selectedTrackIndex = _playlist.CurrentIndex;
        //OnPropertyChanged(nameof(SelectedTrackIndex));
        SelectedTrackIndex = _playlist.CurrentIndex;
        UpdateTrackHighlight();
    }

    /// <summary>
    /// 遍历所有曲目项，将仅当前播放的索引设为高亮。
    /// </summary>
    private void UpdateTrackHighlight()
    {
        var current = _playlist.CurrentIndex;
        for (var i = 0; i < PlaylistItems.Count; i++)
            PlaylistItems[i].IsPlaying = i == current;
    }

    /// <summary>
    /// 根据播放列表重建 UI 曲目集合。
    /// </summary>
    private void RebuildPlaylistItems()
    {
        PlaylistItems.Clear();
        foreach (var trackPath in _playlist.Tracks)
        {
            var fileName = System.IO.Path.GetFileNameWithoutExtension(trackPath);
            PlaylistItems.Add(new TrackItemViewModel(fileName, trackPath));
        }
    }

    /// <summary>
    /// 加载默认 MIDI 文件夹（Documents/SkyPiano/MIDI）。
    /// 文件夹不存在则自动创建并加载。
    /// </summary>
    public void LoadDefaultFolder() {
        if (!System.IO.Directory.Exists(DefaultMidiFolder))
            System.IO.Directory.CreateDirectory(DefaultMidiFolder);

        _player.恶行易施(DefaultMidiFolder);
    }

    /// <summary>
    /// 弹出文件夹选择对话框，让用户手动切换到另一个文件夹。
    /// </summary>
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

    // ---- INotifyPropertyChanged 辅助 ----

    private void OnPropertyChanged(string name) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    // ---- IDisposable ----

    /// <summary>
    /// 释放资源：停止定时器、释放播放器。
    /// </summary>
    public void Dispose()
    {
        _timer.Stop();
        _player.Dispose();
    }
}

/// <summary>
/// 轻量级 ICommand 实现。
/// </summary>
public class RelayCommand : ICommand
{
    private readonly Action _execute;

    public RelayCommand(Action execute) => _execute = execute;
    public bool CanExecute(object? parameter) => true;
    public void Execute(object? parameter) => _execute();

#pragma warning disable CS0067
    public event EventHandler? CanExecuteChanged;
#pragma warning restore CS0067
}
