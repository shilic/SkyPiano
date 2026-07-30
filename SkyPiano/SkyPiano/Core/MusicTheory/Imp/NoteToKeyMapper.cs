namespace SkyPiano.Core.MusicTheory.Imp;

/// <summary>
/// MIDI 音符编号 ↔ 键盘按键的单向/双向映射工具。
/// 覆盖 3 个八度 × 7 个白键 = 21 键，半音（黑键）映射到最近的低位白键。
/// </summary>
public static class NoteToKeyMapper
{
    /// <summary>
    /// 21 键映射表：键盘字符 → MIDI 音符编号。<br/>
    /// 下排（低八度 C3~B3）：Z→C3(48), X→D3(50), C→E3(52), V→F3(53), B→G3(55), N→A3(57), M→B3(59)<br/>
    /// 中排（中八度 C4~B4）：A→C4(60), S→D4(62), D→E4(64), F→F4(65), G→G4(67), H→A4(69), J→B4(71)<br/>
    /// 上排（高八度 C5~B5）：Q→C5(72), W→D5(74), E→E5(76), R→F5(77), T→G5(79), Y→A5(81), U→B5(83)
    /// </summary>
    private static readonly (char key, int midi)[] KeyMap =
    {
        // 下排：低八度（C3~B3），MIDI 48~59
        ('Z', 48), ('X', 50), ('C', 52), ('V', 53), ('B', 55), ('N', 57), ('M', 59),
        // 中排：中八度（C4~B4），MIDI 60~71
        ('A', 60), ('S', 62), ('D', 64), ('F', 65), ('G', 67), ('H', 69), ('J', 71),
        // 上排：高八度（C5~B5），MIDI 72~83
        ('Q', 72), ('W', 74), ('E', 76), ('R', 77), ('T', 79), ('Y', 81), ('U', 83),
    };

    /// <summary>MIDI 编号 → 键盘字符的快速查找字典。</summary>
    private static readonly Dictionary<int, char> MidiToKey = KeyMap.ToDictionary(x => x.midi, x => x.key);

    /// <summary>
    /// 将 MIDI 音符编号映射到对应的键盘按键字符。
    /// 白键直接映射，黑键（半音）映射到低一位的白键。
    /// 超出 48~83 范围的音符返回 <c>null</c>。
    /// </summary>
    /// <param name="midiNumber">MIDI 音符编号（0-127）。</param>
    /// <returns>对应的键盘字符（如 'A'），无匹配时返回 <c>null</c>。</returns>
    public static char? GetKeyForMidi(int midiNumber)
    {
        // 超出三组八度范围（48=C3 ~ 83=B5）则不映射
        if (midiNumber < 48 || midiNumber > 83) return null;

        // 直接命中白键
        if (MidiToKey.TryGetValue(midiNumber, out var key)) return key;

        // 黑键：向下查找最近的白键（如 C#→C, D#→D）
        return GetKeyForMidi(midiNumber - 1);
    }

    /// <summary>
    /// 将键盘字符映射到对应的 MIDI 音符编号。
    /// </summary>
    /// <param name="key">键盘字符（不区分大小写），如 'A'。</param>
    /// <returns>匹配的 MIDI 音符编号。</returns>
    /// <exception cref="ArgumentException">字符不在 21 键映射中时抛出此异常。</exception>
    public static int GetMidiForKey(char key)
    {
        var upper = char.ToUpperInvariant(key);
        foreach (var (k, midi) in KeyMap)
        {
            if (k == upper) return midi;
        }
        throw new ArgumentException($"键盘按键 '{key}' 不在 21 键映射范围内。");
    }
}
