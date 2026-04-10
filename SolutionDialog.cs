using System;
using System.Windows.Forms;
using System.Drawing;

namespace ZeroReferences
{
    /// <summary>
    /// 解決方案選擇對話框類別，提供選擇 .sln 檔案及初步檢查的功能。
    /// 此類別是早期版本的 UI，現已被 MainForm取代，但保留作為備用或擴展使用。
    /// </summary>
    public class SolutionDialog : Form
    {
        // ===== 私有成員欄位 =====

        /// <summary>
        /// 選擇解決方案檔案的按鈕。
        /// </summary>
        private Button selectSolutionButton = null!;

        /// <summary>
        /// 執行專案檢查的按鈕。
        /// </summary>
        private Button checkProjectButton = null!;

        /// <summary>
        /// 目前選取的解決方案檔案路徑。
        /// </summary>
        private string solutionPath = null!;

        // ===== 建構函式 =====

        /// <summary>
        /// 建構函式。初始化 UI 組件並設定視窗屬性。
        /// </summary>
        public SolutionDialog()
        {
            // ===== 建立「選擇 Solution」按鈕 =====
            selectSolutionButton = new Button()
            {
                Text = "Select Solution",  // 按鈕文字
                Left = 30,                   // 左邊界（X 座標）
                Height = 40,                 // 高度
                Width = 120,                 // 寬度
                Top = 20                     // 上邊界（Y 座標）
            };

            // ===== 建立「檢查專案」按鈕 =====
            checkProjectButton = new Button()
            {
                Text = "Check Project",      // 按鈕文字
                Left = 170,                  // 左邊界（位於第一個按鈕右方）
                Height = 40,                 // 高度
                Width = 120,                 // 寬度
                Top = 20,                    // 上邊界
                Enabled = false              // 初始時停用（需先選擇檔案）
            };

            // ===== 訂閱按鈕點擊事件 =====
            selectSolutionButton.Click += SelectSolutionButton_Click;
            checkProjectButton.Click += CheckProjectButton_Click;

            // ===== 將按鈕加入表單 =====
            Controls.Add(selectSolutionButton);
            Controls.Add(checkProjectButton);

            // ===== 設定視窗屬性 =====
            this.Icon = new System.Drawing.Icon("Reference.ico");  // 設定視窗圖示
            Text = "Solution Checker";                              // 設定視窗標題
            StartPosition = FormStartPosition.CenterScreen;         // 設定啟動位置：螢幕中央
            ClientSize = new System.Drawing.Size(640, 480);         // 設定客戶端大小
        }

        // ===== 事件處理常式 =====

        /// <summary>
        /// 處理「選擇 Solution」按鈕的點擊事件。
        /// 彈出檔案選擇對話框，讓使用者選擇 .sln 檔案。
        /// </summary>
        /// <param name="sender">事件來源物件（按鈕）。</param>
        /// <param name="e">事件參數。</param>
        private void SelectSolutionButton_Click(object? sender, EventArgs e)
        {
            // 建立開啟檔案對話框
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                // 設定檔案類型篩選器：只顯示 .sln 檔案
                openFileDialog.Filter = "Solution Files (*.sln)|*.sln";
                openFileDialog.Title = "Select a Solution File";

                // 顯示對話框並取得使用者選擇的結果
                var result = openFileDialog.ShowDialog();

                // ===== 驗證使用者的選擇 =====
                if (result == DialogResult.OK &&
                    // 確認副檔名為 .sln（不區分大小寫）
                    System.IO.Path.GetExtension(openFileDialog.FileName).Equals(".sln", StringComparison.OrdinalIgnoreCase))
                {
                    // 儲存選取的檔案路徑
                    solutionPath = openFileDialog.FileName;
                    // 啟用「檢查專案」按鈕
                    checkProjectButton.Enabled = true;
                }
                else
                {
                    // 若選擇無效，顯示警告訊息
                    MessageBox.Show("Please select a valid .sln file.", "Invalid File", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    // 停用「檢查專案」按鈕
                    checkProjectButton.Enabled = false;
                }
            }
        }

        /// <summary>
        /// 處理「檢查專案」按鈕的點擊事件。
        /// 調用 ReferenceChecker 進行分析。
        /// </summary>
        /// <param name="sender">事件來源物件（按鈕）。</param>
        /// <param name="e">事件參數。</param>
        private void CheckProjectButton_Click(object? sender, EventArgs e)
        {
            // 呼叫 ReferenceChecker 執行檢查（fire-and-forget）
#pragma warning disable CS4014
            ReferenceChecker.Check(solutionPath!);
#pragma warning restore CS4014
        }

        // ===== 私有方法 =====

        /// <summary>
        /// 初始化 UI 組件。
        /// 此方法由 IDE 的表單設計工具自動產生。
        /// </summary>
        private void InitializeComponent()
        {
            // 建立資源管理器，用於取得圖示等資源
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SolutionDialog));

            // 暫停版面配置
            SuspendLayout();

            // 設定對話框屬性
            ClientSize = new Size(866, 538);
            Icon = (Icon?)resources.GetObject("$this.Icon")!;
            Name = "SolutionDialog";
            StartPosition = FormStartPosition.CenterScreen;

            // 恢復版面配置
            ResumeLayout(false);
        }
    }
}
