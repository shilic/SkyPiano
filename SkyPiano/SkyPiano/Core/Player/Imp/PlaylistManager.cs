using System.IO;

namespace SkyPiano.Core.Player.Imp;

/// <summary>
/// 播放列表管理器，负责保存曲目和切换曲目。
/// 支持循环播放：上一首/下一首在边界处自动环绕。
/// </summary>
public class PlaylistManager
{
    /// <summary>内部维护的 MIDI 文件路径数组。</summary>
    private string[] _tracks = [];

    /// <summary>当前曲目索引，-1 表示列表为空。</summary>
    private int _currentIndex = -1;

    /// <summary>
    /// 播放列表中的曲目总数。
    /// </summary>
    public int Count => _tracks.Length;

    /// <summary>
    /// 当前曲目在列表中的索引（从 0 开始）。列表为空时返回 -1。
    /// </summary>
    public int CurrentIndex => _currentIndex;

    /// <summary>
    /// 当前曲目的完整文件路径。列表为空时返回 <c>null</c>。
    /// </summary>
    public string? CurrentTrack =>
        _currentIndex >= 0 && _currentIndex < _tracks.Length ? _tracks[_currentIndex] : null;

    /// <summary>
    /// 播放列表中所有曲目的文件路径列表（只读）。
    /// </summary>
    public IReadOnlyList<string> Tracks => _tracks;

    /// <summary>
    /// 当曲目发生切换时触发。参数为新的曲目文件路径，列表为空时为 <c>null</c>。
    /// </summary>
    public event Action<string?>? TrackChanged;

    /// <summary>
    /// 从指定文件夹加载所有 .mid 文件，按文件名排序后作为播放列表。
    /// 加载后自动选中第一首曲目并触发 <see cref="TrackChanged"/> 事件。
    /// 如果文件夹中无 .mid 文件，列表清空，<see cref="CurrentIndex"/> 设为 -1。
    /// </summary>
    /// <param name="folderPath">包含 .mid 文件的文件夹路径。</param>
    /// <exception cref="DirectoryNotFoundException">指定的文件夹不存在时抛出此异常。</exception>
    public void LoadFromFolder(string folderPath)
    {
        // 扫描顶层目录中的所有 .mid 文件，按文件名排序
        _tracks = Directory.GetFiles(folderPath, "*.mid", SearchOption.TopDirectoryOnly)
            .OrderBy(f => f)
            .ToArray();

        // 有文件则选中第一首，否则标记为空列表
        _currentIndex = _tracks.Length > 0 ? 0 : -1;

        // 通知外部曲目已切换
        TrackChanged?.Invoke(CurrentTrack);
    }

    /// <summary>
    /// 选中指定索引的曲目并触发 <see cref="TrackChanged"/> 事件。
    /// 如果索引相同则不重复触发。
    /// </summary>
    /// <param name="index">要选中的曲目索引（从 0 开始）。</param>
    /// <exception cref="ArgumentOutOfRangeException">索引超出范围时抛出此异常。</exception>
    public void SelectTrack(int index)
    {
        if (index < 0 || index >= _tracks.Length)
            throw new ArgumentOutOfRangeException(nameof(index));

        // 避免重复选中同一首曲目时重复触发事件
        if (index == _currentIndex) return;

        _currentIndex = index;
        TrackChanged?.Invoke(CurrentTrack);
    }

    /// <summary>
    /// 切换到下一首曲目。到达列表末尾时自动循环回第一首。
    /// 如果列表为空则无操作。
    /// </summary>
    public void MoveNext() {
        if (_tracks.Length == 0) return;

        // 取模运算实现循环：最后一首之后回到第一首
        _currentIndex = (_currentIndex + 1) % _tracks.Length;
        TrackChanged?.Invoke(CurrentTrack);
    }

    /// <summary>
    /// 切换到上一首曲目。到达列表开头时自动循环到最后一首。
    /// 如果列表为空则无操作。
    /// </summary>
    public void MovePrevious()
    {
        if (_tracks.Length == 0) return;

        // 加上 Length 后取模，确保在索引为 0 时能回绕到列表末尾
        _currentIndex = (_currentIndex - 1 + _tracks.Length) % _tracks.Length;
        TrackChanged?.Invoke(CurrentTrack);
    }
}
