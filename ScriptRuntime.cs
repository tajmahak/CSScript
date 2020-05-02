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
        /// Метод для начала выполнения скрипта
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        public abstract int RunScript(string arg);

        // Сущности, используемые в скрипте (public):

        public static void Write(object value, Color? color = null)
        {
            Program.OutputLog.Write(value.ToString(), color);
        }

        public static void WriteLine(object value, Color? color = null)
        {
            Program.OutputLog.WriteLine(value.ToString(), color);
        }

        public static void WriteLine()
        {
            Program.OutputLog.WriteLine();
        }

        public static readonly Settings Settings  = Settings.Default;
    }
}
