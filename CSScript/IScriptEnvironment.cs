using CSScript.Properties;
using System.Diagnostics;
using System.Drawing;

namespace CSScript
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

        Settings Settings { get; }

        void Write(object value, Color? foreColor = null);

        void WriteLine(object value, Color? foreColor = null);

        void WriteLine();

        Process CreateManagedProcess();
    }
}
