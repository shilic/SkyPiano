namespace SkyPiano.Core.MusicTheory;

/// <summary>
/// 原子音符事件：在指定时刻按下或释放一个 <see cref="MyNote"/>。
/// 乐谱 <see cref="Score"/> 中存储的最小单位。
/// </summary>
/// <param name="TimeUs">事件触发时间（微秒）。</param>
/// <param name="Note">对应的 21 键音符。</param>
/// <param name="IsPress"><c>true</c> 表示按下，<c>false</c> 表示释放。</param>
/// <param name="LengthUs">音符持续时长（微秒）。按下事件携带真实长度，释放事件为 0。</param>
public record NoteEvent(long TimeUs, MyNote Note, bool IsPress, long LengthUs = 0);
