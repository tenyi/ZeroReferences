using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace ZeroReferences.AvaloniaGUI;

/// <summary>
/// 應用程式主類別。負責初始化 Avalonia 框架並建立主視窗。
/// </summary>
public class App : Application
{
    /// <summary>
    /// 初始化 XAML 資源與樣式。
    /// </summary>
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    /// <summary>
    /// 框架初始化完成後，建立並顯示主視窗。
    /// </summary>
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
