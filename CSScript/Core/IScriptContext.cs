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
        string ScriptPath { get; }

        string[] Args { get; }

        bool Pause { get; set; }

        int ExitCode { get; set; }

        ColorScheme ColorScheme { get; }

        ICollection<LogFragment> OutLog { get; }

        ICollection<LogFragment> ErrorLog { get; }

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
