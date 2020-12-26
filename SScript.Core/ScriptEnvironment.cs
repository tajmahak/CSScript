﻿using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;

namespace CSScript.Core
{
    /// <summary>
    /// Представляет реализацию взаимодействия скрипта с программой.
    /// </summary>
    public class ScriptEnvironment : IScriptEnvironment, IDisposable
    {
        public string[] Args { get; }
        public int ExitCode { get; set; }
        public bool AutoClose { get; set; }
        public string ScriptPath { get; }
        public ColorScheme ColorScheme { get; set; } = ColorScheme.Default;
        private readonly List<Message> messageList = new List<Message>();
        private readonly List<Process> managedProcesses = new List<Process>();


        public ScriptEnvironment(string scriptPath, string[] scriptArgs)
        {
            ScriptPath = scriptPath;
            Args = scriptArgs;
        }


        public delegate void MessageAddedHandler(object sender, Message message);
        public event MessageAddedHandler MessageAdded;


        public Process CreateManagedProcess()
        {
            Process process = new Process();
            lock (managedProcesses) {
                managedProcesses.Add(process);
            }
            return process;
        }

        public string GetInputText()
        {
            return GetInputText(null);
        }

        public string GetInputText(string caption)
        {
            //!!!
            return null;
        }

        public void Write(object value, ConsoleColor? foreColor = null)
        {
            if (value != null) {
                string text = value.ToString();
                if (!string.IsNullOrEmpty(text)) {
                    foreColor = foreColor ?? ColorScheme.Fore;
                    Message message = new Message(text, foreColor.Value);
                    lock (messageList) {
                        messageList.Add(message);
                    }
                    MessageAdded?.Invoke(this, message);
                }
            }
        }

        public void WriteLine(object value, ConsoleColor? foreColor = null)
        {
            Write(value + Environment.NewLine, foreColor);
        }

        public void WriteLine()
        {
            Write(Environment.NewLine);
        }

        public void Dispose()
        {
            // принудительное закрытие выполняющихся контролируемых процессов
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


    }
}