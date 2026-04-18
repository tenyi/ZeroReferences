using Avalonia;

namespace ZeroReferences.AvaloniaGUI;

/// <summary>
/// 應用程式進入點。初始化 Avalonia 框架並啟動桌面生命週期。
/// </summary>
internal class Program
{
    /// <summary>
    /// 應用程式主進入點。
    /// 使用 Avalonia 的 ClassicDesktopLifetime 啟動傳統桌面應用程式。
    /// </summary>
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);

    /// <summary>
    /// 建構 Avalonia 應用程式實例。
    /// </summary>
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
