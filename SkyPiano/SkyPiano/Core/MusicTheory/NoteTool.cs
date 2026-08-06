using System.Reflection;
using System.Runtime.InteropServices;

namespace SkyPiano.Core.MusicTheory;

/// <summary>
/// <see cref="MyNote"/> 枚举的扩展函数：键盘字符、MIDI 编号、音名之间的相互转换。
/// </summary>
public static class NoteTool {
    /// <summary>MIDI 编号 → MyNote 的快速查找字典（含黑键→白键降级）。</summary>
    private static readonly Dictionary<int, MyNote> MidiToNote = [];
    /// <summary>键盘字符 → MyNote 的快速查找字典。</summary>
    private static readonly Dictionary<char, MyNote> CharToNote = [];
    /// <summary>MyNote → 特性的缓存。</summary>
    private static readonly Dictionary<MyNote, NoteInfoAttribute> InfoCache = [];

    static NoteTool() {
        foreach (MyNote note in Enum.GetValues<MyNote>()) {
            var info = note.GetNoteInfo();
            MidiToNote[(int)note] = note;
            CharToNote[info.KeyChar] = note;
            InfoCache[note] = info;
        }

        // 黑键映射到低一位白键：C#→C, D#→D, F#→F, G#→G, A#→A
        for (int i = 48; i <= 83; i++) {
            if (MidiToNote.ContainsKey(i)) continue;
            MidiToNote[i] = MidiToNote[i - 1];
        }
    }

    /// <summary>获取 MyNote 对应的 NoteInfo 特性（含音名和键盘字符）。</summary>
    public static NoteInfoAttribute GetNoteInfo(this MyNote note) {
        if (InfoCache.TryGetValue(note, out var cached))
            return cached;

        var field = typeof(MyNote).GetField(note.ToString());
        var info = field!.GetCustomAttribute<NoteInfoAttribute>()!;
        InfoCache[note] = info;
        return info;
    }
    // 音符转KeyChar和MIDI编号 , 不会抛异常，黑键会降级到低一位白键
    #region 音符转KeyChar和MIDI编号
    /// <summary>获取 MyNote 对应的键盘字符。</summary>
    public static char ToKeyChar(this MyNote note) => note.GetNoteInfo().KeyChar;
    /// <summary>获取 MyNote 对应的 MIDI 编号。</summary>
    public static int ToMidiNumber(this MyNote note) => (int)note;
    #endregion 音符转KeyChar和MIDI编号
    // midi编号转MyNote和KeyChar, 因为可能在枚举中不存在，所以返回可空类型
    #region midi编号转MyNote和KeyChar
    /// <summary>将 MIDI 编号转换为 MyNote。黑键自动降级到低一位白键。超出范围返回 null。</summary>
    public static MyNote? ToMyNote(this int midiNumber) => MidiToNote.TryGetValue(midiNumber, out var note) ? note : null;

    /// <summary>将 MIDI 编号直接转换为键盘字符。黑键自动降级。超出范围返回 null。</summary>
    public static char? ToKeyChar(this int midiNumber) => midiNumber.ToMyNote()?.ToKeyChar();
    #endregion midi编号转MyNote和KeyChar
    // KeyChar转MyNote和MIDI编号, 不会返回null, 不在范围内会抛异常
    #region KeyChar转MyNote和MIDI编号
    /// <summary>将键盘字符转换为 MyNote。不区分大小写。非法字符抛异常。</summary>
    /// <exception cref="ArgumentException">键盘字符不在 21 键映射中时抛出。</exception>
    public static MyNote ToMyNote(this char key) {
        var upper = char.ToUpperInvariant(key);
        return CharToNote.TryGetValue(upper, out var note)
            ? note
            : throw new ArgumentException($"键盘按键 '{key}' 不在 21 键映射范围内。");
    }
    /// <summary>将键盘字符直接转换为 MIDI 编号。不区分大小写。非法字符抛异常。</summary>
    public static int ToMidiNumber(this char key) => (int)key.ToMyNote();
    #endregion KeyChar转MyNote和MIDI编号

    #region 音符转虚拟键码
    /// <summary>Win32 VkKeyScan：将字符映射为虚拟键码。</summary>
    [DllImport("user32.dll")]
    private static extern short VkKeyScan(char ch);
    /// <summary>获取 MyNote 对应的 Windows 虚拟键码。</summary>
    public static byte ToVirtualKey(this MyNote note) {
        var result = VkKeyScan(char.ToUpperInvariant(note.ToKeyChar()));
        if (result == -1)
            throw new ArgumentException($"无法将字符 '{note.ToKeyChar()}' 转换为虚拟键码。");
        return (byte)(result & 0xFF);
    }
    #endregion 音符转虚拟键码
}
