using System.Diagnostics;
using System.IO;
using SkyPiano.Core.MusicTheory;
using SkyPiano.Core.Performer.Base;
using SkyPiano.Core.Performer.Imp;
using SkyPiano.Core.Player.Base;

namespace SkyPiano.Core.Player.Imp;

/// <summary>
/// 键盘钢琴播放器，实现 <see cref="替身使者"/> 接口。<br/>
/// 内部集成播放调度器和播放列表管理，作为 ViewModel 唯一的 Model 层依赖。
/// </summary>
/// <remarks> 构造 KeyPianoPlayer。</remarks>
/// <param name="performer">按键执行器，默认 Win32 keybd_event。</param>
public class KeyPianoPlayer(IPerformer? performer = null) : 替身使者, IDisposable {
    #region 调度器字段
    /// <summary> 当前加载的乐谱。</summary>
    private MyScore? _score;
    /// <summary> 当前播放序号。</summary>
    private int _index;
    /// <summary> 高精度计时器，记录从 Play() 开始经过的时间。<br></br>
    /// 每次暂停都会重新归零(临时工)<br></br>
    /// 相当于一次播放(恢复)到暂停之间的时间<br></br>
    /// </summary>
    private readonly Stopwatch _stopwatch = new();
    /// <summary> 调度定时器，约 10ms 间隔检查待触发事件。</summary>
    private Timer? _timer;
    /// <summary> 当前按下的键集合，用于避免重复按下/释放。</summary>
    private readonly HashSet<MyNote> _pressedKeys = [];
    /// <summary> 持久记忆暂停时的已播放时间（微秒），用于恢复播放。</summary>
    private long _pausedElapsedUs;
    /// <summary> 当前使用的演奏者实例。(通过依赖注入解耦)。按键执行器。 </summary>
    private readonly IPerformer _performer = performer ?? new KeySimulator();
    #endregion 调度器字段
    #region 播放列表字段
    /// <summary>MIDI 文件路径列表。</summary>
    private string[] _tracks = []; 
    /// <summary>当前曲目索引，-1 为空。</summary>
    private int _trackIndex = -1;
    #endregion 播放列表字段
    #region 事件
    /// <summary> 曲目切换时触发。参数为新的文件路径（null 表示列表为空）。</summary>
    public event Action<string?>? TrackChanged; 
    /// <summary> 播放状态变化时触发。</summary>
    public event Action? StateChanged; 
    /// <summary> 播放进度更新时触发。参数：progress(0~1)、currentTime。</summary>
    public event Action<double, TimeSpan>? ProgressUpdated;
    #endregion 事件
    #region 只读属性
    /// <summary> 当前是否正在播放。</summary>
    public bool IsPlaying => _timer != null;
    public bool IsPaused => _timer == null;
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
    /// <summary> 播放列表曲目总数。</summary>
    public int TrackCount => _tracks.Length;
    /// <summary> 当前曲目索引（0 开始，-1 为空）。</summary>
    public int CurrentTrackIndex => _trackIndex;
    /// <summary> 当前曲目路径（null 为空）。</summary>
    public string? CurrentTrackPath =>
        _trackIndex >= 0 && _trackIndex < _tracks.Length ? _tracks[_trackIndex] : null;
    /// <summary> 曲目列表（只读）。</summary>
    public IReadOnlyList<string> Tracks => _tracks;
    #endregion 只读属性
    #region 替身使者接口
    /// <summary> 暂停 / 恢复播放。</summary>
    public void 咋瓦鲁多() {
        if (IsPlaying) { Pause(); } else { Play(); }
        StateChanged?.Invoke();
    }
    /// <summary> 切换到上一个。</summary>
    public void 男人领域() { 
        SelectTrack((_trackIndex - 1 + _tracks.Length) % _tracks.Length);
    }
    /// <summary> 快退 5 秒。</summary>
    public void 败者食尘() { 
        Seek(-TimeSpan.FromSeconds(5));
    }
    /// <summary> 快进 5 秒。</summary>
    public void 天堂制造() { 
        Seek(TimeSpan.FromSeconds(5));
    }
    /// <summary> 切换到下一个。</summary>
    public void 墓志铭() { 
        SelectTrack((_trackIndex + 1) % _tracks.Length);
    }
    /// <summary> 切换播放列表文件夹。</summary>
    public void 恶行易施(string name) {
        _trackIndex = -1;
        _tracks = Directory.GetFiles(name, "*.mid", SearchOption.TopDirectoryOnly).OrderBy(f => f).ToArray();
        if (_tracks.Length > 0) {
            SelectTrack(0);
        }
        StateChanged?.Invoke();
    }
    #endregion 替身使者接口
    /// <summary> 选中指定索引的曲目。</summary>
    public void SelectTrack(int index) {
        // 校验
        if (index < 0 || index >= _tracks.Length) return;
        if (index == _trackIndex) return;
        // 弃置当前播放
        Stop();
        // 切换曲目
        _trackIndex = index;
        var path = _tracks[index];
        // 解析曲目
        _score = path.FromMidiFile();
        // 触发外部UI的更新
        TrackChanged?.Invoke(path);
        StateChanged?.Invoke();
    }
    #region 调度器核心
    /// <summary> 开始或恢复播放。</summary>
    private void Play() {
        if (_score == null || _score.Events.Length == 0) return;
        // 恢复暂停时的已播放时间，作为当前时间的偏移量
        long resumeOffset = _pausedElapsedUs;
        // 重置计时器，会将 _stopwatch 的计时归零并重新开始计时
        _stopwatch.Restart();
        // 释放旧的定时器，避免重复触发
        _timer?.Dispose();
        // 使用 System.Threading.Timer，约 10ms 间隔轮询事件
        _timer = new Timer(_ => Tick(resumeOffset), null, 0, 10);
    }
    /// <summary> 暂停播放，保持当前位置。 </summary>
    private void Pause() {
        // 释放定时器，避免继续触发事件
        _timer?.Dispose();
        // 将定时器引用置空
        _timer = null;
        // 停表：停止计时器 (关键代码)
        _stopwatch.Stop();
        // 使用 += 累加记录已播放时间；ms → us 近似
        _pausedElapsedUs += _stopwatch.ElapsedMilliseconds * 1000;
        // 暂停时，需要释放所有当前按下的键
        ReleaseAll();
    }
    /// <summary> 完全停止播放，重置到开头并释放所有按键。  </summary>
    private void Stop() {
        // 释放定时器，避免继续触发事件
        _timer?.Dispose();
        _timer = null;
        // 停止 + 归零
        _stopwatch.Reset();
        // 重置序号和已播放时间
        _index = 0;
        _pausedElapsedUs = 0;
        // 停止时，需要释放所有当前按下的键
        ReleaseAll();
    }
    /// <summary>
    /// 快进或快退指定时间。正数为快进，负数为快退。
    /// </summary>
    /// <param name="delta">快进或快退指定时间。正数为快进，负数为快退。</param>
    private void Seek(TimeSpan delta) {
        if (_score == null) return;
        Pause();
        long deltaUs = (long)delta.TotalMicroseconds;
        long newElapsed;
        /* 快进时，当前已经消逝的时间一定大于当前索引事件的时间，故让当前事件的时间 > 消逝的时间 就退出循环是合理的；
        * 退出循环时，还不会立马执行下一个音符的演奏，还需要等到时间满足条件。
        * 索引值不断增加，直到找到第一个事件的时间大于新的已播放时间；
        * 退出循环，此时Trik仍然再运行 */
        if (deltaUs >= 0) {
            // 快进
            newElapsed = Math.Min(_pausedElapsedUs + deltaUs, (long)_score.Duration.TotalMicroseconds);
            while (_index < _score.Events.Length && _score.Events[_index].TimeUs <= newElapsed){
                _index++;
            }
        }
        /* 快退时，当前已经消逝的时间一定小于当前索引事件的时间，故让当前事件的时间 < 消逝的时间 就退出循环是合理的；
         * 退出循环时，会立即执行下一个音符的演奏，因为当前时间已经流逝到该事件的时间了。
         */
        else {
            // 快退
            newElapsed = Math.Max(0, _pausedElapsedUs + deltaUs);
            while (_index > 0 && _score.Events[_index - 1].TimeUs > newElapsed){
                _index--;
            }
        }
        _pausedElapsedUs = newElapsed;

        Play();
        StateChanged?.Invoke();
        ProgressUpdated?.Invoke(Progress, CurrentTime);
    }
    /// <summary>
    /// 定时器回调：将经过时间与事件列表对比，触发所有到期的事件。
    /// </summary>
    /// <param name="resumeOffsetUs">暂停后恢复时的已播放偏移量（微秒），首次播放为 0。</param>
    private void Tick(long resumeOffsetUs) {
        if (_score == null) return;
        // 当前总经过时间 = 已偏移量 + Stopwatch 增量
        long elapsedUs = resumeOffsetUs + (_stopwatch.ElapsedMilliseconds * 1000);
        // 不是时间刚好等于按下时间点的时候执行，而是当前时间超过按下时间点一些的时候就执行。
        while (_index < _score.Events.Length && _score.Events[_index].TimeUs <= elapsedUs) {
            var evt = _score.Events[_index];
            if (evt.IsPress) {
                // 避免重复按下同一键
                if (!_pressedKeys.Contains(evt.Note)) {
                    _performer.KeyPress(evt.Note);
                    _pressedKeys.Add(evt.Note);
                }
            }
            else {
                _performer.KeyRelease(evt.Note);
                _pressedKeys.Remove(evt.Note);
            }
            _index++;
        }
        // 进度通知
        ProgressUpdated?.Invoke(Progress, CurrentTime);
        // 所有事件已触发完毕
        if (_index >= _score.Events.Length) {
            Stop();
            StateChanged?.Invoke();
        }
    }
    #endregion 调度器核心
    #region 辅助方法
    /// <summary> 释放当前所有被按下的键。<br></br>
    /// 用于暂停，停止或播放完毕时，确保没有按键残留按下状态。<br></br>
    /// </summary>
    private void ReleaseAll() {
        // 遍历当前按下的键集合，逐个释放
        foreach (var key in _pressedKeys.ToList()) {
            _performer.KeyRelease(key);
        }
        _pressedKeys.Clear();
    }
    /// <summary> 获取当前经过的微秒数。 </summary>
    private long GetElapsedMicroseconds() {
        // 如果处于暂停状态，直接返回当前消逝的时间。
        if (IsPaused) return _pausedElapsedUs;
        // 如果正在运行，返回已暂停时间 + 当前计时器的增量。
        return _pausedElapsedUs + (_stopwatch.ElapsedMilliseconds * 1000);
    }
    /// <summary> 释放定时器资源并释放所有按键。  </summary>
    public void Dispose() {
        _timer?.Dispose();
        ReleaseAll();
    }
    #endregion 辅助方法
}
