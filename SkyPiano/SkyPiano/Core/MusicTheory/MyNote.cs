namespace SkyPiano.Core.MusicTheory;

/// <summary>
/// 21 键音符枚举。值直接等于 MIDI 音符编号。
/// 通过 <see cref="NoteInfoAttribute"/> 获取音名和键盘字符。
/// <code>(int)myNote</code> 可获得 MIDI 编号。
/// </summary>
public enum MyNote : int {
    // ---- 低八度 C3~B3 ----
    [NoteInfo("C3", 'Z')] doDown = 48,
    [NoteInfo("D3", 'X')] reDown = 50,
    [NoteInfo("E3", 'C')] miDown = 52,
    [NoteInfo("F3", 'V')] faDown = 53,
    [NoteInfo("G3", 'B')] soDown = 55,
    [NoteInfo("A3", 'N')] laDown = 57,
    [NoteInfo("B3", 'M')] tiDown = 59,

    // ---- 中八度 C4~B4 ----
    [NoteInfo("C4", 'A')] Do = 60,
    [NoteInfo("D4", 'S')] Re = 62,
    [NoteInfo("E4", 'D')] Mi = 64,
    [NoteInfo("F4", 'F')] Fa = 65,
    [NoteInfo("G4", 'G')] So = 67,
    [NoteInfo("A4", 'H')] La = 69,
    [NoteInfo("B4", 'J')] Ti = 71,

    // ---- 高八度 C5~B5 ----
    [NoteInfo("C5", 'Q')] doUp = 72,
    [NoteInfo("D5", 'W')] reUp = 74,
    [NoteInfo("E5", 'E')] miUp = 76,
    [NoteInfo("F5", 'R')] faUp = 77,
    [NoteInfo("G5", 'T')] soUp = 79,
    [NoteInfo("A5", 'Y')] laUp = 81,
    [NoteInfo("B5", 'U')] tiUp = 83,
}
