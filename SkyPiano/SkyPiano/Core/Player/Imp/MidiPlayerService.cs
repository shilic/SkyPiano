using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using Melanchall.DryWetMidi.Multimedia;

namespace SkyPiano.Core.Player.Imp;

public class MidiPlayerService : IDisposable
{
    private Playback? _playback;
    private OutputDevice? _outputDevice;
    private SynchronizationContext? _syncContext;

    public bool IsPlaying => _playback?.IsRunning == true;
    public TimeSpan CurrentTime => _playback?.GetCurrentTime<MetricTimeSpan>() is { } mts
        ? TimeSpan.FromMicroseconds(mts.TotalMicroseconds)
        : TimeSpan.Zero;
    public TimeSpan Duration { get; private set; }

    public event Action<int, byte>? NotePlayed;
    public event Action<int>? NoteStopped;
    public event Action? Finished;

    public void SetSyncContext(SynchronizationContext ctx) => _syncContext = ctx;

    public MidiPlayerService()
    {
        var devices = OutputDevice.GetAll();
        _outputDevice = devices.FirstOrDefault();
    }

    public void LoadFile(string filePath)
    {
        Stop();
        _playback?.Dispose();

        var midiFile = MidiFile.Read(filePath);
        var tempoMap = midiFile.GetTempoMap();

        _playback = midiFile.GetPlayback(_outputDevice);
        _playback.NotesPlaybackStarted += OnNotesStarted;
        _playback.NotesPlaybackFinished += OnNotesFinished;
        _playback.Finished += OnFinished;

        var duration = midiFile.GetDuration<MetricTimeSpan>();
        Duration = TimeSpan.FromMicroseconds(duration.TotalMicroseconds);
    }

    public void Play()
    {
        _playback?.Start();
    }

    public void Pause()
    {
        _playback?.Stop();
    }

    public void TogglePlayPause()
    {
        if (IsPlaying) Pause();
        else Play();
    }

    public void SeekForward(TimeSpan delta)
    {
        if (_playback == null) return;
        var current = _playback.GetCurrentTime<MetricTimeSpan>();
        var newTime = new MetricTimeSpan(current.TotalMicroseconds + (long)(delta.TotalMicroseconds * 1000));
        if (newTime.TotalMicroseconds < 0) newTime = new MetricTimeSpan(0);
        _playback.MoveToTime(newTime);
    }

    public void SeekBackward(TimeSpan delta)
    {
        SeekForward(-delta);
    }

    public void Stop()
    {
        _playback?.Stop();
    }

    private void OnNotesStarted(object? sender, NotesEventArgs e)
    {
        if (_syncContext == null) return;
        _syncContext.Post(_ =>
        {
            foreach (var note in e.Notes)
                NotePlayed?.Invoke(note.NoteNumber, note.Velocity);
        }, null);
    }

    private void OnNotesFinished(object? sender, NotesEventArgs e)
    {
        if (_syncContext == null) return;
        _syncContext.Post(_ =>
        {
            foreach (var note in e.Notes)
                NoteStopped?.Invoke(note.NoteNumber);
        }, null);
    }

    private void OnFinished(object? sender, EventArgs e)
    {
        if (_syncContext == null) return;
        _syncContext.Post(_ => Finished?.Invoke(), null);
    }

    public void Dispose()
    {
        _playback?.Dispose();
        _outputDevice?.Dispose();
    }
}
