using System.Windows;
using System.Windows.Data;
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

    // BindingExpression

    public static int GetMyProperty(DependencyObject obj)
    {
        return (int)obj.GetValue(MyPropertyProperty);
    }

    public static void SetMyProperty(DependencyObject obj, int value)
    {
        obj.SetValue(MyPropertyProperty, value);
    }

    // Using a DependencyProperty as the backing store for MyProperty.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty MyPropertyProperty =
        DependencyProperty.RegisterAttached("MyProperty", typeof(int), typeof(MainWindow), new PropertyMetadata(0));


    public MainWindow() {
        InitializeComponent();

        _vm = new MainViewModel();
        DataContext = _vm;

        // 窗口加载完成时自动加载默认 MIDI 文件夹
        Loaded += OnLoaded;

        // 窗口关闭时释放资源
        Closing += (_, _) => _vm.Dispose();
        //Binding binding = new Binding() { 
        //    Source = ""
        //};
        //BindingOperations.SetBinding

    }

    private void OnLoaded(object sender, RoutedEventArgs e) {
        _vm.LoadDefaultFolder();
    }
}
