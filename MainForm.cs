using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ZeroReferences
{
    public partial class MainForm : Form
    {

        private string solutionPath;
        private ModalDialog dialog = new ModalDialog("檢查中，請稍候...");
        public MainForm()
        {
            InitializeComponent();
        }

        private async void checkProjectButton_Click(object sender, EventArgs e)
        {
            resultListBox.Items.Clear();
            //Task.Run(() => (dialog.ShowDialog()));
            Task.Run(() => (this.Invoke((MethodInvoker)delegate {
                dialog.ShowDialog();
            })));
            var result = await ReferenceChecker.Check(solutionPath);
            Task.Run(() => this.Invoke((MethodInvoker)delegate {
                dialog.Hide();
            }));
            if (result != null)
                {
                    //dialog.Hide();
                    //dialog.Close();
                    MessageBox.Show($"檢查完成，計有 {result.Count} 個未參照方法。");
                    foreach (var item in result)
                    {
                        resultListBox.Items.Add(item);
                    }
                }

        }

        private void selectSolutionButton_Click(object sender, EventArgs e)
        {
            resultListBox.Items.Clear();
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
                    labelSolution.Text = solutionPath;
                }
                else
                {
                    MessageBox.Show("Please select a valid .sln file.", "Invalid File", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    checkProjectButton.Enabled = false;
                }
            }
        }


    }
}
