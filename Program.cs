//#define USE_DEBUG_SCRIPT_STAND // использовать стенд для отладки скриптов

using System;
using System.Diagnostics;
using System.Reflection;
using System.Windows.Forms;

namespace CSScript
{
    internal class Program
    {
        [STAThread]
        private static int Main(string[] args)
        {
            Program program = new Program();
            return program.ExecuteProgram(args);
        }



        private ProgramModel programModel;

        private LogForm logForm;

        private int ExecuteProgram(string[] args)
        {
#if DEBUG && USE_DEBUG_SCRIPT_STAND
            args = new string[] { "/debug" };
#endif
            programModel = new ProgramModel(Properties.Settings.Default, args);
            programModel.AddMessageEvent += ProgramModel_AddMessageEvent;
            programModel.FinishedEvent += ProgramModel_FinishedEvent;

            // для подгрузки библиотек рантаймом, которые не подгружаются самостоятельно
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolveEvent;

            Application.ApplicationExit += Application_ApplicationExit;

            if (programModel.GUIMode)
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                logForm = new LogForm(programModel);

                programModel.ExecuteScriptAsync(); // чтобы форма успевала прогрузиться до того, как будет уничтожена при завершении поточной операции
                Application.Run(logForm);
            }
            else
            {
                programModel.ExecuteScriptAsync();
                programModel.JoinExecutingThread();
            }
            return programModel.ExitCode;
        }

        private void Application_ApplicationExit(object sender, EventArgs e)
        {
            if (programModel != null)
            {
                programModel.Dispose();
            }
            if (logForm != null)
            {
                logForm.Dispose();
            }
        }

        private Assembly CurrentDomain_AssemblyResolveEvent(object sender, ResolveEventArgs args)
        {
            return programModel.ResolveAssembly(args.Name);
        }

        private void ProgramModel_AddMessageEvent(object sender, Message logItem)
        {
            Debug.Write(logItem.Text);
        }

        private void ProgramModel_FinishedEvent(object sender)
        {
            if (programModel.GUIForceExit)
            {
                Application.Exit();
            }
        }
    }
}
