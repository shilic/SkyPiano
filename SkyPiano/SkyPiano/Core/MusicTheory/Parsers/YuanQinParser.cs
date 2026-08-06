using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using SkyPiano.Core.MusicTheory;

namespace SkyPiano.Core.MusicTheory.Parsers;

/// <summary>
/// 原琴（YuanQin）乐谱格式解析器。<br/>
/// JSON 格式：name/pause/short/longPause/split/toneStr。<br/>
/// toneStr 语法：(AB)=和弦、A=单音、/=长停顿、&lt;AB&gt; 英文中括号【】 =快速连弹。
/// </summary>
public static class YuanQinParser {
    private record YuanQinData(
        string name,
        int pause, int @short, int longPause,
        string split, string toneStr);

    /// <summary>将原琴 .yuan 文件转换为 MyScore。</summary>
    public static MyScore? YuanQinToScore(this string filePath) {
        var json = File.ReadAllText(filePath);
        YuanQinData? data = JsonSerializer.Deserialize<YuanQinData>(json);
        if (data == null || string.IsNullOrEmpty(data.toneStr)) return null;

        // 去除非键盘字符的注释（中文字段名、换行等，只保留 A-Za-z/()<>|）
        var cleaned = Regex.Replace(data.toneStr, @"[^A-Za-z/()<>|]", "");

        // 用原琴正则解析 toneStr
        var regex = new Regex(@"\((?<multi>[A-Za-z]{2,})\)|(?<single>[A-Za-z])|(?<longPause>/+)|\<(?<short>[A-Za-z]{2,})\>");
        var events = new List<MyNoteEvent>();
        long currentUs = 0;

        foreach (Match m in regex.Matches(cleaned)) {
            if (m.Groups["multi"].Success) {
                var keys = m.Groups["multi"].Value;
                // 和弦：所有键同时按下
                foreach (char c in keys) {
                    var note = char.ToUpperInvariant(c).ToMyNote();
                    events.Add(new MyNoteEvent(currentUs, note, true, data.@short));
                    events.Add(new MyNoteEvent(currentUs + data.@short, note, false));
                }
                currentUs += data.@short + data.pause * 1000L;
            }
            else if (m.Groups["single"].Success) {
                var c = m.Groups["single"].Value[0];
                var note = char.ToUpperInvariant(c).ToMyNote();
                events.Add(new MyNoteEvent(currentUs, note, true, data.@short));
                events.Add(new MyNoteEvent(currentUs + data.@short, note, false));
                currentUs += data.@short + data.pause * 1000L;
            }
            else if (m.Groups["longPause"].Success) {
                currentUs += data.longPause * 1000L;
            }
            else if (m.Groups["short"].Success) {
                var keys = m.Groups["short"].Value;
                // 快速连弹：每个键间隔 shortMills
                foreach (char c in keys)
                {
                    var note = char.ToUpperInvariant(c).ToMyNote();
                    events.Add(new MyNoteEvent(currentUs, note, true, data.@short));
                    events.Add(new MyNoteEvent(currentUs + data.@short, note, false));
                    currentUs += data.@short + data.@short * 1000L;
                }
            }
        }

        events.Sort((a, b) => {
            int cmp = a.TimeUs.CompareTo(b.TimeUs);
            return cmp != 0 ? cmp : a.IsPress.CompareTo(b.IsPress);
        });

        return new MyScore(data.name, events.ToArray(), TimeSpan.FromMicroseconds(currentUs));
    }
}
