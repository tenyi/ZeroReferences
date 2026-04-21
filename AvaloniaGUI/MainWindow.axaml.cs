using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;

namespace ZeroReferences.AvaloniaGUI;

/// <summary>
/// 主視窗類別。提供選擇解決方案檔案、觸發參照檢查、顯示結果及移除孤兒方法的功能。
/// 這是 Avalonia 版本的核心 UI，對應 WinForms 版的 MainForm。
/// </summary>
public partial class MainWindow : Window
{
    // ===== 存取層級篩選常數 =====

    /// <summary>篩選器：顯示全部方法。</summary>
    private const string FilterAll = "全部";

    /// <summary>篩選器：只顯示 public 方法。</summary>
    private const string FilterPublicOnly = "只看 public";

    /// <summary>篩選器：只顯示 private 方法。</summary>
    private const string FilterPrivateOnly = "只看 private";

    /// <summary>篩選器：只顯示 protected 方法。</summary>
    private const string FilterProtectedOnly = "只看 protected";

    // ===== 私有成員欄位 =====

    /// <summary>目前選取的解決方案檔案路徑。</summary>
    private string _solutionPath = string.Empty;

    /// <summary>保存最新一次檢查得到的完整方法清單，供 UI 篩選使用。</summary>
    private readonly List<string> _allMethodResults = new();

    /// <summary>
    /// 建構函式。初始化 XAML 元件。
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();
    }

    // ===== 事件處理常式 =====

    /// <summary>
    /// 處理「選擇專案」按鈕的點擊事件。
    /// 使用 Avalonia StorageProvider 開啟跨平台檔案選擇器。
    /// </summary>
    private async void SelectSolutionButton_Click(object? sender, RoutedEventArgs e)
    {
        // 取得頂層視窗的 StorageProvider
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        // 設定檔案選擇器選項：只允許 .sln 和 .slnx
        var options = new FilePickerOpenOptions
        {
            Title = "選擇 Solution 檔案",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Solution/Project Files")
                {
                    Patterns = new[] { "*.sln", "*.slnx", "*.csproj" }
                }
            }
        };

        // 顯示檔案選擇器
        var files = await topLevel.StorageProvider.OpenFilePickerAsync(options);

        if (files.Count > 0)
        {
            // 使用者選擇了檔案
            var filePath = files[0].TryGetLocalPath();
            if (filePath != null)
            {
                string ext = System.IO.Path.GetExtension(filePath).ToLower();
                if (ext == ".sln" || ext == ".slnx" || ext == ".csproj")
                {
                    // 清空之前的結果
                    ResultListBox.ItemsSource = null;
                    _allMethodResults.Clear();

                    _solutionPath = filePath;
                    SolutionPathText.Text = _solutionPath;
                    CheckProjectButton.IsEnabled = true;
                    return;
                }
            }
        }

        // 未選擇有效檔案
        CheckProjectButton.IsEnabled = false;
    }

    /// <summary>
    /// 處理「檢查專案」按鈕的點擊事件。
    /// 顯示等待覆蓋層，執行非同步參照檢查，完成後更新結果列表。
    /// </summary>
    private async void CheckProjectButton_Click(object? sender, RoutedEventArgs e)
    {
        // 清空之前的結果
        ResultListBox.ItemsSource = null;
        _allMethodResults.Clear();

        // 防止重複點擊
        CheckProjectButton.IsEnabled = false;

        // 顯示等待覆蓋層
        LoadingMessage.Text = "檢查中，請稍候...";
        LoadingOverlay.IsVisible = true;

        try
        {
            // 執行參照檢查（非同步作業）
            var result = await ReferenceChecker.Check(_solutionPath);

            if (result != null)
            {
                _allMethodResults.AddRange(result);
                ApplyAccessibilityFilter();

                // 顯示結果摘要
                await ShowInfoMessage("檢查完成", $"檢查完成，計有 {result.Count} 個未參照方法。");
            }
        }
        catch (System.Exception ex)
        {
            await ShowInfoMessage("錯誤", $"檢查過程發生錯誤：{ex.Message}");
        }
        finally
        {
            // 隱藏等待覆蓋層並恢復按鈕
            LoadingOverlay.IsVisible = false;
            CheckProjectButton.IsEnabled = true;
        }
    }

    /// <summary>
    /// 處理「移除方法」按鈕的點擊事件。
    /// 支援多選刪除，批次刪除所有選取的未參照方法。
    /// </summary>
    private async void RemoveMethodButton_Click(object? sender, RoutedEventArgs e)
    {
        // 取得所有選取的方法簽名
        var selectedItems = ResultListBox.SelectedItems?
            .Cast<string>()
            .ToList();

        if (selectedItems == null || selectedItems.Count == 0) return;

        // 顯示確認對話框
        string methodList = string.Join("\n", selectedItems);
        var confirmDialog = new ConfirmDialog(
            "確認刪除",
            $"即將刪除 {selectedItems.Count} 個方法：\n{methodList}\n\n" +
            "若有實作介面，介面中的方法也會一併刪除。\n\n" +
            "確定要繼續嗎？");

        bool confirmed = await confirmDialog.ShowDialog<bool>(this);
        if (!confirmed) return;

        // 停用按鈕防止重複點擊
        RemoveMethodButton.IsEnabled = false;
        CheckProjectButton.IsEnabled = false;

        // 顯示等待覆蓋層
        LoadingMessage.Text = $"刪除 {selectedItems.Count} 個方法中，請稍候...";
        LoadingOverlay.IsVisible = true;

        try
        {
            // 執行批次刪除
            var (success, message) = await ReferenceChecker.RemoveMethodsAsync(
                _solutionPath, selectedItems);

            if (success)
            {
                await ShowInfoMessage("刪除成功", message);

                // 重新執行檢查以更新清單
                ResultListBox.ItemsSource = null;
                _allMethodResults.Clear();

                var checkResult = await ReferenceChecker.Check(_solutionPath);
                if (checkResult != null)
                {
                    _allMethodResults.AddRange(checkResult);
                    ApplyAccessibilityFilter();
                    await ShowInfoMessage("重新檢查完成", $"計有 {checkResult.Count} 個未參照方法。");
                }
            }
            else
            {
                await ShowInfoMessage("刪除失敗", message);
            }
        }
        catch (System.Exception ex)
        {
            await ShowInfoMessage("錯誤", $"刪除過程發生錯誤：{ex.Message}");
        }
        finally
        {
            LoadingOverlay.IsVisible = false;
            CheckProjectButton.IsEnabled = true;
            RemoveMethodButton.IsEnabled = ResultListBox.SelectedItems != null &&
                                           ResultListBox.SelectedItems.Count > 0;
        }
    }

    /// <summary>
    /// 處理篩選下拉選單切換事件，重新套用目前的結果篩選。
    /// </summary>
    private void AccessibilityFilterComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        ApplyAccessibilityFilter();
    }

    /// <summary>
    /// 處理 ListBox 選取項目變更的事件。
    /// 當有選取項目時啟用「移除方法」按鈕，無選取時停用。
    /// </summary>
    private void ResultListBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        RemoveMethodButton.IsEnabled = ResultListBox.SelectedItems != null &&
                                       ResultListBox.SelectedItems.Count > 0;
    }

    // ===== 私有輔助方法 =====

    /// <summary>
    /// 根據下拉選單選項，將完整結果清單過濾後顯示到 ListBox。
    /// </summary>
    private void ApplyAccessibilityFilter()
    {
        // XAML 初始化期間控制項可能尚未建立，直接返回
        if (AccessibilityFilterComboBox == null || ResultListBox == null)
            return;

        var comboBox = AccessibilityFilterComboBox;
        var selectedItem = (comboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? FilterAll;

        // 根據篩選條件過濾方法清單
        var filtered = _allMethodResults.Where(method =>
        {
            if (selectedItem == FilterPublicOnly)
                return method.StartsWith("public ", System.StringComparison.OrdinalIgnoreCase);
            if (selectedItem == FilterPrivateOnly)
                return method.StartsWith("private ", System.StringComparison.OrdinalIgnoreCase);
            if (selectedItem == FilterProtectedOnly)
                return method.StartsWith("protected ", System.StringComparison.OrdinalIgnoreCase);
            return true;
        }).ToList();

        ResultListBox.ItemsSource = filtered;
        RemoveMethodButton.IsEnabled = false;
    }

    /// <summary>
    /// 顯示簡易資訊對話框（僅含「確定」按鈕）。
    /// </summary>
    private async Task ShowInfoMessage(string title, string message)
    {
        var dialog = new ConfirmDialog(title, message);

        // 隱藏「否」按鈕，只顯示「是」（改為「確定」）
        dialog.NoButton.IsVisible = false;
        dialog.YesButton.Content = "確定";

        await dialog.ShowDialog<bool>(this);
    }
}
