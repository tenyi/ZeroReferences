using System;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Resources;

namespace ZeroReferences
{
    public class SolutionDialog : Form
    {
        private Button selectSolutionButton;
        private Button checkProjectButton;
        private string solutionPath;

        public SolutionDialog()
        {
            selectSolutionButton = new Button()
            {
                Text = "Select Solution",
                Left = 30,
                Height = 40,
                Width = 120,
                Top = 20
            };
            checkProjectButton = new Button()
            {
                Text = "Check Project",
                Left = 170,
                Height = 40,
                Width = 120,
                Top = 20,
                Enabled = false
            };

            selectSolutionButton.Click += SelectSolutionButton_Click;
            checkProjectButton.Click += CheckProjectButton_Click;

            Controls.Add(selectSolutionButton);
            Controls.Add(checkProjectButton);
            this.Icon = new System.Drawing.Icon("Reference.ico");
            
            Text = "Solution Checker";
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new System.Drawing.Size(640, 480);
            
        }

        private void SelectSolutionButton_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "Solution Files (*.sln)|*.sln";
                openFileDialog.Title = "Select a Solution File";

                var result = openFileDialog.ShowDialog();
                if (result == DialogResult.OK &&
                    System.IO.Path.GetExtension(openFileDialog.FileName).Equals(".sln", StringComparison.OrdinalIgnoreCase))
                {
                    solutionPath = openFileDialog.FileName;
                    checkProjectButton.Enabled = true;
                }
                else
                {
                    MessageBox.Show("Please select a valid .sln file.", "Invalid File", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    checkProjectButton.Enabled = false;
                }
            }
        }

        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SolutionDialog));
            SuspendLayout();
            // 
            // SolutionDialog
            // 
            ClientSize = new Size(866, 538);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Name = "SolutionDialog";
            StartPosition = FormStartPosition.CenterScreen;
            ResumeLayout(false);
        }

        private void CheckProjectButton_Click(object sender, EventArgs e)
        {
            // Add code here to check the project using the selected solutionPath
            // MessageBox.Show($"Selected solution: {solutionPath}", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
            ReferenceChecker.Check(solutionPath);
        }

    }
}

