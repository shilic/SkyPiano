using SkyPiano.Core.Player.Base;

namespace SkyPiano.Core.Player.Imp;

/// <summary>
/// 键盘钢琴播放器，实现 <see cref="替身使者"/> 接口。
/// 内部委托给 <see cref="MidiPlayerService"/>（音频引擎）和 <see cref="PlaylistManager"/>（播放列表），
/// 负责将 JOJO 主题的控制命令翻译为实际的播放器操作。
/// </summary>
public class KeyPianoPlayer : 替身使者
{
    /// <summary>MIDI 音频引擎。</summary>
    private readonly MidiPlayerService _player;

    /// <summary>播放列表管理器。</summary>
    private readonly PlaylistManager _playlist;

    /// <summary>
    /// 构造 KeyPianoPlayer，注入已有的音频引擎和播放列表管理器。
    /// </summary>
    /// <param name="player">已配置好输出设备的 <see cref="MidiPlayerService"/> 实例。</param>
    /// <param name="playlist">已加载或待加载的 <see cref="PlaylistManager"/> 实例。</param>
    public KeyPianoPlayer(MidiPlayerService player, PlaylistManager playlist)
    {
        _player = player;
        _playlist = playlist;
    }

    /// <summary>
    /// 暂停 / 恢复播放。
    /// </summary>
    public void 咋瓦鲁多() => _player.TogglePlayPause();

    /// <summary>
    /// 切换到上一首曲目。
    /// </summary>
    public void 男人领域() => _playlist.MovePrevious();

    /// <summary>
    /// 快退 5 秒。
    /// </summary>
    public void 败者食尘() => _player.SeekBackward(TimeSpan.FromSeconds(5));

    /// <summary>
    /// 快进 5 秒。
    /// </summary>
    public void 天堂制造() => _player.SeekForward(TimeSpan.FromSeconds(5));

    /// <summary>
    /// 切换到下一首曲目。
    /// </summary>
    public void 墓志铭() => _playlist.MoveNext();

    /// <summary>
    /// 切换播放列表，加载指定文件夹中的所有 .mid 文件。
    /// </summary>
    /// <param name="name">包含 MIDI 文件的文件夹路径。</param>
    public void 恶行易施(string name) => _playlist.LoadFromFolder(name);
}
