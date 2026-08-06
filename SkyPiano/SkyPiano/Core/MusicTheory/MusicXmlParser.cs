using System.IO;
using System.Xml.Linq;

namespace SkyPiano.Core.MusicTheory;

/// <summary>
/// MusicXML → MyScore 转换器。处理单声部（Part）、单旋律线，忽略和弦和装饰音。
/// 读取 &lt;note&gt; 节点中的 &lt;step&gt;&lt;octave&gt;&lt;duration&gt;，
/// 用 &lt;divisions&gt; + 当前 tempo 换算为微秒时间。
/// </summary>
public static class MusicXmlParser
{
    private static readonly Dictionary<string, int> StepToOffset = new()
    {
        ["C"] = 0, ["D"] = 2, ["E"] = 4, ["F"] = 5, ["G"] = 7, ["A"] = 9, ["B"] = 11,
    };

    /// <summary>解析 MusicXML 文件（.musicxml / .mxl 未压缩），返回 MyScore。</summary>
    public static MyScore XmlToScore(this string filePath)
    {
        var doc = XDocument.Load(filePath);
        var root = doc.Root!;
        XNamespace ns = root.GetDefaultNamespace();

        // 获取第一个声部
        var part = root.Element(ns + "part");
        if (part == null) return EmptyScore(filePath);

        // 获取 divisions（每四分音符的刻度数）
        var divisions = int.Parse(root.Descendants(ns + "divisions").First().Value);

        // 默认速度：120 BPM = 每四分音符 500000 微秒
        int tempo = 500000;
        var sound = root.Descendants(ns + "sound").FirstOrDefault();
        if (sound != null)
            tempo = int.Parse(sound.Attribute("tempo")?.Value ?? "120") * 1000; // bpm → us/beat
        else
        {
            var tempoEl = root.Descendants(ns + "per-minute").FirstOrDefault();
            if (tempoEl != null) tempo = 60_000_000 / int.Parse(tempoEl.Value);
        }

        // 遍历所有 note 元素
        var events = new List<MyNoteEvent>();
        int currentTick = 0;
        var pendingPress = new Dictionary<string, (MyNote note, int startTick)>(); // 按 pitch 去重

        foreach (var measure in part.Elements(ns + "measure"))
        {
            foreach (var noteEl in measure.Elements(ns + "note"))
            {
                var duration = noteEl.Element(ns + "duration")?.Value;
                var type = noteEl.Element(ns + "type")?.Value;    // whole/half/quarter/...

                // 跳过休止符（但仍然推进 tick）
                if (noteEl.Element(ns + "rest") != null) goto advance;

                var step = noteEl.Element(ns + "step")?.Value;
                var octave = noteEl.Element(ns + "octave")?.Value;
                var chord = noteEl.Element(ns + "chord");

                if (step == null || octave == null) goto advance;

                // 计算 MIDI 编号：C4=60, 偏移 + (octave+1)*12
                int midi = StepToOffset[step] + (int.Parse(octave) + 1) * 12;
                var myNote = midi.ToMyNote();
                if (myNote == null) goto advance;

                string pitch = $"{step}{octave}";

                if (chord != null)
                {
                    // 和弦音：共用上一个音符的开始时间
                    if (pendingPress.TryGetValue(pitch, out _)) continue;
                    pendingPress[pitch] = (myNote.Value, currentTick);
                }
                else if (pendingPress.TryGetValue(pitch, out var pending))
                {
                    // 同音高再次出现，先释放上一个
                    int length = currentTick - pending.startTick;
                    if (length > 0)
                    {
                        long startUs = TicksToUs(pending.startTick, divisions, tempo);
                        long endUs = TicksToUs(currentTick, divisions, tempo);
                        events.Add(new MyNoteEvent(startUs, pending.note, true, endUs - startUs));
                        events.Add(new MyNoteEvent(endUs, pending.note, false));
                    }
                    pendingPress[pitch] = (myNote.Value, currentTick);
                }
                else
                {
                    pendingPress[pitch] = (myNote.Value, currentTick);
                }

            advance:
                if (duration != null)
                    currentTick += int.Parse(duration);
            }
        }

        // 释放所有未释放的音符（每个持续到最后一个 measure 结束）
        foreach (var (_, pending) in pendingPress)
        {
            long startUs = TicksToUs(pending.startTick, divisions, tempo);
            long endUs = TicksToUs(currentTick, divisions, tempo);
            if (endUs > startUs)
            {
                events.Add(new MyNoteEvent(startUs, pending.note, true, endUs - startUs));
                events.Add(new MyNoteEvent(endUs, pending.note, false));
            }
        }

        events.Sort((a, b) =>
        {
            int cmp = a.TimeUs.CompareTo(b.TimeUs);
            return cmp != 0 ? cmp : a.IsPress.CompareTo(b.IsPress);
        });

        var name = Path.GetFileNameWithoutExtension(filePath);
        var totalUs = events.Count > 0 ? events.Max(e => e.TimeUs) : 0;
        return new MyScore(name, events.ToArray(), TimeSpan.FromMicroseconds(totalUs));
    }

    private static long TicksToUs(int ticks, int divisions, int tempo)
        => (long)(ticks / (double)divisions * tempo);

    private static MyScore EmptyScore(string filePath)
        => new(Path.GetFileNameWithoutExtension(filePath), [], TimeSpan.Zero);
}
