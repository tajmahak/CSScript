﻿using CSScript.Properties;
using System;
using System.Windows.Forms;

namespace CSScript
{
    /// <summary>
    /// Форма для вывода логов программы
    /// </summary>
    internal partial class LogForm : Form
    {
        private readonly Timer timer;
        private int currentLogPosition;

        public LogForm()
        {
            InitializeComponent();
            Icon = Resources.favicon;
            richTextBox.BackColor = Settings.Default.BackColor;
            richTextBox.ForeColor = Settings.Default.ForeColor;

            timer = new Timer();
            timer.Interval = 100;
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        private void Form_FormClosed(object sender, FormClosedEventArgs e)
        {
            timer.Stop();
        }

        private void Form_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape || e.KeyCode == Keys.Enter)
            {
                if (Program.ProgramModel.Finished)
                {
                    e.Handled = e.SuppressKeyPress = true;
                    Close();
                }
            }
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            timer.Stop();

            System.Collections.ObjectModel.ReadOnlyCollection<LogItem> logs = Program.ProgramModel.LogItems;
            while (currentLogPosition < logs.Count)
            {
                LogItem log = logs[currentLogPosition];
                InvokeEx(() => Print(log));
                currentLogPosition++;
            }

            if (!Program.ProgramModel.Finished)
            {
                timer.Start();
            }
        }

        private void Print(LogItem log)
        {
            string text = log.Text.Replace("\r", ""); // RichTextBox автоматически отсекает '\r'

            richTextBox.AppendText(text);
            if (log.ForeColor.HasValue)
            {
                richTextBox.Select(richTextBox.TextLength - text.Length, text.Length);
                richTextBox.SelectionColor = log.ForeColor.Value;
            }
            richTextBox.Select(richTextBox.TextLength, 0);
            Application.DoEvents(); // для того, чтобы нажатия клавиш не уходили в очередь сообщений
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
