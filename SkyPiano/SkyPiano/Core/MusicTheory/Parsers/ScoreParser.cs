using System.IO;

namespace SkyPiano.Core.MusicTheory.Parsers;

/// <summary>
/// 乐谱构建器：统一入口 + 后缀自动分派。
/// 所有解析器放在 Parsers 文件夹下，各管一种格式。
/// </summary>
public static class ScoreParser {
    /// <summary>支持的文件后缀列表。新增格式在此加一项，文件夹扫描自动生效。</summary>
    public static readonly string[] SupportedExtensions = [".mid", ".musicxml", ".yuan"];

    /// <summary>
    /// 统一入口：根据扩展名自动分派。
    /// 新增格式只需在此加一个 case。
    /// </summary>
    /// <returns>解析成功返回 MyScore，不支持的格式返回 null。</returns>
    public static MyScore? ToScore(this string filePath)
        => Path.GetExtension(filePath).ToLowerInvariant() switch {
            ".mid" or ".midi" => filePath.MidiFileToScore(),
            ".musicxml"       => filePath.XmlToScore(),
            ".yuan" => filePath.YuanQinToScore(),
            _                 => null,
        };
}
