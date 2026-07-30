using System.Windows;
using SkyPiano.ViewModel;

namespace SkyPiano;

/// <summary>
/// 主窗口的代码隐藏，最小化设计。
/// 仅负责创建 ViewModel、设置 DataContext、窗口加载时自动读取默认 MIDI 文件夹，
/// 以及在窗口关闭时释放 ViewModel 资源。
/// 所有业务逻辑均在 <see cref="MainViewModel"/> 中处理。
/// </summary>
public partial class MainWindow : Window
{
    /// <summary>
    /// 主视图模型实例，作为整个窗口的 DataContext。
    /// </summary>
    private readonly MainViewModel _vm;

    /// <summary>
    /// 构造主窗口，初始化 XAML 组件、创建 ViewModel 并绑定生命周期事件。
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();

        // 创建 ViewModel 并设置为数据上下文，供 XAML 绑定
        _vm = new MainViewModel();
        DataContext = _vm;

        // 窗口加载完成时自动读取默认 MIDI 文件夹
        Loaded += OnLoaded;

        // 窗口关闭时释放资源（停止定时器、释放音频设备）
        Closing += (_, _) => _vm.Dispose();
    }

    /// <summary>
    /// 窗口加载完成后的回调，自动加载默认 MIDI 文件夹。
    /// 如果默认文件夹不存在则自动创建。
    /// </summary>
    /// <param name="sender">事件源（当前窗口）。</param>
    /// <param name="e">路由事件参数。</param>
    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _vm.LoadDefaultFolder();
    }
}
