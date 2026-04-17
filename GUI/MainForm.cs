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
        private string solutionPath = null!;

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
            // 訂閱 ListBox 的選取變更事件，控制移除按鈕的啟用狀態
            resultListBox.SelectedIndexChanged += resultListBox_SelectedIndexChanged;
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
            resultListBox.Items.Clear();
            // 防止重複點擊
            checkProjectButton.Enabled = false;

            // 在背景執行緒顯示「檢查中」對話框
            // 注意：ShowDialog 會封鎖 UI，因此必須在背景執行緒執行
#pragma warning disable CS4014
            Task.Run(() => (this.Invoke((MethodInvoker)delegate
            {
                dialog.ShowDialog();
            })));
#pragma warning restore CS4014

            // 執行參照檢查（此為非同步作業）
            var result = await ReferenceChecker.Check(solutionPath);

            // 關閉對話框
            this.Invoke((MethodInvoker)delegate
            {
                dialog.Close();
            });

            // 顯示檢查結果
            if (result != null)
            {
                MessageBox.Show($"檢查完成，計有 {result.Count} 個未參照方法。");

                foreach (var item in result)
                {
                    resultListBox.Items.Add(item);
                }
            }

            checkProjectButton.Enabled = true;
        }

        /// <summary>
        /// 處理「選擇 Solution」按鈕的點擊事件。
        /// 彈出檔案選擇對話框，讓使用者選擇 .sln / .slnx 檔案。
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
                // 設定檔案類型篩選器：只顯示 .sln /.slnx檔案
                openFileDialog.Filter = "Solution Files (*.sln;*.slnx)|*.sln;*.slnx";
                openFileDialog.Title = "Select a Solution File";

                // 顯示對話框並取得使用者選擇的結果
                var result = openFileDialog.ShowDialog();

                // ===== 驗證使用者的選擇 =====
                if (result == DialogResult.OK)
                {
                    string ext = Path.GetExtension(openFileDialog.FileName).ToLower();
                    // 確認副檔名為 .sln / .slnx （不區分大小寫）
                    if (ext == ".sln" || ext == ".slnx")
                    {
                        // 儲存選取的檔案路徑
                        solutionPath = openFileDialog.FileName;
                        // 啟用「檢查專案」按鈕
                        checkProjectButton.Enabled = true;
                        // 在標籤上顯示選取的路徑
                        labelSolution.Text = solutionPath;
                    }
                }
                else
                {
                    // 若選擇無效，顯示警告訊息
                    MessageBox.Show("Please select a valid .sln/.slnx file.", "Invalid File", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    // 停用「檢查專案」按鈕
                    checkProjectButton.Enabled = false;
                }
            }
        }

        /// <summary>
        /// 處理 ListBox 選取項目變更的事件。
        /// 當有選取項目時啟用「移除方法」按鈕，無選取時停用。
        /// </summary>
        /// <param name="sender">事件來源物件。</param>
        /// <param name="e">事件參數。</param>
        private void resultListBox_SelectedIndexChanged(object? sender, EventArgs e)
        {
            // 只有在有選取項目時才啟用移除按鈕
            removeMethodButton.Enabled = resultListBox.SelectedItems.Count > 0;
        }

        /// <summary>
        /// 處理「移除方法」按鈕的點擊事件。
        /// 支援多選刪除，批次刪除所有選取的未參照方法。
        /// 若方法有實作介面也會一併刪除介面中的方法宣告。
        /// </summary>
        /// <param name="sender">事件來源物件（按鈕）。</param>
        /// <param name="e">事件參數。</param>
        private async void removeMethodButton_Click(object sender, EventArgs e)
        {
            // 取得所有選取的方法簽名
            var selectedItems = resultListBox.SelectedItems;
            if (selectedItems.Count == 0) return;

            // 收集選取的簽名清單
            var signatures = new List<string>();
            foreach (var item in selectedItems)
            {
                signatures.Add(item.ToString()!);
            }

            // 彈出確認對話框，顯示即將刪除的方法數量及清單
            string methodList = string.Join("\n", signatures);
            var confirmResult = MessageBox.Show(
                $"即將刪除 {signatures.Count} 個方法：\n{methodList}\n\n" +
                "若有實作介面，介面中的方法也會一併刪除。\n\n" +
                "確定要繼續嗎？",
                "確認刪除",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            // 使用者取消
            if (confirmResult != DialogResult.Yes) return;

            // 停用按鈕防止重複點擊
            removeMethodButton.Enabled = false;
            checkProjectButton.Enabled = false;

            // 顯示等待對話框
            dialog.SetMessage($"刪除 {signatures.Count} 個方法中，請稍候...");
#pragma warning disable CS4014
            Task.Run(() => (this.Invoke((MethodInvoker)delegate
            {
                dialog.ShowDialog();
            })));
#pragma warning restore CS4014

            // 執行批次刪除
            var (success, message) = await ReferenceChecker.RemoveMethodsAsync(solutionPath, signatures);

            // 關閉等待對話框
            this.Invoke((MethodInvoker)delegate
            {
                dialog.Close();
            });

            // 顯示結果
            if (success)
            {
                MessageBox.Show(message, "刪除成功", MessageBoxButtons.OK, MessageBoxIcon.Information);

                // 重新執行檢查以更新清單
                resultListBox.Items.Clear();
                var checkResult = await ReferenceChecker.Check(solutionPath);
                if (checkResult != null)
                {
                    foreach (var item in checkResult)
                    {
                        resultListBox.Items.Add(item);
                    }
                    MessageBox.Show($"重新檢查完成，計有 {checkResult.Count} 個未參照方法。");
                }
            }
            else
            {
                MessageBox.Show(message, "刪除失敗", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            checkProjectButton.Enabled = true;
        }
    }
}
