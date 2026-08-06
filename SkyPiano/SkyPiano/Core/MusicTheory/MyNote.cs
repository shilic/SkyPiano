using System.Windows.Input;

namespace SkyPiano.Core.MusicTheory;

/// <summary>
/// 21 键音符枚举。<br></br>
/// 值直接等于 MIDI 音符编号。<br></br>
/// 通过 <see cref="NoteInfoAttribute"/> 获取音名和键盘字符。<br></br>
/// <code>(int)myNote</code> 可获得 MIDI 编号。
/// </summary>
public enum MyNote : int {
    // ---- 低八度 C3~B3 ----
    [NoteInfo("C3", 'Z', 48, Key.Z)] doDown = 48,
    [NoteInfo("D3", 'X', 50, Key.X)] reDown = 50,
    [NoteInfo("E3", 'C', 52, Key.C)] miDown = 52,
    [NoteInfo("F3", 'V', 53, Key.V)] faDown = 53,
    [NoteInfo("G3", 'B', 55, Key.B)] soDown = 55,
    [NoteInfo("A3", 'N', 57, Key.N)] laDown = 57,
    [NoteInfo("B3", 'M', 59, Key.M)] tiDown = 59,

    // ---- 中八度 C4~B4 ----
    [NoteInfo("C4", 'A', 60, Key.A)] Do = 60,
    [NoteInfo("D4", 'S', 62, Key.S)] Re = 62,
    [NoteInfo("E4", 'D', 64, Key.D)] Mi = 64,
    [NoteInfo("F4", 'F', 65, Key.F)] Fa = 65,
    [NoteInfo("G4", 'G', 67, Key.G)] So = 67,
    [NoteInfo("A4", 'H', 69, Key.H)] La = 69,
    [NoteInfo("B4", 'J', 71, Key.J)] Ti = 71,

    // ---- 高八度 C5~B5 ----
    [NoteInfo("C5", 'Q', 72, Key.Q)] doUp = 72,
    [NoteInfo("D5", 'W', 74, Key.W)] reUp = 74,
    [NoteInfo("E5", 'E', 76, Key.E)] miUp = 76,
    [NoteInfo("F5", 'R', 77, Key.R)] faUp = 77,
    [NoteInfo("G5", 'T', 79, Key.T)] soUp = 79,
    [NoteInfo("A5", 'Y', 81, Key.Y)] laUp = 81,
    [NoteInfo("B5", 'U', 83, Key.U)] tiUp = 83,
}
