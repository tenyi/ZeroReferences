namespace ZeroReferences
{
    /// <summary>
    /// 應用程式的進入點類別，負責啟動應用程式並執行初始設定。
    /// </summary>
    internal static class Program
    {
        /// <summary>
        /// 應用程式的主進入點。
        /// 初始化 Windows Forms 應用程式環境並啟動主視窗。
        /// </summary>
        [STAThread]
        static void Main()
        {
            // 初始化應用程式配置（高 DPI 設定、預設字體等）
            ApplicationConfiguration.Initialize();
            // 啟用 Windows 視覺化樣式（XP/Vista 風格）
            Application.EnableVisualStyles();
            // 執行應用程式並指定主視窗為 MainForm
            Application.Run(new MainForm());
        }
    }
}
