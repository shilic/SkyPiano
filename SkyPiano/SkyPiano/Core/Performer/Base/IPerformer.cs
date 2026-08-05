using SkyPiano.Core.MusicTheory;

namespace SkyPiano.Core.Performer.Base;

/// <summary>
/// 演奏者接口，定义按键的按下与释放操作。<br></br>
/// 参数使用 <see cref="MyNote"/> 枚举，确保不会传入非法按键。<br></br>
/// </summary>
public interface IPerformer {
    /// <summary>按下指定的音符按键。</summary>
    void KeyPress(MyNote note);
    /// <summary>释放指定的音符按键。</summary>
    void KeyRelease(MyNote note);
}
