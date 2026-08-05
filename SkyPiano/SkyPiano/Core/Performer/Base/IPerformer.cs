namespace SkyPiano.Core.Performer.Base;

/// <summary>
/// 演奏者接口，定义键盘按键的按下与释放操作。<br></br>
/// 由不同的实现类决定如何将按键事件传递到目标（如 Win32 API、SendKeys 等）。<br></br>
/// 键盘就绑定到对应的键盘按键，鼠标就绑定到对应的屏幕坐标。实现解耦。 <br></br>
/// </summary>
public interface IPerformer {
    /// <summary> 按下指定的键盘按键。 </summary>
    /// <param name="key">键盘字符（如 'A'），不区分大小写。</param>
    void KeyPress(char key);

    /// <summary>
    /// 释放指定的键盘按键。
    /// </summary>
    /// <param name="key">键盘字符（如 'A'），不区分大小写。</param>
    void KeyRelease(char key);
}
