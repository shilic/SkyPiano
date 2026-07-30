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
/// <item>创建并管理 21 个键盘键位的视图模型集合。</item>
/// <item>通过 <see cref="DispatcherTimer"/> 定时轮询播放进度。</item>
/// <item>订阅 <see cref="MidiPlayerService"/> 的音符事件以驱动键盘高亮。</item>
/// <item>暴露播放控制命令（播放/暂停、上下曲、快进/快退）给 UI 绑定。</item>
/// <item>管理自动切歌：曲目播放完毕后自动切换到下一首。</item>
/// </list>
/// </summary>
public class MainViewModel : INotifyPropertyChanged, IDisposable
{
    /// <summary>MIDI 音频引擎，负责实际播放和音符事件。</summary>
    private readonly MidiPlayerService _player;

    /// <summary>播放列表管理器，负责曲目导航。</summary>
    private readonly PlaylistManager _playlist;

    /// <summary>UI 定时器，每 100ms 刷新播放进度。</summary>
    private readonly DispatcherTimer _timer;

    /// <summary>UI 线程同步上下文，在构造时捕获。</summary>
    private readonly SynchronizationContext _sync = SynchronizationContext.Current!;

    /// <summary>MIDI 音符编号 → 键位视图模型的快速查找字典。</summary>
    private readonly Dictionary<int, KeyNoteViewModel> _keyLookup = new();

    // ---- 属性变更事件 ----

    public event PropertyChangedEventHandler? PropertyChanged;

    // ---- 键位集合 ----

    /// <summary>
    /// 21 个钢琴键位的视图模型集合，供 UI 的 ItemsControl 绑定。
    /// </summary>
    public ObservableCollection<KeyNoteViewModel> KeyViewModels { get; } = new();

    // ---- 构造函数 ----

    /// <summary>
    /// 构造 MainViewModel，初始化音频引擎、播放列表管理器、21 键键盘和进度轮询定时器。
    /// 构造完成后需调用 <see cref="OpenFolder"/> 选择 MIDI 文件夹才能开始播放。
    /// </summary>
    public MainViewModel()
    {
        // 初始化音频引擎并注入 UI 线程上下文
        _player = new MidiPlayerService();
        _player.SetSyncContext(_sync);

        // 初始化播放列表并订阅曲目和播放完成事件
        _playlist = new PlaylistManager();
        _playlist.TrackChanged += OnTrackChanged;
        _player.Finished += OnTrackFinished;

        // 订阅音符事件用于键盘可视化
        _player.NotePlayed += OnNotePlayed;
        _player.NoteStopped += OnNoteStopped;

        // 初始化 21 个钢琴键位
        InitKeys();

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

    /// <summary>
    /// 初始化 21 个钢琴键位（3 个八度 × 7 个白键）。
    /// 下排 Z~M 对应 C3~B3，中排 A~J 对应 C4~B4，上排 Q~U 对应 C5~B5。
    /// </summary>
    private void InitKeys()
    {
        // 三组键位：下排、中排、上排，每组 7 个白键
        var keys = new (string label, int midi)[]
        {
            // 下排：C3~B3
            ("Z", 48), ("X", 50), ("C", 52), ("V", 53), ("B", 55), ("N", 57), ("M", 59),
            // 中排：C4~B4
            ("A", 60), ("S", 62), ("D", 64), ("F", 65), ("G", 67), ("H", 69), ("J", 71),
            // 上排：C5~B5
            ("Q", 72), ("W", 74), ("E", 76), ("R", 77), ("T", 79), ("Y", 81), ("U", 83),
        };

        foreach (var (label, midi) in keys)
        {
            var vm = new KeyNoteViewModel(label, midi);
            KeyViewModels.Add(vm);          // 加入 UI 绑定集合
            _keyLookup[midi] = vm;          // 加入快速查找字典（用于音符事件→键位联动）
        }
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

    // ---- 曲目管理 ----

    /// <summary>
    /// 当播放列表切换曲目时被回调。
    /// 加载新曲目到音频引擎并自动开始播放，同时更新 UI 的曲目名和索引。
    /// </summary>
    /// <param name="path">新曲目的完整文件路径。如果为 <c>null</c> 则忽略（列表为空）。</param>
    private void OnTrackChanged(string? path)
    {
        if (path == null) return;

        // 加载并播放新曲目
        _player.LoadFile(path);
        _player.Play();

        // 更新 UI 显示
        CurrentTrackName = System.IO.Path.GetFileNameWithoutExtension(path);
        TrackIndex = $"{_playlist.CurrentIndex + 1}/{_playlist.Count}";
        IsPlaying = true;
    }

    /// <summary>
    /// 当曲目播放完毕时被回调。自动切换到下一首曲目。
    /// </summary>
    private void OnTrackFinished()
    {
        _playlist.MoveNext();
    }

    // ---- 音符事件：驱动键盘可视化 ----

    /// <summary>
    /// 音频引擎报告有音符开始播放。查找对应的键位并设置高亮。
    /// </summary>
    /// <param name="midi">MIDI 音符编号（0-127）。</param>
    /// <param name="velocity">音符力度（0-127），当前版本未使用。</param>
    private void OnNotePlayed(int midi, byte velocity)
    {
        // 查找是否有键位对应此 MIDI 编号
        // 注意：只有 21 个白键对应的 MIDI 值才会被找到，黑键的 MIDI 值会被忽略
        if (_keyLookup.TryGetValue(midi, out var vm))
            vm.IsActive = true;
    }

    /// <summary>
    /// 音频引擎报告有音符停止播放。查找对应的键位并取消高亮。
    /// </summary>
    /// <param name="midi">MIDI 音符编号（0-127）。</param>
    private void OnNoteStopped(int midi)
    {
        if (_keyLookup.TryGetValue(midi, out var vm))
            vm.IsActive = false;
    }

    // ---- 文件夹选择 ----

    /// <summary>
    /// 打开文件夹选择对话框，让用户选择包含 MIDI 文件的文件夹。
    /// 选择后自动加载第一首曲目并开始播放。
    /// 在窗口加载完成后自动调用。
    /// </summary>
    public void OpenFolder()
    {
        // 使用 WPF 原生的 OpenFolderDialog（Windows Vista 及以上支持）
        var dlg = new Microsoft.Win32.OpenFolderDialog
        {
            Title = "选择 MIDI 文件夹",
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
        };

        // 用户确认选择后加载播放列表
        if (dlg.ShowDialog() == true)
            _playlist.LoadFromFolder(dlg.FolderName);
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
