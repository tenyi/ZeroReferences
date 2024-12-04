namespace ZeroReferences
{
    partial class MainForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            selectSolutionButton = new Button();
            checkProjectButton = new Button();
            resultListBox = new ListBox();
            labelSolution = new Label();
            SuspendLayout();
            // 
            // selectSolutionButton
            // 
            selectSolutionButton.Font = new Font("Microsoft JhengHei UI", 12F);
            selectSolutionButton.Location = new Point(252, 61);
            selectSolutionButton.Name = "selectSolutionButton";
            selectSolutionButton.Size = new Size(107, 45);
            selectSolutionButton.TabIndex = 0;
            selectSolutionButton.Text = "選擇專案";
            selectSolutionButton.UseVisualStyleBackColor = true;
            selectSolutionButton.Click += selectSolutionButton_Click;
            // 
            // checkProjectButton
            // 
            checkProjectButton.Enabled = false;
            checkProjectButton.Font = new Font("Microsoft JhengHei UI", 12F);
            checkProjectButton.Location = new Point(765, 61);
            checkProjectButton.Name = "checkProjectButton";
            checkProjectButton.Size = new Size(103, 45);
            checkProjectButton.TabIndex = 1;
            checkProjectButton.Text = "檢查專案";
            checkProjectButton.UseVisualStyleBackColor = true;
            checkProjectButton.Click += checkProjectButton_Click;
            // 
            // resultListBox
            // 
            resultListBox.Font = new Font("Microsoft JhengHei UI", 11.25F, FontStyle.Regular, GraphicsUnit.Point, 136);
            resultListBox.FormattingEnabled = true;
            resultListBox.ItemHeight = 19;
            resultListBox.Location = new Point(56, 171);
            resultListBox.Name = "resultListBox";
            resultListBox.Size = new Size(1179, 403);
            resultListBox.TabIndex = 2;
            // 
            // labelSolution
            // 
            labelSolution.AutoSize = true;
            labelSolution.Font = new Font("Microsoft JhengHei UI", 12F, FontStyle.Regular, GraphicsUnit.Point, 136);
            labelSolution.Location = new Point(116, 119);
            labelSolution.Name = "labelSolution";
            labelSolution.Size = new Size(0, 20);
            labelSolution.TabIndex = 3;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1278, 608);
            Controls.Add(labelSolution);
            Controls.Add(resultListBox);
            Controls.Add(checkProjectButton);
            Controls.Add(selectSolutionButton);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Name = "MainForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "檢查 Reference 為零";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Button selectSolutionButton;
        private Button checkProjectButton;
        private ListBox resultListBox;
        private Label labelSolution;
    }
}