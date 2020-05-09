using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace CSScript
{
    /// <summary>
    /// Представляет реализацию графического интерфейса программы
    /// </summary>
    internal class GUIProgram : Program
    {
        public GUIProgram(string[] args) : base(args)
        {
            ProgramModel.MessageManager.MessageAdded += ProgramModel_MessageAdded;
            ProgramModel.FinishedEvent += ProgramModel_FinishedEvent;
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

        private void ProgramModel_MessageAdded(object sender, Message message)
        {
            Debug.Write(message.Text);
            Console.WriteLine(message.Text);
        }

        private void ProgramModel_FinishedEvent(object sender, bool autoClose)
        {
            if (autoClose)
            {
                Application.Exit();
            }
        }
    }
}
