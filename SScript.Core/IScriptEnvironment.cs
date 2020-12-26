using System;
using System.Diagnostics;

namespace CSScript.Core
{
    /// <summary>
    /// Представляет интерфейс для взаимодействия скрипта с программой.
    /// </summary>
    public interface IScriptEnvironment
    {
        string[] Args { get; }

        int ExitCode { get; set; }

        bool AutoClose { get; set; }

        string ScriptPath { get; }

        void Write(object value, ConsoleColor? foreColor = null);

        void WriteLine(object value, ConsoleColor? foreColor = null);

        void WriteLine();

        string GetInputText();

        string GetInputText(string caption);

        Process CreateManagedProcess();
    }
}
