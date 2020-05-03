using CSScript.Properties;
using System.Drawing;

namespace CSScript
{
    /// <summary>
    /// Представляет оболочку для выполнения скомпилированного скрипта в программе 
    /// </summary>
    public abstract class ScriptRuntime
    {
        /// <summary>
        /// Запуск выполнения скрипта
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        public abstract int StartScript(string arg);

        // Сущности, используемые в скрипте (public):

        public static void Write(object value, Color? color = null)
        {
            Program.ProgramModel.WriteLog(value.ToString(), color);
        }

        public static void WriteLine(object value, Color? color = null)
        {
            value = value ?? string.Empty;
            Program.ProgramModel.WriteLineLog(value.ToString(), color);
        }

        public static void WriteLine()
        {
            Program.ProgramModel.WriteLineLog();
        }

        public static readonly Settings Settings = Settings.Default;
    }
}
