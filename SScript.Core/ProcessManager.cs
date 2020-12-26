using System.Collections.Generic;
using System.Diagnostics;

namespace CSScript.Core
{
    /// <summary>
    /// Представляет средство для работы с процессами, запускаемыми скриптом.
    /// </summary>
    internal class ProcessManager
    {
        private readonly List<Process> managedProcesses = new List<Process>();

        public Process CreateManagedProcess()
        {
            Process process = new Process();
            lock (managedProcesses) {
                managedProcesses.Add(process);
            }
            return process;
        }

        public void KillManagedProcesses()
        {
            lock (managedProcesses) {
                for (int i = 0; i < managedProcesses.Count; i++) {
                    try {
                        managedProcesses[i].Kill();
                    }
                    catch {
                    }
                }
            }
        }

        public static void RestartWindowsExplorer()
        {
            Process[] explorer = Process.GetProcessesByName("explorer");
            foreach (Process process in explorer) {
                process.Kill();
            }
            // Запускается автоматически
            //Process.Start("explorer.exe");
        }
    }
}
