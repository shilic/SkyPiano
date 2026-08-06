using System.Windows;
using System.Windows.Input;
using SkyPiano.Common;
using SkyPiano.ViewModel;

namespace SkyPiano;

public partial class MainWindow : Window {
    public MainWindow() {
        InitializeComponent();
        Loaded += OnLoaded;
        Closing += (_, _) => ((MainViewModel)DataContext).Dispose();
    }

    private void OnLoaded(object sender, RoutedEventArgs e) {
        var vm = (MainViewModel)DataContext;

        // 切换为英文输入法，避免 keybd_event 被中文输入法拦截
        InputLanguageManager.Current.CurrentInputLanguage =
            System.Globalization.CultureInfo.GetCultureInfo("en-US");

        GlobalHotkey.Instance.Initialize(this);
        // 注册全局热键 Ctrl + Alt + Space → 播放 / 暂停
        GlobalHotkey.Instance.Register(
            id: 1, 
            modifiers: ModifierKeys.Control | ModifierKeys.Alt,
            key: Key.Space,
            callback: () => {
                if (vm.PlayPauseCommand.CanExecute(null)) vm.PlayPauseCommand.Execute(null);
            }
        );
    }
}
