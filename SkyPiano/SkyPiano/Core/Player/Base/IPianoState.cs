namespace SkyPiano.Core.Player.Base;

/// <summary>
/// 播放器状态接口：定义播放器向外暴露的只读状态属性。
/// ViewModel 通过此接口读取播放进度、曲目信息等，无需直接依赖具体实现。
/// </summary>
public interface IPianoState {
    /// <summary>当前是否正在播放。</summary>
    bool IsPlaying { get; }

    /// <summary>是否处于暂停状态。</summary>
    bool IsPaused { get; }

    /// <summary>当前播放位置。</summary>
    TimeSpan CurrentTime { get; }

    /// <summary>曲目总时长。</summary>
    TimeSpan Duration { get; }

    /// <summary>播放进度，0.0（开头）到 1.0（结尾）。</summary>
    double Progress { get; }

    /// <summary>播放列表中的曲目总数。</summary>
    int TrackCount { get; }

    /// <summary>当前曲目在列表中的索引（从 0 开始）。</summary>
    int CurrentTrackIndex { get; }

    /// <summary>当前曲目的完整文件路径（null 表示列表为空）。</summary>
    string? CurrentTrackPath { get; }

    /// <summary>播放列表中所有曲目的文件路径列表（只读）。</summary>
    IReadOnlyList<string> Tracks { get; }
}
