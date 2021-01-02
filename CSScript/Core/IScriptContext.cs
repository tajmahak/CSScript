using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace CSScript.Core
{
    /// <summary>
    /// Представляет интерфейс контекста для взаимодействия скрипта с окружением.
    /// </summary>
    public interface IScriptContext
    {
        string[] Args { get; }
        int ExitCode { get; set; }
        bool Pause { get; set; }
        string ScriptPath { get; }
        ICollection<LogFragment> Log { get; }
        ColorScheme ColorScheme { get; }

        void Write(object value, ConsoleColor? color = null);

        void WriteLine(object value, ConsoleColor? color = null);

        void WriteLine();

        string ReadLine(ConsoleColor? color = null);

        Process CreateManagedProcess();
    }
}
