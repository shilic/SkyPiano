using System.IO;
using System.Xml.Linq;
using SkyPiano.Core.MusicTheory;

namespace SkyPiano.Core.MusicTheory.Parsers;

/// <summary>MusicXML 文件解析器（.musicxml）。</summary>
public class MusicXmlParser : IScoreParser
{
    /// <inheritdoc />
    public string Extension => ".musicxml";

    private static readonly Dictionary<string, int> StepToOffset = new()
    {
        ["C"] = 0, ["D"] = 2, ["E"] = 4, ["F"] = 5, ["G"] = 7, ["A"] = 9, ["B"] = 11,
    };

    /// <inheritdoc />
    public MyScore? Parse(string filePath)
    {
        var doc = XDocument.Load(filePath);
        var root = doc.Root!;
        XNamespace ns = root.GetDefaultNamespace();

        var part = root.Element(ns + "part");
        if (part == null) return EmptyScore(filePath);

        var divisions = int.Parse(root.Descendants(ns + "divisions").First().Value);

        int tempo = 500000;
        var sound = root.Descendants(ns + "sound").FirstOrDefault();
        if (sound != null)
            tempo = int.Parse(sound.Attribute("tempo")?.Value ?? "120") * 1000;
        else
        {
            var tempoEl = root.Descendants(ns + "per-minute").FirstOrDefault();
            if (tempoEl != null) tempo = 60_000_000 / int.Parse(tempoEl.Value);
        }

        var events = new List<MyNoteEvent>();
        int currentTick = 0;
        var pendingPress = new Dictionary<string, (MyNote note, int startTick)>();

        foreach (var measure in part.Elements(ns + "measure"))
        {
            foreach (var noteEl in measure.Elements(ns + "note"))
            {
                var duration = noteEl.Element(ns + "duration")?.Value;

                if (noteEl.Element(ns + "rest") != null) goto advance;

                var step = noteEl.Element(ns + "step")?.Value;
                var octave = noteEl.Element(ns + "octave")?.Value;
                var chord = noteEl.Element(ns + "chord");

                if (step == null || octave == null) goto advance;

                int midi = StepToOffset[step] + (int.Parse(octave) + 1) * 12;
                var myNote = midi.ToMyNote();
                if (myNote == null) goto advance;

                string pitch = $"{step}{octave}";

                if (chord != null)
                {
                    if (pendingPress.TryGetValue(pitch, out _)) continue;
                    pendingPress[pitch] = (myNote.Value, currentTick);
                }
                else if (pendingPress.TryGetValue(pitch, out var pending))
                {
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
