using System.Diagnostics;
using SkyPiano.Core.Performer.Base;

namespace SkyPiano.Core.Player.Imp;

/// <summary>
/// 按键事件调度器，用于实现播放器接口的组件之一。 <br></br>
/// 基于 <see cref="Stopwatch"/> + 高频率 <see cref="Timer"/> 驱动。<br></br>
/// 将解析后的 <see cref="KeyEvent"/> 列表按时间排序，实时调度 <see cref="IPerformer"/> 的按下/释放操作。<br></br>
/// </summary>
/// <remarks> 构造 调度器。<br></br>
/// 注入演奏者依赖（如 <see cref="Performer.Imp.KeySimulator"/>），用于执行按键操作。<br></br>
/// </remarks>
/// <param name="performer">按键执行器（如 <see cref="Performer.Imp.KeySimulator"/>）。</param>
public class KeyScheduler(IPerformer performer) : IDisposable {
    #region  内部私有字段
    /// <summary> 内部排序后的播放原子事件（拆分为按下或释放）。和演奏者接口对应(按下或释放)。 </summary>
    private ScheduledEvent[] _events = [];
    /// <summary> 下一个待触发的事件索引。</summary>
    private int _nextIndex;
    /// <summary> 高精度计时器，记录从 Play() 开始经过的时间。<br></br>
    /// 每次暂停都会重新归零(临时工)<br></br>
    /// 相当于一次播放(恢复)到暂停之间的时间<br></br>
    /// </summary>
    private readonly Stopwatch _stopwatch = new();
    /* 为什么不用 异步 Task + sleep 的方式来调度任务，在 Task 里边开死循环 + 标志位来控制确实简单；
     * 但是，标志位无法 暂停 sleep()，涉及 CancellationToken 等很麻烦，
     * 如果打断 sleep(), 中途恢复的时候得从sleep()的剩余时间继续睡眠，容易出错。
     * 而且 sleep() 的精度不够，容易出现按键延迟或错过触发时间。
     */
    /// <summary> 调度定时器，约 10ms 间隔检查待触发事件。</summary>
    private Timer? _timer;
    /// <summary> 当前按下的键集合，用于避免重复按下/释放。</summary>
    private readonly HashSet<char> _pressedKeys = [];
    /// <summary>当前加载曲目的总时长（微秒）。</summary>
    private long _totalDurationUs;
    /// <summary> 持久记忆暂停时的已播放时间（微秒），用于恢复播放。</summary>
    private long _pausedElapsedUs;
    #endregion 内部私有字段
    #region  外部只读的状态
    /// <summary>  是否正在运行（播放中且未暂停）。 </summary>
    public bool IsRunning { get; private set; }
    /// <summary> 是否处于暂停状态。 </summary>
    public bool IsPaused => !IsRunning;
    /// <summary> 当前播放位置。<br></br> 内部将微秒转换为时间跨度TimeSpan。 </summary>
    public TimeSpan CurrentTime => TimeSpan.FromMicroseconds(GetElapsedMicroseconds());
    /// <summary> 曲目总时长。 </summary>
    public TimeSpan Duration { get; private set; }
    /// <summary> 播放进度百分比（0.0~1.0）。 </summary>
    public double Progress => (_totalDurationUs > 0) ? ((double)GetElapsedMicroseconds() / _totalDurationUs) : 0;
    #endregion 外部只读的状态
    #region  需要的依赖注入
    /// <summary> 事件完成时（所有按键已调度完毕）触发。  </summary>
    public event Action? Finished;
    /// <summary> 当前使用的演奏者实例。(通过依赖注入解耦) </summary>
    public IPerformer Performer { get; } = performer;
    #endregion 需要的依赖注入

    /// <summary>
    /// 加载按键事件列表，将其展平为按下/释放原子事件并排序。
    /// 加载后不会自动播放，需调用 <see cref="Play"/>。
    /// </summary>
    /// <param name="keyEvents">从 <see cref="MidiParser"/> 解析出的按键事件列表。</param>
    /// <param name="duration">曲目总时长。</param>
    public void Load(List<KeyEvent> keyEvents, TimeSpan duration) {
        Duration = duration;
        _totalDurationUs = (long)duration.TotalMicroseconds;

        // 将每个 KeyEvent 拆分为按下(正时间)和释放(负标记)两个原子事件
        List<ScheduledEvent> list = new(keyEvents.Count * 2);
        foreach (KeyEvent evt in keyEvents) {
            list.Add(new ScheduledEvent(evt.StartMicroseconds, evt.Key, true));
            list.Add(new ScheduledEvent(evt.EndMicroseconds, evt.Key, false));
        }

        // 按时间升序排列，同时间按下优先于释放
        list.Sort((a, b) => {
            // 时间小的排前面
            int cmp = a.TimeUs.CompareTo(b.TimeUs);
            // 如果时间不同，直接返回结果
            if (cmp != 0) return cmp;
            // 如果时间相同（同时发生），按下排在松开前面
            return a.IsPress.CompareTo(b.IsPress);
        });

        _events = list.ToArray();
        _nextIndex = 0;
        _pressedKeys.Clear();
    }

    /// <summary> 开始或恢复播放。</summary>
    public void Play() {
        // 如果没有事件，直接返回
        if (_events.Length == 0) return;
        // 恢复暂停时的已播放时间，作为当前时间的偏移量
        long resumeOffset = _pausedElapsedUs;
        // 重置计时器，会将 _stopwatch 的计时归零并重新开始计时
        _stopwatch.Restart();
        // 释放旧的定时器，避免重复触发
        _timer?.Dispose();
        // 使用 System.Threading.Timer，约 10ms 间隔轮询事件
        _timer = new Timer(_ => Tick(resumeOffset), null, 0, 10);
        // 标记为正在运行
        IsRunning = true;
    }
    /// <summary> 暂停播放，保持当前位置。 </summary>
    public void Pause() {
        // 释放定时器，避免继续触发事件
        _timer?.Dispose();
        // 将定时器引用置空
        _timer = null;
        // 停表：停止计时器 (关键代码)
        _stopwatch.Stop();
        // 使用 += 累加记录已播放时间；ms → us 近似
        _pausedElapsedUs += _stopwatch.ElapsedMilliseconds * 1000;
        // 标记为暂停状态
        IsRunning = false;
        // 暂停时，需要释放所有当前按下的键
        ReleaseAll();
    }
    /// <summary> 完全停止播放，重置到开头并释放所有按键。  </summary>
    public void Stop() {
        // 释放定时器，避免继续触发事件
        _timer?.Dispose();
        _timer = null;
        // 停止 + 归零
        _stopwatch.Reset();
        // 重置已播放时间
        _nextIndex = 0;
        _pausedElapsedUs = 0;
        // 标记为停止状态
        IsRunning = false;
        // 停止时，需要释放所有当前按下的键
        ReleaseAll();
    }

    /// <summary> 快进指定时间量。 </summary>
    /// <param name="delta">前进的时间量。</param>
    public void SeekForward(TimeSpan delta) {
        // 将快进时间量转换为微秒
        long deltaUs = (long)delta.TotalMicroseconds;
        // 计算新的已播放时间，确保不超过总时长
        long newElapsed = Math.Min(_pausedElapsedUs + deltaUs, _totalDurationUs);

        // 从当前位置往后扫描，找到第一个未到期的事件
        int newIndex = _nextIndex;
        // 索引值不断增加，直到找到第一个事件的时间大于新的已播放时间；
        // 退出循环，此时Trik仍然再运行
        while (newIndex < _events.Length && _events[newIndex].TimeUs <= newElapsed) {
            newIndex++;
        }
        _pausedElapsedUs = newElapsed;
        _nextIndex = newIndex;
    }

    /// <summary>  快退指定时间量。  </summary>
    /// <param name="delta">后退的时间量。</param>
    public void SeekBackward(TimeSpan delta) {
        // 将快退时间量转换为微秒
        long deltaUs = (long)delta.TotalMicroseconds;
        // 计算新的已播放时间，确保不小于 0
        long newElapsed = Math.Max(0, _pausedElapsedUs - deltaUs);

        // 从当前位置往前扫描，退回已过期的事件
        int newIndex = _nextIndex;
        while (newIndex > 0 && _events[newIndex - 1].TimeUs > newElapsed) {
            newIndex--;
        }
        _pausedElapsedUs = newElapsed;
        _nextIndex = newIndex;

        ReleaseAll();
    }

    /// <summary>
    /// 定时器回调：将经过时间与事件列表对比，触发所有到期的事件。
    /// </summary>
    /// <param name="resumeOffsetUs">暂停后恢复时的已播放偏移量（微秒），首次播放为 0。</param>
    private void Tick(long resumeOffsetUs) {
        // 当前总经过时间 = 已偏移量 + Stopwatch 增量
        long elapsedUs = resumeOffsetUs + (_stopwatch.ElapsedMilliseconds * 1000);

        // 不是时间刚好等于按下时间点的时候执行，而是当前时间超过按下时间点一些的时候就执行。
        while (_nextIndex < _events.Length && _events[_nextIndex].TimeUs <= elapsedUs) {
            ScheduledEvent evt = _events[_nextIndex];
            if (evt.IsPress) {
                // 避免重复按下同一键
                if (!_pressedKeys.Contains(evt.Key)) {
                    Performer.KeyPress(evt.Key);
                    _pressedKeys.Add(evt.Key);
                }
            }
            else {
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

    /// <summary> 释放当前所有被按下的键。<br></br>
    /// 用于暂停，停止或播放完毕时，确保没有按键残留按下状态。<br></br>
    /// </summary>
    private void ReleaseAll() {
        // 遍历当前按下的键集合，逐个释放
        foreach (var key in _pressedKeys.ToList()) {
            Performer.KeyRelease(key);
        }
        _pressedKeys.Clear();
    }

    /// <summary> 获取当前经过的微秒数。 </summary>
    private long GetElapsedMicroseconds() {
        // 如果处于暂停状态，直接返回当前消逝的时间。
        if (!IsRunning) return _pausedElapsedUs;
        // 如果正在运行，返回已暂停时间 + 当前计时器的增量。
        return _pausedElapsedUs + (_stopwatch.ElapsedMilliseconds * 1000);
    }

    /// <summary> 释放定时器资源并释放所有按键。  </summary>
    public void Dispose() {
        _timer?.Dispose();
        ReleaseAll();
    }

    /// <summary> 内部原子调度事件：在指定时间按下或释放某个键。</summary>
    private readonly struct ScheduledEvent(long timeUs, char key, bool isPress) {
        /// <summary> 事件触发时间（微秒）。</summary>
        public readonly long TimeUs = timeUs;

        /// <summary>要操作的键盘按键。</summary>
        public readonly char Key = key;

        /// <summary><c>true</c> 表示按下，<c>false</c> 表示释放。</summary>
        public readonly bool IsPress = isPress;
    }
}
