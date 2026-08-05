namespace SkyPiano.Core.MusicTheory;

/// <summary>
/// 乐谱：全曲原子事件的纯数据容器。<br/>
/// 字典键为序号（0..N-1），值为 <see cref="NoteEvent"/>。<br/>
/// 序号可直接用于暂停/恢复时记录播放进度。
/// </summary>
/// <param name="Name">乐谱名称。</param>
/// <param name="Events">序号 → 原子事件的字典。</param>
/// <param name="Duration">全曲总时长。</param>
public record Score(string Name, IReadOnlyDictionary<int, NoteEvent> Events, TimeSpan Duration);
