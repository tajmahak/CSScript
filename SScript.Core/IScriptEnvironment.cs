using System;
using System.Diagnostics;

namespace CSScript.Core
{
    /// <summary>
    /// Представляет интерфейс для взаимодействия скрипта с окружением.
    /// </summary>
    public interface IScriptEnvironment
    {
        string[] Args { get; }

        ColorScheme ColorScheme { get; }

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
