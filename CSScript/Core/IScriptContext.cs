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
        ICollection<LogFragment> OutLog { get; }
        ICollection<LogFragment> ErrorLog { get; }
        ColorScheme ColorScheme { get; }

        void Write(object value, ConsoleColor? color = null);

        void WriteLine(object value, ConsoleColor? color = null);

        void WriteLine();

        void WriteError(object value);

        void WriteErrorLine(object value);

        void WriteErrorLine();

        string ReadLine(ConsoleColor? color = null);

        Process CreateManagedProcess();
    }
}
