using System.ComponentModel;

namespace SkyPiano.ViewModel;

public class KeyNoteViewModel : INotifyPropertyChanged
{
    public string Label { get; }
    public int MidiNumber { get; }

    private bool _isActive;
    public bool IsActive
    {
        get => _isActive;
        set { _isActive = value; PropertyChanged?.Invoke(this, new(nameof(IsActive))); }
    }

    public KeyNoteViewModel(string label, int midiNumber)
    {
        Label = label;
        MidiNumber = midiNumber;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}
