using CSScript.Properties;
using System;
using System.Windows.Forms;

namespace CSScript
{
    /// <summary>
    /// Форма для вывода логов программы
    /// </summary>
    internal partial class LogForm : Form
    {
        private readonly ProgramModel programModel;
        private readonly Timer timer;
        private int currentLogPosition;
        private bool stopLogOutput;

        public LogForm(ProgramModel programModel)
        {
            InitializeComponent();
            Icon = Resources.favicon;
            richTextBox.BackColor = Settings.Default.BackColor;
            richTextBox.ForeColor = Settings.Default.ForeColor;
            richTextBox.WordWrap = Settings.Default.WordWrap;

            this.programModel = programModel;
            timer = new Timer();
            timer.Interval = 100;
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        private void Form_FormClosed(object sender, FormClosedEventArgs e)
        {
            stopLogOutput = true;
            timer.Dispose();
        }

        private void Form_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape || e.KeyCode == Keys.Enter)
            {
                if (programModel.Finished)
                {
                    e.Handled = e.SuppressKeyPress = true;
                    Close();
                }
            }
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            timer.Stop();

            System.Collections.ObjectModel.ReadOnlyCollection<Message> logs = programModel.MessageList;
            while (currentLogPosition < logs.Count)
            {
                if (stopLogOutput)
                {
                    break;
                }
                Message log = logs[currentLogPosition];
                Print(log);
                currentLogPosition++;
            }

            if (!programModel.Finished && !stopLogOutput)
            {
                timer.Start();
            }
        }

        private void Print(Message log)
        {
            string text = log.Text.Replace("\r", ""); // RichTextBox автоматически отсекает '\r'

            richTextBox.AppendText(text);
            if (log.ForeColor.HasValue)
            {
                richTextBox.Select(richTextBox.TextLength - text.Length, text.Length);
                richTextBox.SelectionColor = log.ForeColor.Value;
                richTextBox.Select(richTextBox.TextLength, 0);
            }
        }
    }
}
