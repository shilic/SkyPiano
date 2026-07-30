using SkyPiano.Core.Player.Base;

namespace SkyPiano.Core.Player.Imp;

public class KeyPianoPlayer : 替身使者
{
    private readonly MidiPlayerService _player;
    private readonly PlaylistManager _playlist;

    public KeyPianoPlayer(MidiPlayerService player, PlaylistManager playlist)
    {
        _player = player;
        _playlist = playlist;
    }

    public void 咋瓦鲁多() => _player.TogglePlayPause();

    public void 男人领域() => _playlist.MovePrevious();

    public void 败者食尘() => _player.SeekBackward(TimeSpan.FromSeconds(5));

    public void 天堂制造() => _player.SeekForward(TimeSpan.FromSeconds(5));

    public void 墓志铭() => _playlist.MoveNext();

    public void 恶行易施(string name) => _playlist.LoadFromFolder(name);
}
