using System.Diagnostics;
using System.IO;
using SkyPiano.Core.MusicTheory;
using SkyPiano.Core.Performer.Base;
using SkyPiano.Core.Performer.Imp;
using SkyPiano.Core.Player.Base;
using SkyPiano.SkyPiano.Core.MusicTheory;

namespace SkyPiano.Core.Player.Imp;

/// <summary>
/// 键盘钢琴播放器，实现 <see cref="替身使者"/> 接口。<br/>
/// 内部集成播放调度器和播放列表管理，作为 ViewModel 唯一的 Model 层依赖。
/// </summary>
/// <remarks> 构造 KeyPianoPlayer。</remarks>
/// <param name="performer">按键执行器，默认 Win32 keybd_event。</param>
public class KeyPianoPlayer(IPerformer? performer = null) : 替身使者, IDisposable {
    // ==================== 调度器字段 ====================
    /// <summary> 当前加载的乐谱。</summary>
    private MyScore? _score;
    /// <summary> 当前播放序号。</summary>
    private int _index;
    /// <summary> 临时工：记录每次 Play() 开始后经过的时间。</summary>
    private readonly Stopwatch _stopwatch = new();
    /// <summary> 调度定时器，约 10ms 间隔。</summary>
    private Timer? _timer;
    /// <summary> 当前按下的键集合。</summary>
    private readonly HashSet<MyNote> _pressedKeys = [];
    /// <summary> 持久记忆：暂停时的已播放时间（微秒）。</summary>
    private long _pausedElapsedUs;
    /// <summary> 按键执行器。演奏者。 </summary>
    private readonly IPerformer _performer = performer ?? new KeySimulator();

    // ==================== 播放列表字段 ====================
    /// <summary>MIDI 文件路径列表。</summary>
    private string[] _tracks = []; 
    /// <summary>当前曲目索引，-1 为空。</summary>
    private int _trackIndex = -1;

    // ==================== 事件 ==================== 
    /// <summary> 曲目切换时触发。参数为新的文件路径（null 表示列表为空）。</summary>
    public event Action<string?>? TrackChanged; 
    /// <summary> 播放状态变化时触发。</summary>
    public event Action? StateChanged; 
    /// <summary> 播放进度更新时触发。参数：progress(0~1)、currentTime。</summary>
    public event Action<double, TimeSpan>? ProgressUpdated;

    // ==================== 属性 ====================
    /// <summary> 当前是否正在播放。</summary>
    public bool IsPlaying => _timer != null;
    /// <summary> 当前播放位置。</summary>
    public TimeSpan CurrentTime => TimeSpan.FromMicroseconds(GetElapsedMicroseconds());
    /// <summary> 曲目总时长。</summary>
    public TimeSpan Duration => _score?.Duration ?? TimeSpan.Zero;
    /// <summary> 播放进度（0.0~1.0）。</summary>
    public double Progress {
        get {
            var dur = _score?.Duration.TotalMicroseconds ?? 0;
            return dur > 0 ? (double)GetElapsedMicroseconds() / dur : 0;
        }
    } 
    /// <summary>播放列表曲目总数。</summary>
    public int TrackCount => _tracks.Length;

    /// <summary>当前曲目索引（0 开始，-1 为空）。</summary>
    public int CurrentTrackIndex => _trackIndex;

    /// <summary>当前曲目路径（null 为空）。</summary>
    public string? CurrentTrackPath =>
        _trackIndex >= 0 && _trackIndex < _tracks.Length ? _tracks[_trackIndex] : null;

    /// <summary>曲目列表（只读）。</summary>
    public IReadOnlyList<string> Tracks => _tracks;

    // ==================== 替身使者接口 ====================

    /// <summary> 暂停 / 恢复播放。</summary>
    public void 咋瓦鲁多() {
        if (_timer != null) {
            _timer?.Dispose();
            _timer = null;
            _stopwatch.Stop();
            _pausedElapsedUs += _stopwatch.ElapsedMilliseconds * 1000;
            ReleaseAll();
        }
        else
        {
            Play();
        }
        StateChanged?.Invoke();
    }

    /// <summary>切换到上一个个。</summary>
    public void 男人领域() { SelectTrack((_trackIndex - 1 + _tracks.Length) % _tracks.Length); StateChanged?.Invoke(); }

    /// <summary>快退 5 秒。</summary>
    public void 败者食尘() => Seek(-TimeSpan.FromSeconds(5));

    /// <summary>快进 5 秒。</summary>
    public void 天堂制造() => Seek(TimeSpan.FromSeconds(5));

    /// <summary>切换到下一个个。</summary>
    public void 墓志铭() { SelectTrack((_trackIndex + 1) % _tracks.Length); StateChanged?.Invoke(); }

    /// <summary>切换播放列表文件夹。</summary>
    public void 恶行易施(string name)
    {
        _trackIndex = -1;
        _tracks = Directory.GetFiles(name, "*.mid", SearchOption.TopDirectoryOnly).OrderBy(f => f).ToArray();
        if (_tracks.Length > 0)
            SelectTrack(0);
        StateChanged?.Invoke();
    }

    /// <summary>选中指定索引的曲目。</summary>
    public void SelectTrack(int index)
    {
        if (index < 0 || index >= _tracks.Length) return;
        if (index == _trackIndex) return;

        // 弃置当前播放
        StopScheduler();

        _trackIndex = index;
        var path = _tracks[index];
        TrackChanged?.Invoke(path);

        // 解析曲目
        _score = path.FromMidiFile();
        _index = 0;
        _pressedKeys.Clear();
        _pausedElapsedUs = 0;
    }

    /// <summary>强制开始播放（已播放则无操作）。</summary>
    public void RequestPlay()
    {
        if (_timer == null)
            Play();
    }

    // ==================== 调度器核心 ====================

    private void Play()
    {
        if (_score == null || _score.Events.Count == 0) return;

        long resumeOffset = _pausedElapsedUs;
        _stopwatch.Restart();
        _timer?.Dispose();
        _timer = new Timer(_ => Tick(resumeOffset), null, 0, 10);
    }

    private void StopScheduler()
    {
        _timer?.Dispose();
        _timer = null;
        _stopwatch.Reset();
        _index = 0;
        _pausedElapsedUs = 0;
        ReleaseAll();
    }

    private void Seek(TimeSpan delta)
    {
        bool wasPlaying = _timer != null;

        // 暂停
        _timer?.Dispose();
        _timer = null;
        _stopwatch.Stop();
        _pausedElapsedUs += _stopwatch.ElapsedMilliseconds * 1000;
        ReleaseAll();

        if (_score == null) return;

        long deltaUs = (long)delta.TotalMicroseconds;
        long newElapsed;

        if (deltaUs >= 0)
        {
            // 快进
            newElapsed = Math.Min(_pausedElapsedUs + deltaUs, (long)_score.Duration.TotalMicroseconds);
            while (_index < _score.Events.Count && _score.Events[_index].TimeUs <= newElapsed)
                _index++;
        }
        else
        {
            // 快退
            newElapsed = Math.Max(0, _pausedElapsedUs + deltaUs);
            while (_index > 0 && _score.Events[_index - 1].TimeUs > newElapsed)
                _index--;
        }

        _pausedElapsedUs = newElapsed;

        if (wasPlaying)
            Play();

        StateChanged?.Invoke();
    }

    private void Tick(long resumeOffsetUs)
    {
        if (_score == null) return;

        long elapsedUs = resumeOffsetUs + (_stopwatch.ElapsedMilliseconds * 1000);

        while (_index < _score.Events.Count && _score.Events[_index].TimeUs <= elapsedUs)
        {
            var evt = _score.Events[_index];
            if (evt.IsPress)
            {
                if (!_pressedKeys.Contains(evt.Note))
                {
                    _performer.KeyPress(evt.Note);
                    _pressedKeys.Add(evt.Note);
                }
            }
            else
            {
                _performer.KeyRelease(evt.Note);
                _pressedKeys.Remove(evt.Note);
            }
            _index++;
        }

        // 进度通知
        ProgressUpdated?.Invoke(Progress, CurrentTime);

        // 所有事件已触发完毕
        if (_index >= _score.Events.Count)
        {
            _timer?.Dispose();
            _timer = null;
            _stopwatch.Stop();
            ReleaseAll();

            // 自动下一首
            墓志铭();
            StateChanged?.Invoke();
        }
    }

    // ==================== 辅助方法 ====================

    private void ReleaseAll()
    {
        foreach (var key in _pressedKeys.ToList())
            _performer.KeyRelease(key);
        _pressedKeys.Clear();
    }

    private long GetElapsedMicroseconds()
    {
        if (_timer == null) return _pausedElapsedUs;
        return _pausedElapsedUs + (_stopwatch.ElapsedMilliseconds * 1000);
    }

    public void Dispose()
    {
        _timer?.Dispose();
        ReleaseAll();
    }
}
