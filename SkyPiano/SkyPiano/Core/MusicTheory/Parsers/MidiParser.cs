using System.IO;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using SkyPiano.Core.MusicTheory;

namespace SkyPiano.Core.MusicTheory.Parsers;

/// <summary>
/// MIDI 文件解析器（.mid / .midi）。
/// </summary>
public static class MidiParser {
    /// <summary>从 MIDI 文件构建乐谱。非 .mid / .midi 后缀直接报错。</summary>
    /// <exception cref="ArgumentException">文件后缀不是 .mid 或 .midi 时抛出。</exception>
    public static MyScore MidiFileToScore(this string filePath) {
        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        if (ext is not ".mid" and not ".midi")
            throw new ArgumentException($"文件后缀 '{ext}' 不是有效的 MIDI 格式（需为 .mid 或 .midi）。");

        var midiFile = MidiFile.Read(filePath);
        var tempoMap = midiFile.GetTempoMap();
        ICollection<Note> notes = midiFile.GetNotes();

        List<MyNoteEvent> list = new(notes.Count * 2);
        foreach (Note note in notes) {
            var myNote = ((int)note.NoteNumber).ToMyNote();
            if (myNote == null) continue;

            var startUs = note.TimeAs<MetricTimeSpan>(tempoMap).TotalMicroseconds;
            var lengthUs = note.LengthAs<MetricTimeSpan>(tempoMap).TotalMicroseconds;
            // 将每个 KeyEvent 拆分为按下(正时间)和释放(负标记)两个原子事件
            list.Add(new MyNoteEvent(startUs, myNote.Value, true, lengthUs));
            list.Add(new MyNoteEvent(startUs + lengthUs, myNote.Value, false));
        }
        // 按时间升序排列，同时间按下优先于释放
        list.Sort((a, b) => {
            // 时间小的排前面
            int cmp = a.TimeUs.CompareTo(b.TimeUs);
            // 如果时间不同，直接返回结果  // 如果时间相同（同时发生），按下排在松开前面
            return cmp != 0 ? cmp : a.IsPress.CompareTo(b.IsPress);
        });

        var events = list.ToArray();
        var duration = midiFile.GetDuration<MetricTimeSpan>();
        var name = Path.GetFileNameWithoutExtension(filePath);
        return new MyScore(name, events, TimeSpan.FromMicroseconds(duration.TotalMicroseconds));
    }
}
