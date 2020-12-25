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
        private int currentMessageIndex;
        private bool stopLogOutput;

        public LogForm(ProgramModel programModel)
        {
            InitializeComponent();
            Icon = Resources.favicon;
            richTextBox.BackColor = programModel.Settings.BackColor;
            richTextBox.ForeColor = programModel.Settings.ForeColor;
            richTextBox.WordWrap = programModel.Settings.WordWrap;

            this.programModel = programModel;
            timer = new Timer {
                Interval = 100
            };
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
            if (e.KeyCode == Keys.Escape || e.KeyCode == Keys.Enter) {
                if (programModel.Finished) {
                    e.Handled = e.SuppressKeyPress = true;
                    Close();
                }
            }
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            timer.Stop();

            System.Collections.ObjectModel.ReadOnlyCollection<Message> messageList = programModel.MessageManager.MessageList;
            while (currentMessageIndex < messageList.Count) {
                if (stopLogOutput) {
                    break;
                }
                Message message = messageList[currentMessageIndex++];
                PrintMessage(message);
            }

            if (!programModel.Finished && !stopLogOutput) {
                timer.Start();
            }
        }

        private void PrintMessage(Message message)
        {
            string text = message.Text.Replace("\r", ""); // RichTextBox автоматически отсекает '\r'

            richTextBox.AppendText(text);
            if (message.ForeColor.HasValue) {
                richTextBox.Select(richTextBox.TextLength - text.Length, text.Length);
                richTextBox.SelectionColor = message.ForeColor.Value;
                richTextBox.Select(richTextBox.TextLength, 0);
            }
        }
    }
}
