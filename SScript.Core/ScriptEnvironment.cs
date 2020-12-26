using System;
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
                    Message message = new Message(text, DateTime.Now, foreColor.Value);
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

        public void WriteHelpInfo(string helpText)
        {
            string[] lines = helpText.Split(new string[] { "\r\n" }, StringSplitOptions.None);
            for (int i = 0; i < lines.Length; i++) {
                ConsoleColor? color = null;
                string[] line = ParseHelpInfoLine(lines[i]);
                switch (line[0]) {
                    case "c": color = ColorScheme.Caption; break;
                    case "i": color = ColorScheme.Info; break;
                }
                WriteLine(line[1], color);
            }
        }

        public void WriteStartInfo(string scriptPath)
        {
            WriteLine($"## {scriptPath}", ColorScheme.Info);
            WriteLine($"## {DateTime.Now}", ColorScheme.Info);
            WriteLine();
        }

        public void WriteException(Exception ex)
        {
            WriteLine($"# Ошибка: {ex.Message}", ColorScheme.Error);
            foreach (string stackTraceLine in ex.StackTrace.Split(new string[] { Environment.NewLine }, StringSplitOptions.None)) {
                WriteLine("#" + stackTraceLine, ColorScheme.StackTrace);
            }

            if (ex.InnerException != null) {
                WriteLine();
                WriteException(ex.InnerException);
            }
        }

        public void WriteCompileErrors(CompilerResults compilerResults)
        {
            WriteLine($"# Ошибок компиляции: {compilerResults.Errors.Count}", ColorScheme.Error);
            int errorNumber = 1;
            foreach (CompilerError error in compilerResults.Errors) {
                if (error.Line > 0) {
                    WriteLine($"# {errorNumber++} (cтрока {error.Line}): {error.ErrorText}", ColorScheme.Error);
                }
                else {
                    WriteLine($"# {errorNumber++}: {error.ErrorText}", ColorScheme.Error);
                }
            }
        }

        public void WriteSourceCode(string sourceCode, CompilerResults compilerResults = null)
        {
            HashSet<int> errorLines = new HashSet<int>();
            if (compilerResults != null) {
                foreach (CompilerError error in compilerResults.Errors) {
                    errorLines.Add(error.Line);
                }
            }

            string[] lines = sourceCode.Split(new string[] { "\r\n" }, StringSplitOptions.None);
            for (int i = 0; i < lines.Length; i++) {
                string line = lines[i];
                int lineNumber = i + 1;

                Write(lineNumber.ToString().PadLeft(4) + ": ");
                if (errorLines.Contains(lineNumber)) {
                    WriteLine(line, ColorScheme.Error);
                }
                else {
                    if (line.TrimStart().StartsWith("//")) {
                        WriteLine(line, ColorScheme.Comment);
                    }
                    else {
                        WriteLine(line, ColorScheme.SourceCode);
                    }
                }
            }
        }

        public void WriteExitCode(int exitCode)
        {
            WriteLine();
            WriteLine($"# Выполнено с кодом возврата: {exitCode}", ColorScheme.Info);
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


        private string[] ParseHelpInfoLine(string line)
        {
            if (line.StartsWith("`")) {
                int index = line.IndexOf("`", 1);
                return new string[]
                {
                    line.Substring(1, index - 1),
                    line.Substring(index + 1),
                };
            }
            else {
                return new string[] { null, line };
            }
        }
    }
}
