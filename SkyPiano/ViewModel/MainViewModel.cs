using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using System.Windows.Threading;
using SkyPiano.Core.Player.Imp;

namespace SkyPiano.ViewModel;

/// <summary>
/// 主窗口的视图模型，作为 UI 和音频引擎之间的桥梁。
/// 负责：
/// <list type="bullet">
/// <item>启动时自动加载默认 MIDI 文件夹（不存在则创建）。</item>
/// <item>管理播放列表（曲目集合、当前选中曲目标记）。</item>
/// <item>通过 <see cref="DispatcherTimer"/> 定时轮询播放进度。</item>
/// <item>暴露播放控制命令（播放/暂停、上下曲、快进/快退）给 UI 绑定。</item>
/// <item>管理自动切歌：曲目播放完毕后自动切换到下一首。</item>
/// </list>
/// </summary>
public class MainViewModel : INotifyPropertyChanged, IDisposable
{
    /// <summary>MIDI 音频引擎，负责实际播放。</summary>
    private readonly MidiPlayerService _player;

    /// <summary>播放列表管理器，负责曲目导航和文件列表。</summary>
    private readonly PlaylistManager _playlist;

    /// <summary>UI 定时器，每 100ms 刷新播放进度。</summary>
    private readonly DispatcherTimer _timer;

    /// <summary>UI 线程同步上下文，在构造时捕获。</summary>
    private readonly SynchronizationContext _sync = SynchronizationContext.Current!;

    /// <summary>
    /// 默认 MIDI 文件夹路径：用户文档目录下的 "SkyPiano/MIDI"。
    /// </summary>
    private static readonly string DefaultMidiFolder = System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "SkyPiano", "MIDI");

    // ---- 属性变更事件 ----

    public event PropertyChangedEventHandler? PropertyChanged;

    // ---- 播放列表集合 ----

    /// <summary>
    /// 播放列表中的所有曲目项，供 UI 的 ListBox 绑定。
    /// </summary>
    public ObservableCollection<TrackItemViewModel> PlaylistItems { get; } = new();

    // ---- 构造函数 ----

    /// <summary>
    /// 构造 MainViewModel，初始化音频引擎、播放列表管理器和进度轮询定时器。
    /// </summary>
    public MainViewModel()
    {
        // 初始化音频引擎并注入 UI 线程上下文
        _player = new MidiPlayerService();
        _player.SetSyncContext(_sync);

        // 初始化播放列表并订阅曲目切换和播放完成事件
        _playlist = new PlaylistManager();
        _playlist.TrackChanged += OnTrackChanged;
        _player.Finished += OnTrackFinished;

        // 启动定时器，每 100ms 刷新进度条和时间显示
        _timer = new DispatcherTimer(
            TimeSpan.FromMilliseconds(100),
            DispatcherPriority.Normal,
            (_, _) =>
            {
                OnPropertyChanged(nameof(Progress));
                OnPropertyChanged(nameof(CurrentTimeDisplay));
            },
            Dispatcher.CurrentDispatcher);
        _timer.Start();
    }

    // ---- UI 绑定属性 ----

    /// <summary>
    /// 播放进度，0.0（开头）到 1.0（结尾）。
    /// 通过定时器每 100ms 更新，供进度条 Slider 绑定。
    /// </summary>
    public double Progress => _player.Duration.TotalSeconds > 0
        ? _player.CurrentTime.TotalSeconds / _player.Duration.TotalSeconds
        : 0;

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
        set
        {
            _currentTrackName = value;
            OnPropertyChanged(nameof(CurrentTrackName));
        }
    }

    /// <summary>曲目索引后备字段，如 "3/12"。</summary>
    private string _trackIndex = "";

    /// <summary>
    /// 曲目索引显示字符串，格式为 "当前序号/总数"，如 "3/12"。
    /// </summary>
    public string TrackIndex
    {
        get => _trackIndex;
        set
        {
            _trackIndex = value;
            OnPropertyChanged(nameof(TrackIndex));
        }
    }

    /// <summary>播放状态后备字段。</summary>
    private bool _isPlaying;

    /// <summary>
    /// 当前是否正在播放。用于切换播放/暂停按钮的图标。
    /// </summary>
    public bool IsPlaying
    {
        get => _isPlaying;
        set
        {
            _isPlaying = value;
            OnPropertyChanged(nameof(IsPlaying));
        }
    }

    /// <summary>ListBox 选中索引的后备字段。</summary>
    private int _selectedTrackIndex = -1;

    /// <summary>
    /// 播放列表中当前选中的曲目索引（从 0 开始，-1 表示无选中）。
    /// 与 ListBox 双向绑定，用户点击列表项时自动切换曲目。
    /// </summary>
    public int SelectedTrackIndex
    {
        get => _selectedTrackIndex;
        set
        {
            if (_selectedTrackIndex == value) return;

            // 索引有效且不同于当前播放索引时，切换到对应曲目
            if (value >= 0 && value < _playlist.Count && value != _playlist.CurrentIndex)
            {
                _playlist.SelectTrack(value);
            }

            _selectedTrackIndex = value;
            OnPropertyChanged(nameof(SelectedTrackIndex));
        }
    }

    // ---- 播放控制命令 ----

    /// <summary>播放/暂停命令的后备字段（惰性初始化）。</summary>
    private ICommand? _playPauseCommand;

    /// <summary>
    /// 播放 / 暂停命令。切换播放状态并同步更新 <see cref="IsPlaying"/>。
    /// </summary>
    public ICommand PlayPauseCommand => _playPauseCommand ??= new RelayCommand(() =>
    {
        _player.TogglePlayPause();
        IsPlaying = _player.IsPlaying;
    });

    /// <summary>下一首命令的后备字段。</summary>
    private ICommand? _nextCommand;

    /// <summary>
    /// 下一首命令。切换到播放列表中的下一首曲目。
    /// </summary>
    public ICommand NextCommand => _nextCommand ??= new RelayCommand(() => _playlist.MoveNext());

    /// <summary>上一首命令的后备字段。</summary>
    private ICommand? _prevCommand;

    /// <summary>
    /// 上一首命令。切换到播放列表中的上一首曲目。
    /// </summary>
    public ICommand PrevCommand => _prevCommand ??= new RelayCommand(() => _playlist.MovePrevious());

    /// <summary>快退命令的后备字段。</summary>
    private ICommand? _rewindCommand;

    /// <summary>
    /// 快退命令。将播放位置向后跳转 5 秒。
    /// </summary>
    public ICommand RewindCommand => _rewindCommand ??= new RelayCommand(
        () => _player.SeekBackward(TimeSpan.FromSeconds(5)));

    /// <summary>快进命令的后备字段。</summary>
    private ICommand? _ffCommand;

    /// <summary>
    /// 快进命令。将播放位置向前跳转 5 秒。
    /// </summary>
    public ICommand FastForwardCommand => _ffCommand ??= new RelayCommand(
        () => _player.SeekForward(TimeSpan.FromSeconds(5)));

    /// <summary>切换文件夹命令的后备字段。</summary>
    private ICommand? _switchFolderCommand;

    /// <summary>
    /// 切换文件夹命令。弹出文件夹选择对话框，让用户切换到其他 MIDI 文件夹。
    /// </summary>
    public ICommand SwitchFolderCommand => _switchFolderCommand ??= new RelayCommand(OpenFolder);

    // ---- 曲目管理 ----

    /// <summary>
    /// 当播放列表切换曲目时被回调。
    /// 加载新曲目到音频引擎并自动开始播放，同时更新 UI 的曲目名、索引和高亮。
    /// </summary>
    /// <param name="path">新曲目的完整文件路径。如果为 <c>null</c> 则忽略（列表为空）。</param>
    private void OnTrackChanged(string? path)
    {
        if (path == null) return;

        // 加载并播放新曲目
        _player.LoadFile(path);
        _player.Play();

        // 更新顶部信息栏
        CurrentTrackName = System.IO.Path.GetFileNameWithoutExtension(path);
        TrackIndex = $"{_playlist.CurrentIndex + 1}/{_playlist.Count}";
        IsPlaying = true;

        // 同步 ListBox 选中项和列表高亮
        _selectedTrackIndex = _playlist.CurrentIndex;
        OnPropertyChanged(nameof(SelectedTrackIndex));
        UpdateTrackHighlight();
    }

    /// <summary>
    /// 当曲目播放完毕时被回调。自动切换到下一首曲目。
    /// </summary>
    private void OnTrackFinished()
    {
        _playlist.MoveNext();
    }

    /// <summary>
    /// 遍历所有曲目项，将 <see cref="TrackItemViewModel.IsPlaying"/> 更新为
    /// 仅当前播放的索引为 <c>true</c>，其余为 <c>false</c>。
    /// </summary>
    private void UpdateTrackHighlight()
    {
        var current = _playlist.CurrentIndex;
        for (var i = 0; i < PlaylistItems.Count; i++)
            PlaylistItems[i].IsPlaying = i == current;
    }

    // ---- 文件夹管理 ----

    /// <summary>
    /// 加载默认 MIDI 文件夹（Documents/SkyPiano/MIDI）。
    /// 如果文件夹不存在则自动创建。
    /// 在窗口加载完成时自动调用。
    /// </summary>
    public void LoadDefaultFolder()
    {
        // 确保默认文件夹存在，不存在则创建
        if (!System.IO.Directory.Exists(DefaultMidiFolder))
            System.IO.Directory.CreateDirectory(DefaultMidiFolder);

        LoadFolder(DefaultMidiFolder);
    }

    /// <summary>
    /// 弹出文件夹选择对话框，让用户手动切换到另一个文件夹。
    /// 选择后加载播放列表并自动播放第一首。
    /// </summary>
    private void OpenFolder()
    {
        var dlg = new Microsoft.Win32.OpenFolderDialog
        {
            Title = "选择 MIDI 文件夹",
            InitialDirectory = DefaultMidiFolder
        };

        if (dlg.ShowDialog() != true) return;
        LoadFolder(dlg.FolderName);
    }

    /// <summary>
    /// 加载指定文件夹中所有 .mid 文件，重建播放列表并自动开始播放第一首个。
    /// </summary>
    /// <param name="folderPath">包含 MIDI 文件的文件夹路径。</param>
    private void LoadFolder(string folderPath)
    {
        // 加载播放列表（内部自动选中第一首并触发 TrackChanged → OnTrackChanged → 播放）
        _playlist.LoadFromFolder(folderPath);

        // 根据新加载的曲目列表重建 UI 列表集合
        PlaylistItems.Clear();
        foreach (var trackPath in _playlist.Tracks)
        {
            // 文件名不含扩展名，作为列表显示文本
            var fileName = System.IO.Path.GetFileNameWithoutExtension(trackPath);
            PlaylistItems.Add(new TrackItemViewModel(fileName, trackPath));
        }

        // LoadFromFolder 内部已触发 TrackChanged → OnTrackChanged 会处理高亮和播放
    }

    // ---- INotifyPropertyChanged 辅助 ----

    /// <summary>
    /// 触发属性变更通知，通知 WPF 绑定系统刷新对应控件。
    /// </summary>
    /// <param name="name">变更的属性名称。</param>
    private void OnPropertyChanged(string name) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    // ---- IDisposable ----

    /// <summary>
    /// 释放资源：停止定时器、释放音频引擎。
    /// 在窗口关闭时调用。
    /// </summary>
    public void Dispose()
    {
        _timer.Stop();
        _player.Dispose();
    }
}

/// <summary>
/// 轻量级 ICommand 实现，用于将无参数 Action 委托包装为 WPF 可绑定命令。
/// 所有命令均始终可执行（<see cref="CanExecute"/> 恒返回 <c>true</c>）。
/// </summary>
public class RelayCommand : ICommand
{
    /// <summary>命令执行逻辑的委托。</summary>
    private readonly Action _execute;

    /// <summary>
    /// 构造 RelayCommand。
    /// </summary>
    /// <param name="execute">命令执行时调用的无参数委托。</param>
    public RelayCommand(Action execute) => _execute = execute;

    /// <summary>
    /// 判断命令当前是否可执行。始终返回 <c>true</c>。
    /// </summary>
    /// <param name="parameter">命令参数，不使用。</param>
    /// <returns>始终返回 <c>true</c>。</returns>
    public bool CanExecute(object? parameter) => true;

    /// <summary>
    /// 执行命令，调用构造时传入的委托。
    /// </summary>
    /// <param name="parameter">命令参数，不使用。</param>
    public void Execute(object? parameter) => _execute();

    /// <summary>
    /// 当命令的可执行状态发生变化时引发。
    /// 注意：此命令始终可执行，因此此事件从未被引发。
    /// 保留此事件仅为满足 <see cref="ICommand"/> 接口要求。
    /// </summary>
#pragma warning disable CS0067
    public event EventHandler? CanExecuteChanged;
#pragma warning restore CS0067
}
