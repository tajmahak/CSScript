using CSScript.Properties;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.IO;
using System.Text;

namespace CSScript
{
    /// <summary>
    /// Представляет средство для работы с информационными сообщениями.
    /// </summary>
    internal class MessageManager
    {
        public ReadOnlyCollection<Message> MessageList => messageList.AsReadOnly();

        private readonly List<Message> messageList = new List<Message>();

        private readonly ProgramModel programModel;



        public delegate void MessageAddedHandler(object sender, Message message);

        public event MessageAddedHandler MessageAdded;



        public MessageManager(ProgramModel programModel)
        {
            this.programModel = programModel;
        }



        public void Write(object value, Color? foreColor = null)
        {
            if (value != null)
            {
                string text = value.ToString();
                if (!string.IsNullOrEmpty(text))
                {
                    Message message = new Message(text, DateTime.Now, foreColor);
                    lock (messageList)
                    {
                        messageList.Add(message);
                    }
                    MessageAdded?.Invoke(this, message);
                }
            }
        }

        public void WriteLine(object value, Color? foreColor = null)
        {
            Write(value + Environment.NewLine, foreColor);
        }

        public void WriteLine()
        {
            Write(Environment.NewLine);
        }

        public void WriteHelpInfo()
        {
            string[] lines = Resources.HelpText.Split(new string[] { "\r\n" }, StringSplitOptions.None);
            for (int i = 0; i < lines.Length; i++)
            {
                Color? color = null;
                string[] line = ParseHelpInfoLine(lines[i]);
                switch (line[0])
                {
                    case "c": color = programModel.Settings.CaptionColor; break;
                    case "i": color = programModel.Settings.InfoColor; break;
                }
                WriteLine(line[1], color);
            }
        }

        public void WriteStartInfo(string scriptPath)
        {
            WriteLine($"## {scriptPath}", programModel.Settings.InfoColor);
            WriteLine($"## {DateTime.Now}", programModel.Settings.InfoColor);
            WriteLine();
        }

        public void WriteException(Exception ex)
        {
            WriteLine($"# Ошибка: {ex.Message}", programModel.Settings.ErrorColor);
            foreach (string stackTraceLine in ex.StackTrace.Split(new string[] { Environment.NewLine }, StringSplitOptions.None))
            {
                WriteLine("#" + stackTraceLine, programModel.Settings.StackTraceColor);
            }
        }

        public void WriteCompileErrors(CompilerResults compilerResults)
        {
            WriteLine($"# Ошибок компиляции: {compilerResults.Errors.Count}", programModel.Settings.ErrorColor);
            int errorNumber = 1;
            foreach (CompilerError error in compilerResults.Errors)
            {
                if (error.Line > 0)
                {
                    WriteLine($"# {errorNumber++} (cтрока {error.Line}): {error.ErrorText}", programModel.Settings.ErrorColor);
                }
                else
                {
                    WriteLine($"# {errorNumber++}: {error.ErrorText}", programModel.Settings.ErrorColor);
                }
            }
        }

        public void WriteSourceCode(string sourceCode, CompilerResults compilerResults = null)
        {
            HashSet<int> errorLines = new HashSet<int>();
            if (compilerResults != null)
            {
                foreach (CompilerError error in compilerResults.Errors)
                {
                    errorLines.Add(error.Line);
                }
            }

            string[] lines = sourceCode.Split(new string[] { "\r\n" }, StringSplitOptions.None);
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                int lineNumber = i + 1;

                Write(lineNumber.ToString().PadLeft(4) + ": ");
                if (errorLines.Contains(lineNumber))
                {
                    WriteLine(line, programModel.Settings.ErrorColor);
                }
                else
                {
                    if (line.TrimStart().StartsWith("//"))
                    {
                        WriteLine(line, programModel.Settings.CommentColor);
                    }
                    else
                    {
                        WriteLine(line, programModel.Settings.SourceCodeColor);
                    }
                }
            }
        }

        public void WriteExitCode(int exitCode)
        {
            WriteLine();
            WriteLine($"# Выполнено с кодом возврата: {exitCode}", programModel.Settings.InfoColor);
        }

        public string GetLog()
        {
            StringBuilder messageLog = new StringBuilder();
            foreach (Message message in messageList)
            {
                messageLog.Append(message.Text);
            }
            return messageLog.ToString();
        }

        public void SaveLog(string logPath)
        {
            if (!string.IsNullOrEmpty(logPath))
            {
                try
                {
                    SaveLogInternal(logPath);
                }
                catch (Exception ex)
                {
                    WriteLine($"# Не удалось сохранить лог: {ex.Message}", programModel.Settings.ErrorColor);
                }
            }
        }

        private void SaveLogInternal(string logPath)
        {
            string messageLog = GetLog();
            using (StreamWriter writer = new StreamWriter(logPath, true, Encoding.UTF8))
            {
                writer.WriteLine(messageLog);
                writer.WriteLine();
                writer.WriteLine();
            }
        }

        private string[] ParseHelpInfoLine(string line)
        {
            if (line.StartsWith("`"))
            {
                int index = line.IndexOf("`", 1);
                return new string[]
                {
                    line.Substring(1, index - 1),
                    line.Substring(index + 1),
                };
            }
            else
            {
                return new string[] { null, line };
            }
        }
    }
}
