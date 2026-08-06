namespace SkyPiano.Core.Player.Base;

/// <summary>
/// 替身使者 — 播放控制接口，定义所有播放器必须实现的操控方法。<br/>
/// 命名灵感来源于 JOJO 的奇妙冒险中的替身能力：
/// <list type="number">
/// <item><b>咋瓦鲁多（The World）</b>：暂停 / 恢复播放。</item>
/// <item><b>男人领域（Mandom）</b>：切换到上一首曲目。</item>
/// <item><b>败者食尘（Bites the Dust）</b>：快退。</item>
/// <item><b>天堂制造（Made in Heaven）</b>：快进。</item>
/// <item><b>墓志铭（Epitaph）</b>：切换到下一首曲目。</item>
/// <item><b>恶行易施（Dirty Deeds Done Dirt Cheap）</b>：切换播放列表（加载指定文件夹）。</item>
/// </list>
/// </summary>
public interface 替身使者 {
    /// <summary>
    /// 暂停 / 恢复播放。当前正在播放时暂停，已暂停时恢复播放。
    /// </summary>
    void 咋瓦鲁多();

    /// <summary>
    /// 切换到上一首曲目。如果当前是第一首，则循环到最后一首。
    /// </summary>
    void 男人领域();

    /// <summary>
    /// 快退指定时间（默认 5 秒）。
    /// </summary>
    void 败者食尘();

    /// <summary>
    /// 快进指定时间（默认 5 秒）。
    /// </summary>
    void 天堂制造();

    /// <summary>
    /// 切换到下一首曲目。如果当前是最后一首，则循环到第一首。
    /// </summary>
    void 墓志铭();

    /// <summary>
    /// 切换播放列表到指定文件夹，扫描其中所有 .mid 文件。
    /// </summary>
    /// <param name="name">包含 MIDI 文件的文件夹路径。</param>
    void 恶行易施(string name);
    /// <summary>
    /// 切换播放列表到指定索引的曲目。
    /// </summary>
    /// <param name="index"></param>
    void 恶行易施(int index);
    /// <summary>跳转到指定百分比位置，0.0 为开头，1.0 为结尾。</summary>
    /// <param name="percent">目标位置百分比（0.0~1.0）。</param>
    void 时间删除(double percent);
}
