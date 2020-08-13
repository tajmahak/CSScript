using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace CSScript
{
    /// <summary>
    /// Представляет реализацию графического интерфейса программы
    /// </summary>
    internal class GUIProgram : ProgramBase
    {
        public GUIProgram(ProgramModel programModel) : base(programModel)
        {
            ProgramModel.MessageManager.MessageAdded += MessageManager_MessageAdded;
            ProgramModel.FinishedEvent += ProgramModel_FinishedEvent;
            ProgramModel.InputTextEvent += ProgramModel_InputTextEvent;
        }

        private LogForm logForm;

        protected override void StartProgram()
        {
            try
            {
                if (ProgramModel.HideMode)
                {
                    ProgramModel.StartAsync().Join();
                }
                else
                {
                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);
                    logForm = new LogForm(ProgramModel);

                    ProgramModel.StartAsync(); // чтобы форма успевала прогрузиться до того, как будет уничтожена при завершении поточной операции
                    Application.Run(logForm);
                }
            }
            finally
            {
                if (ProgramModel != null)
                {
                    Environment.ExitCode = ProgramModel.ExitCode;
                }
                ProgramModel?.Dispose();
                logForm?.Dispose();
            }
        }

        private void MessageManager_MessageAdded(object sender, Message message)
        {
            Debug.Write(message.Text);
        }

        private void ProgramModel_FinishedEvent(object sender, bool autoClose)
        {
            if (autoClose)
            {
                Application.Exit();
            }
        }

        private string ProgramModel_InputTextEvent(object sender, string caption)
        {
            string inputData = null;
            InputForm inputForm = new InputForm(ProgramModel);
            if (!string.IsNullOrEmpty(caption))
            {
                inputForm.Text = caption;
            }
            InvokeEx(() =>
            {
                if (inputForm.ShowDialog(logForm) == DialogResult.OK)
                {
                    inputData = inputForm.InputText;
                }
            });
            return inputData;
        }

        private void InvokeEx(Action action)
        {
            if (logForm.InvokeRequired)
            {
                logForm.Invoke(action);
            }
            else
            {
                action();
            }
        }
    }
}
