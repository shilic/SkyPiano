namespace SkyPiano.Core.MusicTheory;

/// <summary>
/// 21 键音符枚举。值直接等于 MIDI 音符编号。
/// 通过 <see cref="NoteInfoAttribute"/> 获取音名和键盘字符。
/// <code>(int)myNote</code> 可获得 MIDI 编号。
/// </summary>
public enum MyNote : int {
    // ---- 低八度 C3~B3 ----
    [NoteInfo("C3", 'Z', 48)] doDown = 48,
    [NoteInfo("D3", 'X', 50)] reDown = 50,
    [NoteInfo("E3", 'C', 52)] miDown = 52,
    [NoteInfo("F3", 'V', 53)] faDown = 53,
    [NoteInfo("G3", 'B', 55)] soDown = 55,
    [NoteInfo("A3", 'N', 57)] laDown = 57,
    [NoteInfo("B3", 'M', 59)] tiDown = 59,

    // ---- 中八度 C4~B4 ----
    [NoteInfo("C4", 'A', 60)] Do = 60,
    [NoteInfo("D4", 'S', 62)] Re = 62,
    [NoteInfo("E4", 'D', 64)] Mi = 64,
    [NoteInfo("F4", 'F', 65)] Fa = 65,
    [NoteInfo("G4", 'G', 67)] So = 67,
    [NoteInfo("A4", 'H', 69)] La = 69,
    [NoteInfo("B4", 'J', 71)] Ti = 71,

    // ---- 高八度 C5~B5 ----
    [NoteInfo("C5", 'Q', 72)] doUp = 72,
    [NoteInfo("D5", 'W', 74)] reUp = 74,
    [NoteInfo("E5", 'E', 76)] miUp = 76,
    [NoteInfo("F5", 'R', 77)] faUp = 77,
    [NoteInfo("G5", 'T', 79)] soUp = 79,
    [NoteInfo("A5", 'Y', 81)] laUp = 81,
    [NoteInfo("B5", 'U', 83)] tiUp = 83,
}
