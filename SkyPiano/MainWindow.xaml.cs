using System.Windows;
using SkyPiano.ViewModel;

namespace SkyPiano;

/// <summary>
/// 主窗口的代码隐藏，最小化设计。
/// 窗口加载时自动读取默认 MIDI 文件夹（Documents/SkyPiano/MIDI），
/// 不存在则自动创建。关闭时释放 ViewModel 资源。
/// </summary>
public partial class MainWindow : Window
{
    private readonly MainViewModel _vm;

    public MainWindow()
    {
        InitializeComponent();

        _vm = new MainViewModel();
        DataContext = _vm;

        // 窗口加载完成时自动加载默认 MIDI 文件夹
        Loaded += OnLoaded;

        // 窗口关闭时释放资源
        Closing += (_, _) => _vm.Dispose();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _vm.LoadDefaultFolder();
    }
}
