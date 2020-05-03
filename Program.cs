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
            bool startDebugScript = false;
#if DEBUG
            startDebugScript = true;
#endif

            ProgramModel = startDebugScript ? new ProgramModel() : new ProgramModel(args);
            ProgramModel.FinishedEvent += ProgramModel_FinishedEvent;
            ProgramModel.ShowGUIEvent += ProgramModel_ShowGUIEvent;
            ProgramModel.AddLogEvent += ProgramModel_AddLogEvent;

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
            ProgramModel.KillManagedProcesses();
        }

        private static Assembly CurrentDomain_AssemblyResolveEvent(object sender, ResolveEventArgs args)
        {
            return ProgramModel.ResolveAssembly(args.Name);
        }

        private static void ProgramModel_ShowGUIEvent()
        {
            logForm.ShowForm();
        }

        private static void ProgramModel_FinishedEvent()
        {
            if (ProgramModel.GUIMode && ProgramModel.AutoCloseGUI)
            {
                logForm.CloseForm();
            }
        }

        private static void ProgramModel_AddLogEvent(LogItem logItem)
        {
            Debug.WriteLine(logItem.Text);
        }


        internal static ProgramModel ProgramModel { get; private set; }

        private static LogForm logForm;
    }
}
