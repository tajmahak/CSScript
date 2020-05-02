using CSScript.Properties;
using System;
using System.Windows.Forms;

namespace CSScript
{
    /// <summary>
    /// Форма для вывода логов программы
    /// </summary>
    internal partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
            Icon = Resources.favicon;
            richTextBox.BackColor = Program.Settings.BackColor;
            richTextBox.ForeColor = Program.Settings.ForeColor;

            Program.OutputLog.ItemAppened += OutputLog_ItemAppened;

            foreach (LogItem block in Program.OutputLog.Items)
            {
                Print(block);
            }
        }

        private void Form_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape || e.KeyCode == Keys.Enter)
            {
                if (Program.Finished)
                {
                    e.Handled = e.SuppressKeyPress = true;
                    Close();
                }
            }
        }

        private void OutputLog_ItemAppened(LogItem block)
        {
            Print(block);
        }

        private void Print(LogItem block)
        {
            InvokeEx(() => 
            {
                string text = block.Text.Replace("\r", ""); // RichTextBox автоматически отсекает '\r'

                richTextBox.AppendText(text);
                if (block.ForeColor.HasValue)
                {
                    richTextBox.Select(richTextBox.TextLength - text.Length, text.Length);
                    richTextBox.SelectionColor = block.ForeColor.Value;
                }
                richTextBox.Select(richTextBox.TextLength, 0);

                Application.DoEvents(); // для того, чтобы нажатия клавиш не уходили в очередь сообщений
            });
        }

        private void InvokeEx(Action action)
        {
            if (InvokeRequired)
            {
                Invoke(action);
            }
            else
            {
                action();
            }
        }
    }
}
