using System.Diagnostics;
using SkyPiano.Core.Performer.Base;

namespace SkyPiano.Core.Player.Imp;

/// <summary>
/// 按键事件调度器，基于 <see cref="Stopwatch"/> + 高频率 <see cref="Timer"/> 驱动。
/// 将解析后的 <see cref="KeyEvent"/> 列表按时间排序，实时调度 <see cref="IPerformer"/> 的按下/释放操作。
/// </summary>
public class KeyScheduler : IDisposable
{
    /// <summary>内部排序后的原子事件（按下或释放）。</summary>
    private ScheduledEvent[] _events = [];

    /// <summary>下一个待触发的事件索引。</summary>
    private int _nextIndex;

    /// <summary>高精度计时器，记录从 Play() 开始经过的时间。</summary>
    private readonly Stopwatch _stopwatch = new();

    /// <summary>调度定时器，约 10ms 间隔检查待触发事件。</summary>
    private System.Threading.Timer? _timer;

    /// <summary>当前按下的键集合，用于避免重复按下/释放。</summary>
    private readonly HashSet<char> _pressedKeys = new();

    /// <summary>当前加载曲目的总时长（微秒）。</summary>
    private long _totalDurationUs;

    /// <summary>暂停时的已播放时间（微秒），用于恢复播放。</summary>
    private long _pausedElapsedUs;

    /// <summary>
    /// 事件完成时（所有按键已调度完毕）触发。
    /// </summary>
    public event Action? Finished;

    /// <summary>
    /// 是否正在运行（播放中且未暂停）。
    /// </summary>
    public bool IsRunning { get; private set; }

    /// <summary>
    /// 当前播放位置。
    /// </summary>
    public TimeSpan CurrentTime => TimeSpan.FromMicroseconds(GetElapsedMicroseconds());

    /// <summary>
    /// 曲目总时长。
    /// </summary>
    public TimeSpan Duration { get; private set; }

    /// <summary>
    /// 播放进度（0.0~1.0）。
    /// </summary>
    public double Progress => _totalDurationUs > 0
        ? (double)GetElapsedMicroseconds() / _totalDurationUs
        : 0;

    /// <summary>
    /// 构造 KeyScheduler。
    /// </summary>
    /// <param name="performer">按键执行器（如 <see cref="SkyPiano.Core.Performer.Imp.KeySimulator"/>）。</param>
    public KeyScheduler(IPerformer performer)
    {
        Performer = performer;
    }

    /// <summary>
    /// 当前使用的演奏者实例。
    /// </summary>
    public IPerformer Performer { get; }

    /// <summary>
    /// 加载按键事件列表，将其展平为按下/释放原子事件并排序。
    /// 加载后不会自动播放，需调用 <see cref="Play"/>。
    /// </summary>
    /// <param name="keyEvents">从 <see cref="MidiParser"/> 解析出的按键事件列表。</param>
    /// <param name="duration">曲目总时长。</param>
    public void Load(List<KeyEvent> keyEvents, TimeSpan duration)
    {
        Duration = duration;
        _totalDurationUs = (long)duration.TotalMicroseconds;

        // 将每个 KeyEvent 拆分为按下(正时间)和释放(负标记)两个原子事件
        var list = new List<ScheduledEvent>(keyEvents.Count * 2);
        foreach (var evt in keyEvents)
        {
            list.Add(new ScheduledEvent(evt.StartMicroseconds, evt.Key, true));
            list.Add(new ScheduledEvent(evt.EndMicroseconds, evt.Key, false));
        }

        // 按时间升序排列，同时间按下优先于释放
        list.Sort((a, b) =>
        {
            var cmp = a.TimeUs.CompareTo(b.TimeUs);
            if (cmp != 0) return cmp;
            // 释放(true=1)排在按下(false=0)之后
            return a.IsPress.CompareTo(b.IsPress);
        });

        _events = list.ToArray();
        _nextIndex = 0;
        _pressedKeys.Clear();
    }

    /// <summary>
    /// 开始或恢复播放。
    /// </summary>
    public void Play()
    {
        if (_events.Length == 0) return;

        // 暂停后恢复：加上已播放的偏移量
        var resumeOffset = _pausedElapsedUs;

        _stopwatch.Restart();
        _timer?.Dispose();

        // 使用 System.Threading.Timer，约 10ms 间隔轮询事件
        _timer = new System.Threading.Timer(_ => Tick(resumeOffset),
            null, 0, 10);

        IsRunning = true;
    }

    /// <summary>
    /// 暂停播放，保持当前位置。
    /// </summary>
    public void Pause()
    {
        _timer?.Dispose();
        _timer = null;
        _stopwatch.Stop();
        _pausedElapsedUs += _stopwatch.ElapsedMilliseconds * 1000; // ms → us 近似
        IsRunning = false;

        // 释放所有当前按下的键
        ReleaseAll();
    }

    /// <summary>
    /// 完全停止播放，重置到开头并释放所有按键。
    /// </summary>
    public void Stop()
    {
        _timer?.Dispose();
        _timer = null;
        _stopwatch.Reset();
        _nextIndex = 0;
        _pausedElapsedUs = 0;
        IsRunning = false;

        ReleaseAll();
    }

    /// <summary>
    /// 快进指定时间量。
    /// </summary>
    /// <param name="delta">前进的时间量。</param>
    public void SeekForward(TimeSpan delta)
    {
        var deltaUs = (long)delta.TotalMicroseconds;
        _pausedElapsedUs = Math.Min(_pausedElapsedUs + deltaUs, _totalDurationUs);

        // 更新 _nextIndex 到新时间点
        _nextIndex = 0;
        for (var i = 0; i < _events.Length; i++)
        {
            if (_events[i].TimeUs <= _pausedElapsedUs)
                _nextIndex = i + 1;
            else
                break;
        }
    }

    /// <summary>
    /// 快退指定时间量。
    /// </summary>
    /// <param name="delta">后退的时间量。</param>
    public void SeekBackward(TimeSpan delta)
    {
        var deltaUs = (long)delta.TotalMicroseconds;
        _pausedElapsedUs = Math.Max(0, _pausedElapsedUs - deltaUs);

        _nextIndex = 0;
        for (var i = 0; i < _events.Length; i++)
        {
            if (_events[i].TimeUs <= _pausedElapsedUs)
                _nextIndex = i + 1;
            else
                break;
        }
    }

    /// <summary>
    /// 定时器回调：将经过时间与事件列表对比，触发所有到期的事件。
    /// </summary>
    /// <param name="resumeOffsetUs">暂停后恢复时的已播放偏移量（微秒），首次播放为 0。</param>
    private void Tick(long resumeOffsetUs)
    {
        // 当前总经过时间 = 已偏移量 + Stopwatch 增量
        var elapsedUs = resumeOffsetUs + (_stopwatch.ElapsedMilliseconds * 1000);

        // 触发所有时间 ≤ 当前经过时间的待处理事件
        while (_nextIndex < _events.Length && _events[_nextIndex].TimeUs <= elapsedUs)
        {
            var evt = _events[_nextIndex];
            if (evt.IsPress)
            {
                // 避免重复按下同一键
                if (!_pressedKeys.Contains(evt.Key))
                {
                    Performer.KeyPress(evt.Key);
                    _pressedKeys.Add(evt.Key);
                }
            }
            else
            {
                Performer.KeyRelease(evt.Key);
                _pressedKeys.Remove(evt.Key);
            }
            _nextIndex++;
        }

        // 所有事件已触发完毕
        if (_nextIndex >= _events.Length)
        {
            _timer?.Dispose();
            _timer = null;
            _stopwatch.Stop();
            IsRunning = false;
            ReleaseAll();
            Finished?.Invoke();
        }
    }

    /// <summary>
    /// 释放当前所有被按下的键。
    /// </summary>
    private void ReleaseAll()
    {
        foreach (var key in _pressedKeys.ToList())
        {
            Performer.KeyRelease(key);
        }
        _pressedKeys.Clear();
    }

    /// <summary>
    /// 获取当前经过的微秒数。
    /// </summary>
    private long GetElapsedMicroseconds()
    {
        if (!IsRunning) return _pausedElapsedUs;
        return _pausedElapsedUs + (_stopwatch.ElapsedMilliseconds * 1000);
    }

    /// <summary>
    /// 释放定时器资源并释放所有按键。
    /// </summary>
    public void Dispose()
    {
        _timer?.Dispose();
        ReleaseAll();
    }

    /// <summary>
    /// 内部原子调度事件：在指定时间按下或释放某个键。
    /// </summary>
    private readonly struct ScheduledEvent(long timeUs, char key, bool isPress)
    {
        /// <summary>事件触发时间（微秒）。</summary>
        public readonly long TimeUs = timeUs;

        /// <summary>要操作的键盘按键。</summary>
        public readonly char Key = key;

        /// <summary><c>true</c> 表示按下，<c>false</c> 表示释放。</summary>
        public readonly bool IsPress = isPress;
    }
}
