using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SkyPiano.Core.Player.Base;
using SkyPiano.Core.Player.Imp;

namespace SkyPiano.ViewModel;

/// <summary>
/// 主窗口的视图模型。通过构造函数注入 <see cref="IPianoPlayer"/>，默认使用 <see cref="KeyPianoPlayer"/>。
/// 使用 CommunityToolkit.Mvvm 源生成器实现属性通知和命令绑定。
/// </summary>
public partial class MainViewModel : ObservableObject, IDisposable {
    #region 内部只读字段
    /// <summary>键盘钢琴播放器接口，通过构造函数注入，默认使用 KeyPianoPlayer。</summary>
    private readonly IPianoPlayer _player;
    /// <summary> 默认 MIDI 文件夹路径：用户文档目录下的 "SkyPiano/MIDI"。</summary>
    private static readonly string DefaultMidiFolder = System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "SkyPiano", "MIDI");
    #endregion 内部只读字段
    #region 被观察的属性（UI 绑定）
    /// <summary> 播放列表中的所有曲目项，供 UI 的 ListBox 绑定。</summary>
    public ObservableCollection<TrackItemViewModel> PlaylistItems { get; } = [];
    /// <summary> 当前播放曲目的文件名（不含扩展名）。</summary>
    [ObservableProperty]
    private string _currentTrackName = "";
    /// <summary> 曲目索引显示字符串，格式为"当前序号/总数"，如"3/12"。</summary>
    [ObservableProperty]
    private string _trackIndex = "";
    /// <summary> 当前是否正在播放。用于切换播放/暂停按钮的图标。</summary>
    [ObservableProperty]
    private bool _isPlaying;
    /// <summary> 播放列表中当前选中的曲目索引（从 0 开始，-1 为无选中）。</summary>
    [ObservableProperty]
    private int _selectedTrackIndex = -1;
    /// <summary> 播放进度，0.0（开头）到 1.0（结尾）。</summary>
    [ObservableProperty]
    private double _progress;
    /// <summary> 当前播放时间的格式化字符串，格式为"mm:ss"。</summary>
    [ObservableProperty]
    private string _currentTimeDisplay = "";
    /// <summary> 曲目总时长的格式化字符串，格式为"mm:ss"。</summary>
    [ObservableProperty]
    private string _durationDisplay = "";
    #endregion 被观察的属性（UI 绑定）
    #region 被观察的UI状态
    /// <summary>
    /// 双向绑定<br></br>
    /// 当用户通过 ListBox 选中曲目时由源生成器回调。<br></br>
    /// 索引有效且不同于当前索引时切换到对应曲目。<br></br>
    /// </summary>
    partial void OnSelectedTrackIndexChanged(int value) {
        _player.恶行易施(value);
    }
    partial void OnProgressChanged(double value) {
        _player.时间删除(value);
    }
    #endregion 被观察的UI状态
    #region 构造 MainViewModel
    /// <summary>
    /// 构造 MainViewModel，创建播放器并注册事件回调。
    /// </summary>
    public MainViewModel() {
        _player = new KeyPianoPlayer();
        // 注册三个 Model 层事件
        _player.TrackChanged += OnTrackChanged;
        _player.StateChanged += RefreshState;
        _player.ProgressUpdated += RefreshProgress;
        LoadDefaultFolder();
    }
    #endregion 构造 MainViewModel
    #region 过时的回调方法（旧版事件）
    /// <summary> 播放状态变更（播放/暂停/切歌）的回调。</summary>
    [Obsolete("Use RefreshState() instead.")]
    private void OnStateChanged() {
       Application.Current.Dispatcher.Invoke(RefreshState);
    }
    /// <summary> 播放进度更新的回调。从参数中直接取值更新进度条和时间显示。</summary>
    /// <param name="progress">当前播放进度（0.0~1.0）。</param>
    /// <param name="time">当前播放位置。</param>
    [Obsolete("Use RefreshProgress(double progress, TimeSpan time) instead.")]
    private void OnProgressUpdated(double progress, TimeSpan time) {
        Application.Current.Dispatcher.Invoke(RefreshProgress);
    }
    #endregion 过时的回调方法（旧版事件）
    // ---- RelayCommand ----
    #region 继电器命令（RelayCommand）绑定到 UI 按钮
    /// <summary>播放 / 暂停命令。切换播放状态。</summary>
    [RelayCommand]
    private void PlayPause() => _player.咋瓦鲁多();
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
    #endregion 继电器命令（RelayCommand）绑定到 UI 按钮
    // ---- 内部方法 ----
    #region 状态变化的回调方法（由 Model 层事件触发）
    /// <summary>
    /// 播放列表切换曲目时的回调。重建 UI 曲目集合并更新标题栏。
    /// 仅在曲目列表实际发生变化时才重建 ObservableCollection（避免不必要的 UI 刷新）。
    /// </summary>
    /// <param name="path">新曲目文件路径（null 表示列表为空）。</param>
    private void OnTrackChanged(string? path){
        if (path == null) return;

        // 仅在文件夹切换时重建列表，单曲切换只更新高亮
        if (PlaylistItems.Count != _player.TrackCount ||
            (PlaylistItems.Count > 0 && PlaylistItems[0].FilePath != _player.Tracks[0])) {
            PlaylistItems.Clear();
            foreach (var trackPath in _player.Tracks) {
                var fileName = System.IO.Path.GetFileNameWithoutExtension(trackPath);
                PlaylistItems.Add(new TrackItemViewModel(fileName, trackPath));
            }
        }

        CurrentTrackName = System.IO.Path.GetFileNameWithoutExtension(path);
        RefreshState();
    }
    /// <summary>
    /// 刷新播放状态到 UI 绑定属性：播放状态、曲目索引、选中项、时长、列表高亮。
    /// 在 StateChanged 事件和 TrackChanged 事件中调用。
    /// </summary>
    private void RefreshState() {
        IsPlaying = _player.IsPlaying;
        TrackIndex = _player.TrackCount > 0 ? $"{_player.CurrentTrackIndex + 1}/{_player.TrackCount}" : "";
        SelectedTrackIndex = _player.CurrentTrackIndex;
        DurationDisplay = $"{(int)_player.Duration.TotalMinutes:D2}:{_player.Duration.Seconds:D2}";

        // 更新播放列表高亮
        var current = _player.CurrentTrackIndex;
        for (var i = 0; i < PlaylistItems.Count; i++)
            PlaylistItems[i].IsPlaying = i == current;
    }
    private void RefreshProgress(double progress, TimeSpan time) {
        Progress = progress;
        CurrentTimeDisplay = $"{(int)time.TotalMinutes:D2}:{time.Seconds:D2}";
    }
    #endregion  状态变化的回调方法（由 Model 层事件触发）
    #region 内部方法
    /// <summary>
    /// 弹出文件夹选择对话框，切换到用户选择的 MIDI 文件夹。
    /// </summary>
    private void OpenFolder() {
        var dlg = new Microsoft.Win32.OpenFolderDialog {
            Title = "选择 MIDI 文件夹",
            InitialDirectory = DefaultMidiFolder
        };
        if (dlg.ShowDialog() != true) { return; }
        _player.恶行易施(dlg.FolderName);
    }
    /// <summary>
    /// 加载默认 MIDI 文件夹（Documents/SkyPiano/MIDI），不存在则自动创建。
    /// 在窗口加载完成时由 MainWindow 调用。
    /// </summary>
    private void LoadDefaultFolder() {
        if (!Directory.Exists(DefaultMidiFolder)){
            Directory.CreateDirectory(DefaultMidiFolder);
        }
        _player.恶行易施(DefaultMidiFolder);
    }
    #endregion 内部方法
    /// <summary>释放播放器资源。</summary>
    public void Dispose() => _player.Dispose();
}
