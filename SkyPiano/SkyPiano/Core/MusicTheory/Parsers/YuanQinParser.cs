using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using SkyPiano.Core.MusicTheory;

namespace SkyPiano.Core.MusicTheory.Parsers;

/// <summary>原琴（YuanQin）乐谱格式解析器（.yuan）。</summary>
public class YuanQinParser : IScoreParser
{
    /// <inheritdoc />
    public string Extension => ".yuan";

    private record YuanQinData(
        string name,
        int pause, int @short, int longPause,
        string split, string toneStr);

    /// <inheritdoc />
    public MyScore? Parse(string filePath)
    {
        var json = File.ReadAllText(filePath);
        var data = JsonSerializer.Deserialize<YuanQinData>(json);
        if (data == null || string.IsNullOrEmpty(data.toneStr)) return null;

        var cleaned = Regex.Replace(data.toneStr, @"[^A-Za-z/()<>|]", "");

        var regex = new Regex(@"\((?<multi>[A-Za-z]{2,})\)|(?<single>[A-Za-z])|(?<longPause>/+)|\<(?<short>[A-Za-z]{2,})\>");
        var events = new List<MyNoteEvent>();
        long currentUs = 0;

        foreach (Match m in regex.Matches(cleaned))
        {
            if (m.Groups["multi"].Success)
            {
                var keys = m.Groups["multi"].Value;
                foreach (char c in keys)
                {
                    var note = char.ToUpperInvariant(c).ToMyNote();
                    events.Add(new MyNoteEvent(currentUs, note, true, data.@short));
                    events.Add(new MyNoteEvent(currentUs + data.@short, note, false));
                }
                currentUs += data.@short + data.pause * 1000L;
            }
            else if (m.Groups["single"].Success)
            {
                var c = m.Groups["single"].Value[0];
                var note = char.ToUpperInvariant(c).ToMyNote();
                events.Add(new MyNoteEvent(currentUs, note, true, data.@short));
                events.Add(new MyNoteEvent(currentUs + data.@short, note, false));
                currentUs += data.@short + data.pause * 1000L;
            }
            else if (m.Groups["longPause"].Success)
            {
                currentUs += data.longPause * 1000L;
            }
            else if (m.Groups["short"].Success)
            {
                var keys = m.Groups["short"].Value;
                foreach (char c in keys)
                {
                    var note = char.ToUpperInvariant(c).ToMyNote();
                    events.Add(new MyNoteEvent(currentUs, note, true, data.@short));
                    events.Add(new MyNoteEvent(currentUs + data.@short, note, false));
                    currentUs += data.@short + data.@short * 1000L;
                }
            }
        }

        events.Sort((a, b) =>
        {
            int cmp = a.TimeUs.CompareTo(b.TimeUs);
            return cmp != 0 ? cmp : a.IsPress.CompareTo(b.IsPress);
        });

        return new MyScore(data.name, events.ToArray(), TimeSpan.FromMicroseconds(currentUs));
    }
}
