using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace CSScript.Core
{
    /// <summary>
    /// Представляет реализацию взаимодействия скрипта с программой.
    /// </summary>
    public class ScriptContext : IScriptContext, IDisposable
    {
        public string ScriptPath { get; }
        public string[] Args { get; }
        public bool Pause { get; set; }
        public int ExitCode { get; set; }
        public ColorScheme ColorScheme { get; set; } = ColorScheme.Default;
        public ICollection<LogFragment> OutLog => outLog.AsReadOnly();
        public ICollection<LogFragment> ErrorLog => errorLog.AsReadOnly();

        private readonly List<LogFragment> outLog = new List<LogFragment>();
        private readonly List<LogFragment> errorLog = new List<LogFragment>();
        private readonly List<Process> managedProcesses = new List<Process>();


        public ScriptContext(string scriptPath, string[] scriptArgs) {
            ScriptPath = scriptPath;
            Args = scriptArgs;
        }


        public delegate void LogFragmentAddedHandler(object sender, LogFragment logFragment);
        public event LogFragmentAddedHandler OutputLogFragmentAdded;
        public event LogFragmentAddedHandler ErrorLogFragmentAdded;

        public delegate string ReadLineRequredHandler(object sender, ConsoleColor color);
        public event ReadLineRequredHandler ReadLineRequred;


        public void Write(object value, ConsoleColor? color = null) {
            if (value != null) {
                string text = value.ToString();
                if (!string.IsNullOrEmpty(text)) {
                    LogFragment logFragment = new LogFragment(text, color ?? ColorScheme.Foreground);
                    lock (outLog) {
                        outLog.Add(logFragment);
                    }
                    OutputLogFragmentAdded?.Invoke(this, logFragment);
                }
            }
        }

        public void WriteLine(object value, ConsoleColor? color = null) {
            Write(value + Environment.NewLine, color);
        }

        public void WriteLine() {
            Write(Environment.NewLine);
        }

        public void WriteError(object value) {
            if (value != null) {
                string text = value.ToString();
                if (!string.IsNullOrEmpty(text)) {
                    LogFragment logFragment = new LogFragment(text, ColorScheme.Error);
                    lock (errorLog) {
                        errorLog.Add(logFragment);
                    }
                    ErrorLogFragmentAdded?.Invoke(this, logFragment);
                }
            }
        }

        public void WriteErrorLine(object value) {
            WriteError(value + Environment.NewLine);
        }

        public void WriteErrorLine() {
            WriteError(Environment.NewLine);
        }

        public string ReadLine(ConsoleColor? color = null) {
            return ReadLineRequred.Invoke(this, color ?? ColorScheme.Foreground);
        }

        public Process CreateManagedProcess() {
            Process process = new Process();
            lock (managedProcesses) {
                managedProcesses.Add(process);
            }
            return process;
        }

        public void Dispose() {
            // Принудительное закрытие выполняющихся контролируемых процессов
            lock (managedProcesses) {
                for (int i = 0; i < managedProcesses.Count; i++) {
                    try {
                        managedProcesses[i].Kill();
                    } catch {
                    }
                }
            }
        }
    }
}
