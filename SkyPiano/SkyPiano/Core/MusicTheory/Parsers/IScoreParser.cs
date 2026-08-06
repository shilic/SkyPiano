namespace SkyPiano.Core.MusicTheory.Parsers;

/// <summary>
/// 乐谱解析器接口。每个实现类负责一种文件格式。
/// </summary>
public interface IScoreParser {
    /// <summary>支持的文件后缀（含点号），如 ".mid"。</summary>
    string Extension { get; }
    /// <summary>解析文件，返回 MyScore。不支持的格式返回 null。</summary>
    MyScore? Parse(string filePath);
}
