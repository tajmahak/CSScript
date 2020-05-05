//#define RUN_DEBUG_SCRIPT

using System;
using System.Diagnostics;
using System.Reflection;
using System.Windows.Forms;

namespace CSScript
{
    internal static class Program
    {
        [STAThread]
        private static int Main(string[] args)
        {
#if DEBUG && RUN_DEBUG_SCRIPT
            args = new string[] { "/debug" };
#endif
            ProgramModel = new ProgramModel(Properties.Settings.Default, args);
            ProgramModel.AddLogEvent += ProgramModel_AddLogEvent;
            ProgramModel.FinishedEvent += ProgramModel_FinishedEvent;

            // для подгрузки библиотек рантаймом, которые не подгружаются самостоятельно
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolveEvent;

            Application.ApplicationExit += Application_ApplicationExit;

            if (ProgramModel.GUIMode)
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                logForm = new LogForm();

                ProgramModel.ExecuteScriptAsync(); // чтобы форма успевала прогрузиться до того, как будет уничтожена при завершении поточной операции
                Application.Run(logForm);
            }
            else
            {
                ProgramModel.ExecuteScriptAsync();
                ProgramModel.JoinExecutingThread();
            }
            return ProgramModel.ExitCode;
        }

        private static void Application_ApplicationExit(object sender, EventArgs e)
        {
            ProgramModel.Dispose();
        }

        private static Assembly CurrentDomain_AssemblyResolveEvent(object sender, ResolveEventArgs args)
        {
            return ProgramModel.ResolveAssembly(args.Name);
        }

        private static void ProgramModel_AddLogEvent(object sender, LogItem logItem)
        {
            Debug.Write(logItem.Text);
        }

        private static void ProgramModel_FinishedEvent(object sender, bool guiForceExit)
        {
            if (guiForceExit)
            {
                Application.Exit();
            }
        }


        internal static ProgramModel ProgramModel { get; private set; }

        private static LogForm logForm;
    }
}
