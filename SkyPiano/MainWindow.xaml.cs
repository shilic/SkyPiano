using System.Windows;
using SkyPiano.ViewModel;

namespace SkyPiano;

/// <summary>
/// 主窗口。DataContext 由 XAML 创建，代码隐藏仅处理关闭时释放资源。
/// </summary>
public partial class MainWindow : Window {
    public MainWindow() {
        InitializeComponent();
        Closing += (_, _) => ((MainViewModel)DataContext).Dispose();
    }
}
