using System.Windows.Input;

namespace SkyPiano.Core.MusicTheory;

/// <summary>
/// 附加在 <see cref="MyNote"/> 枚举成员上的额外信息：音名和键盘字符。
/// </summary>
/// <remarks>
/// 构造 NoteInfo 特性。
/// </remarks>
/// <param name="name">音名，如 "C3"。</param>
/// <param name="keyChar">键盘字符，如 'A'。</param>
/// <param name="midiNumber">MIDI 数字，如 60 表示 C4。</param>
[AttributeUsage(AttributeTargets.Field)]
public class NoteInfoAttribute(string name, char keyChar, int midiNumber, Key key) : Attribute {
    /// <summary>音名，如 "C3"、"D4"、"B5"。</summary>
    public string Name { get; } = name;
    /// <summary>键盘字符，如 'A'、'Z'。</summary>
    public char KeyChar { get; } = keyChar;
    public int MidiNumber { get; } = midiNumber;
    public Key Key { get; } = key;
}
