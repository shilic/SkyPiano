namespace SkyPiano.Core.MusicTheory;

/// <summary>
/// 乐谱：全曲原子事件的纯数据容器。<br/>
/// 数组索引即序号（0..N-1），值为 <see cref="MyNoteEvent"/>。<br/>
/// 序号可直接用于暂停/恢复时记录播放进度。
/// </summary>
/// <param name="Name">乐谱名称。</param>
/// <param name="Events">按序号排序的原子事件数组。</param>
/// <param name="Duration">全曲总时长。</param>
public record MyScore(string Name, MyNoteEvent[] Events, TimeSpan Duration);
