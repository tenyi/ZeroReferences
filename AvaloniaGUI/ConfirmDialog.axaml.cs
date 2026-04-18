using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ZeroReferences.AvaloniaGUI;

/// <summary>
/// 確認對話框。顯示訊息並提供「是/否」按鈕供使用者確認操作。
/// 透過 ShowDialog&lt;bool&gt; 回傳使用者的選擇結果。
/// </summary>
public partial class ConfirmDialog : Window
{
    /// <summary>
    /// 無參數建構函式，供 Avalonia XAML 載入器使用。
    /// </summary>
    public ConfirmDialog()
    {
        InitializeComponent();
    }

    /// <summary>
    /// 帶訊息的建構函式，設定對話框標題與內容文字。
    /// </summary>
    /// <param name="title">對話框標題。</param>
    /// <param name="message">顯示的訊息內容。</param>
    public ConfirmDialog(string title, string message) : this()
    {
        Title = title;
        MessageText.Text = message;
    }

    /// <summary>
    /// 按下「是」按鈕，回傳 true 並關閉對話框。
    /// </summary>
    private void OnYesClick(object? sender, RoutedEventArgs e)
    {
        Close(true);
    }

    /// <summary>
    /// 按下「否」按鈕，回傳 false 並關閉對話框。
    /// </summary>
    private void OnNoClick(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }
}
