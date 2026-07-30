using Melanchall.DryWetMidi.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkyPiano.SkyPiano.Core.MusicTheory.Base
{
    public class MidiToKeyboardMapper
    {
        // 键盘键位映射（示例：21个键，从C4到C6）
        private static readonly Dictionary<int, char> keyMapping = new Dictionary<int, char>
    {
        // 第一排（示例键位，需根据你的实际键盘调整）
        {60, 'a'}, // C4
        {61, 's'}, // C#4
        {62, 'd'}, // D4
        {63, 'f'}, // D#4
        {64, 'g'}, // E4
        {65, 'h'}, // F4
        {66, 'j'}, // F#4
        // 第二排...
    };

        public List<NoteEvent> ParseMidiToKeyEvents(string midiFilePath)
        {
            var events = new List<NoteEvent>();

            // 读取MIDI文件
            MidiFile? midiFile = MidiFile.Read(midiFilePath);

            // 获取所有音符
            var notes = midiFile.GetNotes();

            foreach (var note in notes)
            {
                // 过滤超出键盘范围的音符
                if (keyMapping.TryGetValue(note.NoteNumber, out char keyChar))
                {
                    events.Add(new NoteEvent
                    {
                        Key = keyChar,
                        StartTime = note.Time,      // 开始时间（tick）
                        EndTime = note.Time + note.Length,  // 结束时间
                        Velocity = note.Velocity    // 力度
                    });
                }
            }

            return events;
        }
    }

    public class NoteEvent
    {
        public char Key { get; set; }          // 对应的键盘按键
        public long StartTime { get; set; }    // 按下时间
        public long EndTime { get; set; }      // 松开时间
        public byte Velocity { get; set; }     // 音符力度
    }
}
