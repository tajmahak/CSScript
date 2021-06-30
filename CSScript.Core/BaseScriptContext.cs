using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace CSScript.Core
{
    public abstract class BaseScriptContext : IScriptContext
    {
        private readonly List<LogFragment> log = new List<LogFragment>();
        private readonly List<Process> registeredProcesses = new List<Process>();


        public string ScriptPath { get; set; }

        public string[] Args { get; set; }

        public bool HiddenMode { get; set; }

        public bool Pause { get; set; }

        public int ExitCode { get; set; }

        public ColorScheme ColorScheme { get; set; } = ColorScheme.Default;

        public IList<LogFragment> Log => log.AsReadOnly();


        public string ReadLine(ConsoleColor? color = null) {
            if (HiddenMode || Thread.CurrentThread.ThreadState != System.Threading.ThreadState.Running) {
                return null;
            }
            lock (log) {
                color = color ?? ColorScheme.Foreground;
                string text = OnReadLine(color.Value);
                WriteLog(text, color.Value, false);
                return text;
            }
        }

        public void Write(object value, ConsoleColor? color = null) {
            string text = value?.ToString();
            if (!string.IsNullOrEmpty(text) && Thread.CurrentThread.ThreadState == System.Threading.ThreadState.Running) {
                color = color ?? ColorScheme.Foreground;
                WriteLog(text, color.Value, false);
                OnWrite(text, color.Value);
            }
        }

        public void WriteError(object value) {
            string text = value?.ToString();
            if (!string.IsNullOrEmpty(text) && Thread.CurrentThread.ThreadState == System.Threading.ThreadState.Running) {
                WriteLog(text, ColorScheme.Error, true);
                OnWriteError(text, ColorScheme.Error);
            }
        }

        public void WriteErrorLine(object value) {
            WriteError(value + Environment.NewLine);
        }

        public void WriteErrorLine() {
            WriteError(Environment.NewLine);
        }

        public void WriteLine(object value, ConsoleColor? color = null) {
            Write(value + Environment.NewLine, color);
        }

        public void WriteLine() {
            Write(Environment.NewLine, null);
        }

        public void RegisterProcess(Process process) {
            lock (registeredProcesses) {
                registeredProcesses.Add(process);
            }
        }


        public abstract void OnWrite(string value, ConsoleColor color);

        public abstract void OnWriteError(string value, ConsoleColor color);

        public abstract string OnReadLine(ConsoleColor color);


        public void KillRegisteredProcesses() {
            lock (registeredProcesses) {
                foreach (Process registeredProcess in registeredProcesses) {
                    try {
                        registeredProcess.Kill();
                    } catch {
                    }
                }
                registeredProcesses.Clear();
            }
        }


        private void WriteLog(string text, ConsoleColor color, bool isError) {
            if (!string.IsNullOrEmpty(text)) {
                lock (log) {
                    log.Add(new LogFragment(text, color, isError));
                }
            }
        }
    }
}
