using CSScript.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace CSScript
{
    public class ConsoleContext : IScriptContext
    {
        private readonly List<LogFragment> outLog = new List<LogFragment>();
        private readonly List<LogFragment> errorLog = new List<LogFragment>();
        private readonly List<Process> managedProcesses = new List<Process>();

        public string ScriptPath { get; set; }

        public string[] Args { get; set; }

        public bool Pause { get; set; }

        public int ExitCode { get; set; }

        public bool Hidden { get; set; }

        public ColorScheme ColorScheme { get; set; }

        public IList<LogFragment> OutLog => outLog.AsReadOnly();

        public IList<LogFragment> ErrorLog => errorLog.AsReadOnly();

        public string ReadLine(ConsoleColor? color = null) {
            color = color ?? ColorScheme.Foreground;
            lock (outLog) {
                if (Hidden) {
                    return null;
                } else {
                    ConsoleColor prevColor = Console.ForegroundColor;
                    if (color != prevColor) {
                        Console.ForegroundColor = color.Value;
                    }
                    string line = Hidden ? null : Console.ReadLine();
                    if (!string.IsNullOrEmpty(line)) {
                        outLog.Add(new LogFragment(line, Console.ForegroundColor));
                    }
                    if (color != prevColor) {
                        Console.ForegroundColor = prevColor;
                    }
                    return line;
                }
            }
        }

        public void RegisterProcess(Process process) {
            lock (managedProcesses) {
                managedProcesses.Add(process);
            }
        }

        public void Write(object value, ConsoleColor? color = null) {
            Write(value, color, Console.Out, outLog);
        }

        public void WriteLine(object value, ConsoleColor? color = null) {
            Write(value + Environment.NewLine, color);
        }

        public void WriteLine() {
            Write(Environment.NewLine);
        }

        public void WriteError(object value) {
            Write(value, ColorScheme.Error, Console.Error, errorLog);
        }

        public void WriteErrorLine(object value) {
            WriteError(value + Environment.NewLine);
        }

        public void WriteErrorLine() {
            WriteError(Environment.NewLine);
        }


        public void KillManagedProcesses() {
            lock (managedProcesses) {
                foreach (Process managedProcess in managedProcesses) {
                    try {
                        managedProcess.Kill();
                    } catch {
                    }
                }
                managedProcesses.Clear();
            }
        }


        private void Write(object value, ConsoleColor? color, TextWriter writer, IList<LogFragment> log) {
            color = color ?? ColorScheme.Foreground;
            string strValue = value?.ToString();
            if (!string.IsNullOrEmpty(strValue)) {
                lock (log) {
                    ConsoleColor prevColor = Console.ForegroundColor;
                    if (color != prevColor) {
                        Console.ForegroundColor = color.Value;
                    }
                    writer.Write(strValue);
                    log.Add(new LogFragment(strValue, Console.ForegroundColor));
                    if (color != prevColor) {
                        Console.ForegroundColor = prevColor;
                    }
                }
            }
        }
    }
}
