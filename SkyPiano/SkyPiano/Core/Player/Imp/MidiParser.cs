using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using SkyPiano.Core.MusicTheory.Imp;

namespace SkyPiano.Core.Player.Imp;

/// <summary>
/// MIDI 文件解析结果：一个按键事件，包含按键字符、开始时间、结束时间。
/// </summary>
public class KeyEvent
{
    /// <summary>要按下的键盘字符，如 'A'。</summary>
    public char Key { get; init; }

    /// <summary>按键开始时间（微秒）。</summary>
    public long StartMicroseconds { get; init; }

    /// <summary>按键结束时间（微秒）。</summary>
    public long EndMicroseconds { get; init; }
}

/// <summary>
/// MIDI 文件解析器，将 .mid 文件中的音符转换为 <see cref="KeyEvent"/> 列表。
/// 每个 MIDI 音符映射到一个键盘字符，半音映射到最近的白键。
/// 音符通过 DryWetMidi 的 <see cref="TempoMap"/> 处理速度变化后的实际时间。
/// </summary>
public static class MidiParser
{
    /// <summary>
    /// 解析 MIDI 文件，返回按键事件列表和总时长。
    /// 音符已按开始时间升序排列。
    /// </summary>
    /// <param name="filePath">MIDI 文件的完整路径。</param>
    /// <returns>包含按键事件列表和总时长的元组。</returns>
    /// <exception cref="FileNotFoundException">文件不存在时抛出此异常。</exception>
    public static (List<KeyEvent> Events, TimeSpan Duration) Parse(string filePath)
    {
        var midiFile = MidiFile.Read(filePath);
        var tempoMap = midiFile.GetTempoMap();

        // 获取所有音符，DryWetMidi 自动处理 TempoMap 换算
        var notes = midiFile.GetNotes();

        var events = new List<KeyEvent>();
        foreach (var note in notes) {
            // 将 MIDI 音符编号映射到键盘按键，无映射则跳过
            var key = NoteToKeyMapper.GetKeyForMidi(note.NoteNumber);
            if (key == null) continue;

            // 将 DryWetMidi 的时间转换为微秒
            var startUs = note.TimeAs<MetricTimeSpan>(tempoMap).TotalMicroseconds;
            var lengthUs = note.LengthAs<MetricTimeSpan>(tempoMap).TotalMicroseconds;

            events.Add(new KeyEvent {
                Key = key.Value,
                StartMicroseconds = startUs,
                EndMicroseconds = startUs + lengthUs,
            });
        }

        // 按开始时间升序排列
        events.Sort((a, b) => a.StartMicroseconds.CompareTo(b.StartMicroseconds));

        // 计算总时长
        var duration = midiFile.GetDuration<MetricTimeSpan>();
        var durationTs = TimeSpan.FromMicroseconds(duration.TotalMicroseconds);

        return (events, durationTs);
    }
}
