using System;
using System.Diagnostics;

namespace CSScript.Core
{
    /// <summary>
    /// Представляет реализацию взаимодействия скрипта с программой.
    /// </summary>
    internal class ScriptEnvironment : IScriptEnvironment
    {
        private readonly ScriptHandler scriptHandler;

        public ScriptEnvironment(ScriptHandler scriptHandler, string scriptPath, string[] scriptArgs)
        {
            this.scriptHandler = scriptHandler;
            ScriptPath = scriptPath;
            Args = scriptArgs;
        }

        public string[] Args { get; }

        public int ExitCode { get; set; }

        public bool AutoClose { get; set; }

        public string ScriptPath { get; }

        public Process CreateManagedProcess()
        {
            return scriptHandler.CreateManagedProcess();
        }

        public string GetInputText()
        {
            return scriptHandler.GetInputText(null);
        }

        public string GetInputText(string caption)
        {
            return scriptHandler.GetInputText(caption);
        }

        public void Write(object value, ConsoleColor? foreColor = null)
        {
            scriptHandler.Messages.Write(value, foreColor);
        }

        public void WriteLine(object value, ConsoleColor? foreColor = null)
        {
            scriptHandler.Messages.Write(value, foreColor);
        }

        public void WriteLine()
        {
            scriptHandler.Messages.WriteLine();
        }
    }
}
