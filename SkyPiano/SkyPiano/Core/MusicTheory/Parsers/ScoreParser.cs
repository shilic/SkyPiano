using System.IO;

namespace SkyPiano.Core.MusicTheory.Parsers;

/// <summary>
/// 乐谱构建器：统一注册表 + 后缀自动分派。
/// 所有 <see cref="IScoreParser"/> 在此注册，通过字典按扩展名分派。
/// 新增格式只需 new 一个解析器并 Register，无需修改其他代码。
/// </summary>
public static class ScoreParser {
    /// <summary>扩展名 → 解析器。</summary>
    private static readonly Dictionary<string, IScoreParser> Parsers = [];
    /// <summary>支持的文件后缀列表（由注册的解析器自动生成）。</summary>
    public static string[] SupportedExtensions => Parsers.Keys.ToArray();
    /// <summary>注册解析器。通常在解析器类的静态构造中调用。</summary>
    public static void Register(IScoreParser parser) => Parsers[parser.Extension] = parser;
    static ScoreParser() {
        Register(new MidiParser());
        Register(new MusicXmlParser());
        Register(new YuanQinParser());
    }
    /// <summary>
    /// 统一入口：根据扩展名从注册表中查找解析器。
    /// 新增格式只需 Register，此处无需改动。
    /// </summary>
    /// <returns>解析成功返回 MyScore，不支持的格式返回 null。</returns>
    public static MyScore? ToScore(this string filePath)
        => Parsers.TryGetValue(Path.GetExtension(filePath).ToLowerInvariant(), out var p)
            ? p.Parse(filePath)
            : null;
}
