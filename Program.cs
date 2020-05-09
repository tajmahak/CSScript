using CSScript.Properties;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;

namespace CSScript
{
    internal class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            Program program = new Program();
            program.ExecuteProgram(args);
        }


        private ProgramModel programModel;

        private LogForm logForm;

        private void ExecuteProgram(string[] args)
        {
            try
            {
                programModel = new ProgramModel(Settings.Default, args);
                programModel.MessageManager.MessageAdded += ProgramModel_MessageAdded;
                programModel.FinishedEvent += ProgramModel_FinishedEvent;

                // для подгрузки библиотек рантаймом, которые не подгружаются самостоятельно
                AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolveEvent;

                if (programModel.GUIMode)
                {
                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);
                    logForm = new LogForm(programModel);

                    programModel.StartAsync(); // чтобы форма успевала прогрузиться до того, как будет уничтожена при завершении поточной операции
                    Application.Run(logForm);
                }
                else
                {
                    Thread thread = programModel.StartAsync();
                    thread.Join();
                }
            }
            finally
            {
                if (programModel != null)
                {
                    Environment.ExitCode = programModel.ExitCode;
                }
                programModel?.Dispose();
                logForm?.Dispose();
            }
        }

        private Assembly CurrentDomain_AssemblyResolveEvent(object sender, ResolveEventArgs args)
        {
            return programModel.ResolveAssembly(args.Name);
        }

        private void ProgramModel_MessageAdded(object sender, Message message)
        {
            Debug.Write(message.Text);
        }

        private void ProgramModel_FinishedEvent(object sender, bool guiForceExit)
        {
            if (guiForceExit)
            {
                Application.Exit();
            }
        }
    }
}
