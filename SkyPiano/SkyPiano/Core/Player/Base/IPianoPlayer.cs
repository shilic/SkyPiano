namespace SkyPiano.Core.Player.Base;

/// <summary>
/// 钢琴播放器接口：组合所有子接口 —— 播放控制、事件通知、状态查询。<br></br>
/// 实现此接口的类即可作为 ViewModel 的唯一 Model 层依赖。<br></br>
/// </summary>
public interface IPianoPlayer : 替身使者, IPianoEvents, IPianoState, IDisposable {
}
