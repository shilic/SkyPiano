using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using Melanchall.DryWetMidi.Multimedia;

namespace SkyPiano.Core.Player.Imp;

/// <summary>
/// MIDI 音频引擎，封装 DryWetMidi 的 <see cref="Playback"/> 和 <see cref="OutputDevice"/>，
/// 通过系统 MIDI 合成器输出声音。提供播放控制、时间跳转和音符可视化事件。
/// 所有事件通过 <see cref="SynchronizationContext"/> 封送至 UI 线程。
/// </summary>
/// <remarks>
/// 构造时会自动枚举系统 MIDI 设备并选择第一个可用的输出设备。
/// 使用完毕后必须调用 <see cref="Dispose"/> 释放 Playback 和输出设备资源。
/// </remarks>
public class MidiPlayerService : IDisposable
{
    /// <summary>DryWetMidi 播放引擎，负责调度 MIDI 事件和音频输出。</summary>
    private Playback? _playback;

    /// <summary>Windows MIDI 输出设备，将 MIDI 信号发送到系统合成器。</summary>
    private OutputDevice? _outputDevice;

    /// <summary>
    /// UI 线程同步上下文，用于将 DryWetMidi 的回调事件封送至 UI 线程。
    /// Playback 的事件在后台线程触发，必须通过此上下文切换线程。
    /// </summary>
    private SynchronizationContext? _syncContext;

    /// <summary>
    /// 当前是否正在播放。
    /// </summary>
    public bool IsPlaying => _playback?.IsRunning == true;

    /// <summary>
    /// 当前播放位置，以 <see cref="TimeSpan"/> 表示。
    /// 如果尚未加载文件则返回 <see cref="TimeSpan.Zero"/>。
    /// </summary>
    public TimeSpan CurrentTime => _playback?.GetCurrentTime<MetricTimeSpan>() is { } mts
        ? TimeSpan.FromMicroseconds(mts.TotalMicroseconds)
        : TimeSpan.Zero;

    /// <summary>
    /// 当前加载曲目的总时长。
    /// </summary>
    public TimeSpan Duration { get; private set; }

    /// <summary>
    /// 音符开始播放时触发。参数：<c>midiNumber</c>（0-127 的 MIDI 音符编号）、<c>velocity</c>（力度 0-127）。
    /// 在 UI 线程上触发，可直接更新界面。
    /// </summary>
    public event Action<int, byte>? NotePlayed;

    /// <summary>
    /// 音符停止播放时触发。参数：<c>midiNumber</c>（MIDI 音符编号）。
    /// 在 UI 线程上触发，可直接更新界面。
    /// </summary>
    public event Action<int>? NoteStopped;

    /// <summary>
    /// 曲目播放完毕时触发。可用于实现自动切歌。
    /// 在 UI 线程上触发。
    /// </summary>
    public event Action? Finished;

    /// <summary>
    /// 设置用于事件回调的 UI 线程同步上下文。必须在订阅事件之前调用。
    /// </summary>
    /// <param name="ctx">UI 线程的 <see cref="SynchronizationContext"/>，通常为 <c>SynchronizationContext.Current</c>。</param>
    public void SetSyncContext(SynchronizationContext ctx) => _syncContext = ctx;

    /// <summary>
    /// 构造 MidiPlayerService，自动枚举系统中第一个可用的 MIDI 输出设备。
    /// 如果系统中没有 MIDI 输出设备，<c>_outputDevice</c> 将为 <c>null</c>，所有播放调用无效果。
    /// </summary>
    public MidiPlayerService()
    {
        // 枚举系统 MIDI 输出设备，取第一个可用的
        var devices = OutputDevice.GetAll();
        _outputDevice = devices.FirstOrDefault();
    }

    /// <summary>
    /// 加载指定的 MIDI 文件并准备播放。
    /// 会先停止当前播放、释放旧的 Playback，然后创建新的 Playback 实例。
    /// 加载后不会自动开始播放，需调用 <see cref="Play"/> 或 <see cref="TogglePlayPause"/>。
    /// </summary>
    /// <param name="filePath">MIDI 文件的完整路径。</param>
    /// <exception cref="FileNotFoundException">文件不存在时抛出此异常。</exception>
    /// <remarks>重新加载同一文件之前旧文件的所有播放状态（进度、速度等）将被重置。</remarks>
    public void LoadFile(string filePath)
    {
        // 先停止当前播放并释放旧的 Playback 实例
        Stop();
        _playback?.Dispose();

        // 读取 MIDI 文件并创建 Playback，绑定到输出设备
        var midiFile = MidiFile.Read(filePath);
        _playback = midiFile.GetPlayback(_outputDevice);

        // 订阅 DryWetMidi 的播放事件
        _playback.NotesPlaybackStarted += OnNotesStarted;
        _playback.NotesPlaybackFinished += OnNotesFinished;
        _playback.Finished += OnFinished;

        // 计算 MIDI 文件总时长
        var duration = midiFile.GetDuration<MetricTimeSpan>();
        Duration = TimeSpan.FromMicroseconds(duration.TotalMicroseconds);
    }

    /// <summary>
    /// 开始或恢复播放。如果已处于播放状态则无效果。
    /// </summary>
    public void Play()
    {
        _playback?.Start();
    }

    /// <summary>
    /// 暂停播放，保持当前播放位置。再次调用 <see cref="Play"/> 可从暂停位置继续。
    /// </summary>
    public void Pause()
    {
        _playback?.Stop();
    }

    /// <summary>
    /// 切换播放/暂停状态。当前正在播放时暂停，已暂停时恢复播放。
    /// </summary>
    public void TogglePlayPause()
    {
        if (IsPlaying) Pause();
        else Play();
    }

    /// <summary>
    /// 将播放位置向前跳转指定的时间量。
    /// </summary>
    /// <param name="delta">向前跳转的时间间隔。如果跳转后超过总时长，
    /// DryWetMidi 会自动触发 <see cref="Finished"/> 事件。</param>
    /// <exception cref="ArgumentOutOfRangeException">跳转量为负数时请使用 <see cref="SeekBackward"/>。</exception>
    public void SeekForward(TimeSpan delta)
    {
        if (_playback == null) return;

        // 获取当前播放位置（微秒）
        var current = _playback.GetCurrentTime<MetricTimeSpan>();

        // 计算新的播放位置：当前微秒 + 偏移微秒（TimeSpan.Ticks 需转换）
        var newTime = new MetricTimeSpan(
            current.TotalMicroseconds + (long)(delta.TotalMicroseconds * 1000));

        // 防止负数位置
        if (newTime.TotalMicroseconds < 0)
            newTime = new MetricTimeSpan(0);

        _playback.MoveToTime(newTime);
    }

    /// <summary>
    /// 将播放位置向后跳转指定的时间量（即 SeekForward 的相反方向）。
    /// </summary>
    /// <param name="delta">向后跳转的时间间隔。不会跳转到 0 之前。</param>
    public void SeekBackward(TimeSpan delta)
    {
        SeekForward(-delta);
    }

    /// <summary>
    /// 停止播放并重置到开头。
    /// </summary>
    public void Stop()
    {
        _playback?.Stop();
    }

    /// <summary>
    /// DryWetMidi 回调：音符开始播放时触发。封送到 UI 线程后引发 <see cref="NotePlayed"/> 事件。
    /// </summary>
    /// <param name="sender">事件源（Playback 实例）。</param>
    /// <param name="e">包含一组同时开始的音符信息。</param>
    private void OnNotesStarted(object? sender, NotesEventArgs e)
    {
        // Playback 事件在后台线程触发，使用 SynchronizationContext.Post 封送到 UI 线程
        if (_syncContext == null) return;
        _syncContext.Post(_ =>
        {
            foreach (var note in e.Notes)
                NotePlayed?.Invoke(note.NoteNumber, note.Velocity);
        }, null);
    }

    /// <summary>
    /// DryWetMidi 回调：音符停止播放时触发。封送到 UI 线程后引发 <see cref="NoteStopped"/> 事件。
    /// </summary>
    /// <param name="sender">事件源（Playback 实例）。</param>
    /// <param name="e">包含一组同时结束的音符信息。</param>
    private void OnNotesFinished(object? sender, NotesEventArgs e)
    {
        if (_syncContext == null) return;
        _syncContext.Post(_ =>
        {
            foreach (var note in e.Notes)
                NoteStopped?.Invoke(note.NoteNumber);
        }, null);
    }

    /// <summary>
    /// DryWetMidi 回调：曲目播放完毕时触发。封送到 UI 线程后引发 <see cref="Finished"/> 事件。
    /// </summary>
    /// <param name="sender">事件源（Playback 实例）。</param>
    /// <param name="e">空事件参数。</param>
    private void OnFinished(object? sender, EventArgs e)
    {
        if (_syncContext == null) return;
        _syncContext.Post(_ => Finished?.Invoke(), null);
    }

    /// <summary>
    /// 释放 Playback 和输出设备资源。
    /// 释放后此实例不再可用，需重新构造。
    /// </summary>
    public void Dispose()
    {
        _playback?.Dispose();
        _outputDevice?.Dispose();
    }
}
