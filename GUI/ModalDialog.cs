using System.Windows.Forms;
using System.Drawing;

namespace ZeroReferences
{
    /// <summary>
    /// 模態對話框類別，用於在執行長時間操作時顯示提示訊息給使用者。
    /// 此對話框會在使用者等待時顯示，避免使用者誤以為程式當機。
    /// </summary>
    public partial class ModalDialog : Form
    {
        // ===== 私有成員欄位 =====

        /// <summary>
        /// 訊息標籤，用於顯示提示文字。
        /// </summary>
        private Label labelMessage = null!;

        // ===== 建構函式 =====

        /// <summary>
        /// 無參數建構函式。使用預設值初始化對話框。
        /// </summary>
        public ModalDialog()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 帶文字參數的建構函式。
        /// </summary>
        /// <param name="text">顯示在對話框中的預設訊息。</param>
        public ModalDialog(string text)
        {
            InitializeComponent();
            // 設定訊息標籤的文字內容
            labelMessage!.Text = text;
        }

        // ===== 公開方法 =====

        /// <summary>
        /// 更新對話框中顯示的訊息文字。
        /// </summary>
        /// <param name="message">要顯示的新訊息。</param>
        public void SetMessage(string message)
        {
            labelMessage!.Text = message;
        }

        // ===== 私有方法 =====

        /// <summary>
        /// 初始化 UI 組件。
        /// 此方法負責建立對話框的所有控制項並設定其屬性。
        /// </summary>
        private void InitializeComponent()
        {
            // 建立資源管理器，用於取得圖示等資源
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ModalDialog));

            // 建立訊息標籤
            labelMessage = new Label();

            // 暫停版面配置以提升效能
            SuspendLayout();

            // ===== 設定 labelMessage 標籤的屬性 =====

            // 自動調整大小以符合內容
            labelMessage.AutoSize = true;
            // 設定字體：微軟正黑體、12pt
            labelMessage.Font = new Font("Microsoft JhengHei UI", 12F, FontStyle.Regular, GraphicsUnit.Point, 136);
            // 設定位置：置中偏上
            labelMessage.Location = new Point(99, 78);
            // 設定控制項名稱（用於程式碼參照）
            labelMessage.Name = "labelMessage";
            // 設定標籤大小（初始值，會根據文字調整）
            labelMessage.Size = new Size(54, 20);
            // 設定 Tab 順序
            labelMessage.TabIndex = 0;
            // 設定預設文字
            labelMessage.Text = "label1";
            // 使用等待游標（沙漏圖示）
            labelMessage.UseWaitCursor = true;

            // ===== 設定 ModalDialog 對話框本身的屬性 =====

            // 設定客戶端大小（對話框內部區域大小）
            ClientSize = new Size(363, 200);
            // 隱藏關閉按鈕（避免使用者提前關閉）
            ControlBox = false;
            // 將訊息標籤加入對話框
            Controls.Add(labelMessage);
            // 設定表單邊框樣式：固定對話框（不可調整大小）
            FormBorderStyle = FormBorderStyle.FixedDialog;
            // 設定視窗圖示
            Icon = (Icon?)resources.GetObject("$this.Icon")!;
            // 隱藏最大化按鈕
            MaximizeBox = false;
            // 隱藏最小化按鈕
            MinimizeBox = false;
            // 設定表單名稱
            Name = "ModalDialog";
            // 不在工作列顯示圖示
            ShowInTaskbar = false;
            // 設定啟動位置：螢幕正中央
            StartPosition = FormStartPosition.CenterScreen;
            // 使用等待游標
            UseWaitCursor = true;

            // 恢復版面配置並執行配置
            ResumeLayout(false);
            PerformLayout();
        }
    }
}
