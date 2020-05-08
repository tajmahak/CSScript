using System.Collections.Generic;
using System.Diagnostics;

namespace CSScript
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
            lock (managedProcesses)
            {
                managedProcesses.Add(process);
            }
            return process;
        }

        public void KillManagedProcesses()
        {
            lock (managedProcesses)
            {
                for (int i = 0; i < managedProcesses.Count; i++)
                {
                    try
                    {
                        managedProcesses[i].Kill();
                    }
                    catch
                    {
                    }
                }
            }
        }
    }
}
