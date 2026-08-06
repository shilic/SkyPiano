namespace SkyPiano.Core.Player.Base;

/// <summary>
/// 播放器事件接口。定义播放器向外部（ViewModel）通知状态变化的三个事件。
/// </summary>
public interface IPianoEvents {
    /// <summary>曲目切换时触发。参数为新的文件路径（null 表示列表为空）。</summary>
    event Action<string?>? TrackChanged;

    /// <summary>播放状态变化时触发（播放/暂停/切歌）。</summary>
    event Action? StateChanged;

    /// <summary>播放进度更新时触发。参数：progress(0~1)、currentTime。</summary>
    event Action<double, TimeSpan>? ProgressUpdated;
}
