using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using SkyPiano.Core.MusicTheory;
using System.IO;
using MyNoteEvent = SkyPiano.Core.MusicTheory.MyNoteEvent;

namespace SkyPiano.SkyPiano.Core.MusicTheory;

/// <summary>
/// 乐谱：全曲原子事件的纯数据容器。<br/>
/// 字典键为序号（0..N-1），值为 <see cref="MyNoteEvent"/>。<br/>
/// 序号可直接用于暂停/恢复时记录播放进度。
/// </summary>
/// <param name="Name">乐谱名称。</param>
/// <param name="Events">序号 → 原子事件的字典。</param>
/// <param name="Duration">全曲总时长。</param>
public record MyScore(string Name, IReadOnlyDictionary<int, MyNoteEvent> Events, TimeSpan Duration);

/// <summary> 乐谱构建器：从 MIDI 文件解析并生成 <see cref="MyScore"/>。  </summary>
public static class ScoreParser {
    /// <summary> 从 MIDI 文件构建乐谱。内部将每个音符拆为按下+释放两个原子事件，按时间排序。 </summary>
    public static MyScore FromMidiFile(this string filePath) {
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

        var events = list.Select((e, i) => (index: i, evt: e)).ToDictionary(x => x.index, x => x.evt);

        var duration = midiFile.GetDuration<MetricTimeSpan>();
        var name = Path.GetFileNameWithoutExtension(filePath);
        return new MyScore(name, events, TimeSpan.FromMicroseconds(duration.TotalMicroseconds));
    }
}