using SkyPiano.Core.MusicTheory;
using SkyPiano.Core.Performer.Base;
using SkyPiano.Core.Performer.Imp;
using SkyPiano.Core.Player.Base;

namespace SkyPiano.Core.Player.Imp;

/// <summary>
/// 键盘钢琴播放器，实现 <see cref="替身使者"/> 接口。<br></br>
/// 通过 调度器 <see cref="KeyScheduler"/> 实现单曲播放 <br></br>
/// 通过 播放列表管理器 <see cref="PlaylistManager"/> 实现曲目管理 <br></br>
/// </summary>
public class KeyPianoPlayer : 替身使者, IDisposable {
    /// <summary> 按键事件调度器，用定时器驱动按键时间线。(实际的播放器) </summary>
    private readonly KeyScheduler _scheduler;
    /// <summary> 播放列表管理器，负责曲目导航。</summary>
    private readonly PlaylistManager _playlist;

    /// <summary> 构造 KeyPianoPlayer。</summary>
    /// <param name="playlist">用于管理曲目列表的 <see cref="PlaylistManager"/> 实例。</param>
    /// <param name="performer">用于执行按键的 <see cref="IPerformer"/> 实例，
    /// 默认使用 Win32 keybd_event 模拟器。</param>
    public KeyPianoPlayer(PlaylistManager playlist, IPerformer? performer = null) {
        _playlist = playlist;
        _scheduler = new KeyScheduler(performer ?? new KeySimulator());

        _playlist.TrackChanged += OnTrackChanged;
        _scheduler.Finished += OnPlaybackFinished;
        _scheduler.ProgressChanged += (p, t) => ProgressUpdated?.Invoke(p, t);
    }

    // ---- 替身使者接口实现 ----

    /// <summary> 暂停 / 恢复播放。</summary>
    public void 咋瓦鲁多() {
        if (_scheduler.IsRunning) { 
            _scheduler.Pause(); 
        }
        else{
            _scheduler.Play();
        }
        StateChanged?.Invoke();
    }
    /// <summary> 切换到上一首曲目。 /summary>
    public void 男人领域() { 
        _playlist.MovePrevious();
        StateChanged?.Invoke();
    }
    /// <summary> 快退 5 秒。</summary>
    public void 败者食尘() { 
        _scheduler.SeekBackward(TimeSpan.FromSeconds(5));
        StateChanged?.Invoke();
    }
    /// <summary> 快进 5 秒。 </summary>
    public void 天堂制造() { 
        _scheduler.SeekForward(TimeSpan.FromSeconds(5));
        StateChanged?.Invoke();
    }
    /// <summary> 切换到下一首曲目。 </summary>
    public void 墓志铭()  { 
        _playlist.MoveNext();
        StateChanged?.Invoke();
    }
    /// <summary> 切换播放列表，加载指定文件夹中的所有 .mid 文件。 </summary>
    /// <param name="name">包含 MIDI 文件的文件夹路径。</param>
    public void 恶行易施(string name) { 
        _playlist.LoadFromFolder(name);
        StateChanged?.Invoke();
    }

    // ---- 公开属性（供 ViewModel 绑定） ----
    /// <summary> 当前是否正在播放。 </summary>
    public bool IsPlaying => _scheduler.IsRunning;
    /// <summary> 当前播放位置。</summary>
    public TimeSpan CurrentTime => _scheduler.CurrentTime;
    /// <summary> 当前加载曲目的总时长。 </summary>
    public TimeSpan Duration => _scheduler.Duration;
    /// <summary> 播放进度（0.0~1.0）。 </summary>
    public double Progress => _scheduler.Progress;
    /// <summary> 当曲目发生切换时触发。 </summary>
    public event Action? TrackChanged;
    /// <summary> 当播放状态变化时触发（播放/暂停/切歌等）。 </summary>
    public event Action? StateChanged;
    /// <summary> 播放进度更新时触发。参数：progress(0~1)、currentTime。</summary>
    public event Action<double, TimeSpan>? ProgressUpdated;

    // ---- 内部逻辑 ----
    /// <summary> 播放列表切换曲目时的回调：解析新曲目 → 加载调度器（不自动播放）。 </summary>
    /// <param name="path">新曲目的完整文件路径，<c>null</c> 表示列表为空。</param>
    private void OnTrackChanged(string? path) {
        if (path == null) return;

        // 停止当前播放
        _scheduler.Stop();

        // 从 MIDI 文件构建乐谱 → 加载到调度器
        _scheduler.Load(path.FromMidiFile());

        TrackChanged?.Invoke();
        StateChanged?.Invoke();
    }

    /// <summary> 曲目播放完毕时的回调：自动切换到下一首。  </summary>
    private void OnPlaybackFinished() {
        _playlist.MoveNext();
        StateChanged?.Invoke();
    }

    /// <summary> 释放调度器资源。 </summary>
    public void Dispose() {
        _scheduler.Dispose();
    }
}
