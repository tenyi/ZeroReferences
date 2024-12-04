using System.Windows.Forms;

namespace ZeroReferences
{
    public partial class ModalDialog : Form
    {
        public ModalDialog()
        {
            InitializeComponent();
        }

        public ModalDialog(string text)
        {
            InitializeComponent();
            labelMessage.Text = text;
        }

        // 用於開啟對話框的方法
        public new DialogResult ShowDialog()
        {
            return base.ShowDialog();
        }

        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ModalDialog));
            labelMessage = new Label();
            SuspendLayout();
            // 
            // labelMessage
            // 
            labelMessage.AutoSize = true;
            labelMessage.Font = new Font("Microsoft JhengHei UI", 12F, FontStyle.Regular, GraphicsUnit.Point, 136);
            labelMessage.Location = new Point(99, 78);
            labelMessage.Name = "labelMessage";
            labelMessage.Size = new Size(54, 20);
            labelMessage.TabIndex = 0;
            labelMessage.Text = "label1";
            labelMessage.UseWaitCursor = true;
            // 
            // ModalDialog
            // 
            ClientSize = new Size(363, 200);
            ControlBox = false;
            Controls.Add(labelMessage);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            Icon = (Icon)resources.GetObject("$this.Icon");
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "ModalDialog";
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterScreen;
            UseWaitCursor = true;
            ResumeLayout(false);
            PerformLayout();
        }

        // 用於關閉對話框的方法
        public new void Close()
        {
            base.Close();
        }

        private Label labelMessage;
    }
}
