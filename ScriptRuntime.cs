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
        public abstract int _Main(string arg);

        protected void _Write(object value, Color? color = null)
        {
            Program.OutputLog.Add(value.ToString(), color);
        }

        protected void _WriteLine(object value, Color? color = null)
        {
            Program.OutputLog.AddLine(value.ToString(), color);
        }

        protected void _WriteLine()
        {
            Program.OutputLog.AddLine();
        }
    }
}
