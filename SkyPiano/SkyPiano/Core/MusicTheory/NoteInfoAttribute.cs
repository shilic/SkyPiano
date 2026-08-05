namespace SkyPiano.Core.MusicTheory;

/// <summary>
/// 附加在 <see cref="MyNote"/> 枚举成员上的额外信息：音名和键盘字符。
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public class NoteInfoAttribute : Attribute {
    /// <summary>音名，如 "C3"、"D4"、"B5"。</summary>
    public string Name { get; }

    /// <summary>键盘字符，如 'A'、'Z'。</summary>
    public char KeyChar { get; }

    /// <summary>
    /// 构造 NoteInfo 特性。
    /// </summary>
    /// <param name="name">音名，如 "C3"。</param>
    /// <param name="keyChar">键盘字符，如 'A'。</param>
    public NoteInfoAttribute(string name, char keyChar) {
        Name = name;
        KeyChar = keyChar;
    }
}
