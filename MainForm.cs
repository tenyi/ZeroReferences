using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ZeroReferences
{
    /// <summary>
    /// 主視窗類別，提供選擇解決方案檔案及觸發參照檢查的功能。
    /// 此視窗是應用程式的核心 UI，負責與使用者互動並展示分析結果。
    /// </summary>
    public partial class MainForm : Form
    {
        // ===== 私有成員欄位 =====

        /// <summary>
        /// 目前選取的解決方案檔案路徑。
        /// </summary>
        private string solutionPath;

        /// <summary>
        /// 用於顯示「檢查中」提示的模態對話框執行個體。
        /// </summary>
        private ModalDialog dialog = new ModalDialog("檢查中，請稍候...");

        /// <summary>
        /// 構造函數。初始化 UI 組件，並設置預設的模態對話框。
        /// </summary>
        public MainForm()
        {
            InitializeComponent();
        }

        // ===== 事件處理常式 =====

        /// <summary>
        /// 處理「檢查專案」按鈕的點擊事件。
        /// 啟動非同步檢查流程，並在完成後將結果顯示在列表框中。
        /// </summary>
        /// <param name="sender">事件來源物件（按鈕）。</param>
        /// <param name="e">事件參數。</param>
        private async void checkProjectButton_Click(object sender, EventArgs e)
        {
            // ===== 清空之前的結果 =====
            resultListBox.Items.Clear();

            // ===== 在背景執行緒顯示「檢查中」對話框 =====
            // 注意：ShowDialog 會封鎖 UI，因此必須在背景執行緒執行
            Task.Run(() => (this.Invoke((MethodInvoker)delegate {
                dialog.ShowDialog();
            })));

            // ===== 執行參照檢查（此為非同步作業）=====
            var result = await ReferenceChecker.Check(solutionPath);

            // ===== 在背景執行緒隱藏「檢查中」對話框 =====
            Task.Run(() => this.Invoke((MethodInvoker)delegate {
                dialog.Hide();
            }));

            // ===== 顯示檢查結果 =====
            if (result != null)
            {
                // 彈出訊息方塊，顯示找到的未參照方法數量
                MessageBox.Show($"檢查完成，計有 {result.Count} 個未參照方法。");

                // 將每個未參照的方法名稱加入清單方塊顯示
                foreach (var item in result)
                {
                    resultListBox.Items.Add(item);
                }
            }
        }

        /// <summary>
        /// 處理「選擇 Solution」按鈕的點擊事件。
        /// 彈出檔案選擇對話框，讓使用者選擇 .sln 檔案。
        /// </summary>
        /// <param name="sender">事件來源物件（按鈕）。</param>
        /// <param name="e">事件參數。</param>
        private void selectSolutionButton_Click(object sender, EventArgs e)
        {
            // 清空之前的結果
            resultListBox.Items.Clear();

            // 建立並顯示開啟檔案對話框
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
                    // 在標籤上顯示選取的路徑
                    labelSolution.Text = solutionPath;
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
    }
}
