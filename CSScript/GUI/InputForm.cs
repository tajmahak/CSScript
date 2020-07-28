using CSScript.Properties;
using System;
using System.Windows.Forms;

namespace CSScript
{
    /// <summary>
    /// Форма для вывода логов программы
    /// </summary>
    internal partial class InputForm : Form
    {
        private readonly ProgramModel programModel;

        public InputForm(ProgramModel programModel)
        {
            InitializeComponent();
            Icon = Resources.favicon;
            richTextBox.BackColor = programModel.Settings.BackColor;
            richTextBox.ForeColor = programModel.Settings.ForeColor;
            richTextBox.WordWrap = programModel.Settings.WordWrap;

            this.programModel = programModel;
        }

        public string InputText => richTextBox.Text.Replace("\n", "\r\n");

        private void Form_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                e.Handled = e.SuppressKeyPress = true;
                DialogResult = DialogResult.Cancel;
            }

            else if (e.Control && e.KeyCode == Keys.Enter)
            {
                e.Handled = e.SuppressKeyPress = true;
                DialogResult = DialogResult.OK;
            }
        }
    }
}
