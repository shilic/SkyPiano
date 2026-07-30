using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using System.Windows.Threading;
using SkyPiano.Core.Player.Imp;

namespace SkyPiano.ViewModel;

public class MainViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly MidiPlayerService _player;
    private readonly PlaylistManager _playlist;
    private readonly DispatcherTimer _timer;
    private readonly SynchronizationContext _sync = SynchronizationContext.Current!;
    private readonly Dictionary<int, KeyNoteViewModel> _keyLookup = new();

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<KeyNoteViewModel> KeyViewModels { get; } = new();

    public MainViewModel()
    {
        _player = new MidiPlayerService();
        _player.SetSyncContext(_sync);

        _playlist = new PlaylistManager();
        _playlist.TrackChanged += OnTrackChanged;
        _player.Finished += OnTrackFinished;

        _player.NotePlayed += OnNotePlayed;
        _player.NoteStopped += OnNoteStopped;

        InitKeys();

        _timer = new DispatcherTimer(TimeSpan.FromMilliseconds(100), DispatcherPriority.Normal,
            (_, _) =>
            {
                OnPropertyChanged(nameof(Progress));
                OnPropertyChanged(nameof(CurrentTimeDisplay));
            }, Dispatcher.CurrentDispatcher);
        _timer.Start();
    }

    // ---- 21-key init ----
    private void InitKeys()
    {
        var keys = new (string label, int midi)[]
        {
            ("Z",48),("X",50),("C",52),("V",53),("B",55),("N",57),("M",59),
            ("A",60),("S",62),("D",64),("F",65),("G",67),("H",69),("J",71),
            ("Q",72),("W",74),("E",76),("R",77),("T",79),("Y",81),("U",83),
        };
        foreach (var (label, midi) in keys)
        {
            var vm = new KeyNoteViewModel(label, midi);
            KeyViewModels.Add(vm);
            _keyLookup[midi] = vm;
        }
    }

    // ---- properties ----
    public double Progress => _player.Duration.TotalSeconds > 0
        ? _player.CurrentTime.TotalSeconds / _player.Duration.TotalSeconds
        : 0;

    public string CurrentTimeDisplay => $"{(int)_player.CurrentTime.TotalMinutes:D2}:{_player.CurrentTime.Seconds:D2}";

    public string DurationDisplay => $"{(int)_player.Duration.TotalMinutes:D2}:{_player.Duration.Seconds:D2}";

    private string _currentTrackName = "";
    public string CurrentTrackName
    {
        get => _currentTrackName;
        set { _currentTrackName = value; OnPropertyChanged(nameof(CurrentTrackName)); }
    }

    private string _trackIndex = "";
    public string TrackIndex
    {
        get => _trackIndex;
        set { _trackIndex = value; OnPropertyChanged(nameof(TrackIndex)); }
    }

    private bool _isPlaying;
    public bool IsPlaying
    {
        get => _isPlaying;
        set { _isPlaying = value; OnPropertyChanged(nameof(IsPlaying)); }
    }

    // ---- commands ----
    private ICommand? _playPauseCommand;
    public ICommand PlayPauseCommand => _playPauseCommand ??= new RelayCommand(() =>
    {
        _player.TogglePlayPause();
        IsPlaying = _player.IsPlaying;
    });

    private ICommand? _nextCommand;
    public ICommand NextCommand => _nextCommand ??= new RelayCommand(() => _playlist.MoveNext());

    private ICommand? _prevCommand;
    public ICommand PrevCommand => _prevCommand ??= new RelayCommand(() => _playlist.MovePrevious());

    private ICommand? _rewindCommand;
    public ICommand RewindCommand => _rewindCommand ??= new RelayCommand(
        () => _player.SeekBackward(TimeSpan.FromSeconds(5)));

    private ICommand? _ffCommand;
    public ICommand FastForwardCommand => _ffCommand ??= new RelayCommand(
        () => _player.SeekForward(TimeSpan.FromSeconds(5)));

    // ---- track management ----
    private void OnTrackChanged(string? path)
    {
        if (path == null) return;
        _player.LoadFile(path);
        _player.Play();
        CurrentTrackName = System.IO.Path.GetFileNameWithoutExtension(path);
        TrackIndex = $"{_playlist.CurrentIndex + 1}/{_playlist.Count}";
        IsPlaying = true;
    }

    private void OnTrackFinished()
    {
        _playlist.MoveNext();
    }

    // ---- note events ----
    private void OnNotePlayed(int midi, byte velocity)
    {
        if (_keyLookup.TryGetValue(midi, out var vm))
            vm.IsActive = true;
    }

    private void OnNoteStopped(int midi)
    {
        if (_keyLookup.TryGetValue(midi, out var vm))
            vm.IsActive = false;
    }

    // ---- folder ----
    public void OpenFolder()
    {
        var dlg = new Microsoft.Win32.OpenFolderDialog
        {
            Title = "选择 MIDI 文件夹",
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
        };
        if (dlg.ShowDialog() == true)
            _playlist.LoadFromFolder(dlg.FolderName);
    }

    // ---- INotifyPropertyChanged ----
    private void OnPropertyChanged(string name) =>
        PropertyChanged?.Invoke(this, new(name));

    public void Dispose()
    {
        _timer.Stop();
        _player.Dispose();
    }
}

public class RelayCommand : ICommand
{
    private readonly Action _execute;
    public RelayCommand(Action execute) => _execute = execute;
    public bool CanExecute(object? parameter) => true;
    public void Execute(object? parameter) => _execute();
#pragma warning disable CS0067
    public event EventHandler? CanExecuteChanged;
#pragma warning restore CS0067
}
