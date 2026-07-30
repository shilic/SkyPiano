using System.Windows;
using SkyPiano.ViewModel;

namespace SkyPiano;

public partial class MainWindow : Window
{
    private readonly MainViewModel _vm;

    public MainWindow()
    {
        InitializeComponent();
        _vm = new MainViewModel();
        DataContext = _vm;
        Loaded += OnLoaded;
        Closing += (_, _) => _vm.Dispose();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _vm.OpenFolder();
    }
}
