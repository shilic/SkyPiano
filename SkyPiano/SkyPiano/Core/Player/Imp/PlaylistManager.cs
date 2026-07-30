using System.IO;

namespace SkyPiano.Core.Player.Imp;

public class PlaylistManager
{
    private string[] _tracks = [];
    private int _currentIndex = -1;

    public int Count => _tracks.Length;
    public int CurrentIndex => _currentIndex;
    public string? CurrentTrack => _currentIndex >= 0 && _currentIndex < _tracks.Length ? _tracks[_currentIndex] : null;

    public event Action<string?>? TrackChanged;

    public void LoadFromFolder(string folderPath)
    {
        _tracks = Directory.GetFiles(folderPath, "*.mid", SearchOption.TopDirectoryOnly)
            .OrderBy(f => f)
            .ToArray();
        _currentIndex = _tracks.Length > 0 ? 0 : -1;
        TrackChanged?.Invoke(CurrentTrack);
    }

    public void MoveNext()
    {
        if (_tracks.Length == 0) return;
        _currentIndex = (_currentIndex + 1) % _tracks.Length;
        TrackChanged?.Invoke(CurrentTrack);
    }

    public void MovePrevious()
    {
        if (_tracks.Length == 0) return;
        _currentIndex = (_currentIndex - 1 + _tracks.Length) % _tracks.Length;
        TrackChanged?.Invoke(CurrentTrack);
    }
}
