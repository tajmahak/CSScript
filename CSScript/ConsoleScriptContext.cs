﻿using CSScript.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace CSScript
{
    public class ConsoleScriptContext : IScriptContext
    {
        private readonly List<LogFragment> outLog = new List<LogFragment>();
        private readonly List<LogFragment> errorLog = new List<LogFragment>();
        private readonly List<Process> managedProcesses = new List<Process>();

        public string ScriptPath { get; set; }

        public string[] Args { get; set; }

        public bool Pause { get; set; } = true;

        public bool HiddenMode { get; set; }

        public int ExitCode { get; set; }

        public ColorScheme ColorScheme { get; set; } = ColorScheme.Default;

        public IList<LogFragment> OutLog => outLog.AsReadOnly();

        public IList<LogFragment> ErrorLog => errorLog.AsReadOnly();

        public string ReadLine(ConsoleColor? color = null) {
            if (HiddenMode || Thread.CurrentThread.ThreadState != System.Threading.ThreadState.Running) {
                return null;
            }
            lock (outLog) {
                color = color ?? ColorScheme.Foreground;
                ConsoleColor prevColor = Console.ForegroundColor;
                if (color != prevColor) {
                    Console.ForegroundColor = color.Value;
                }
                string line = HiddenMode ? null : Console.ReadLine();
                if (!string.IsNullOrEmpty(line)) {
                    outLog.Add(new LogFragment(line, Console.ForegroundColor, false));
                }
                if (color != prevColor) {
                    Console.ForegroundColor = prevColor;
                }
                return line;
            }
        }

        public void RegisterProcess(Process process) {
            lock (managedProcesses) {
                managedProcesses.Add(process);
            }
        }

        public void Write(object value, ConsoleColor? color = null) {
            Write(value, color, Console.Out, false, outLog);
        }

        public void WriteLine(object value, ConsoleColor? color = null) {
            Write(value + Environment.NewLine, color);
        }

        public void WriteLine() {
            Write(Environment.NewLine);
        }

        public void WriteError(object value) {
            Write(value, ColorScheme.Error, Console.Error, true, errorLog);
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


        private void Write(object value, ConsoleColor? color, TextWriter writer, bool error, IList<LogFragment> log) {
            color = color ?? ColorScheme.Foreground;
            string strValue = value?.ToString();
            if (!string.IsNullOrEmpty(strValue) && Thread.CurrentThread.ThreadState == System.Threading.ThreadState.Running) {
                lock (log) {
                    ConsoleColor prevColor = Console.ForegroundColor;
                    if (color != prevColor) {
                        Console.ForegroundColor = color.Value;
                    }
                    writer.Write(strValue);
                    log.Add(new LogFragment(strValue, Console.ForegroundColor, error));
                    if (color != prevColor) {
                        Console.ForegroundColor = prevColor;
                    }
                }
            }
        }
    }
}