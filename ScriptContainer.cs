using CSScript.Properties;
using System.Diagnostics;
using System.Drawing;

namespace CSScript
{
    /// <summary>
    /// Представляет оболочку для выполнения скомпилированного скрипта в программе. 
    /// </summary>
    public abstract class ScriptContainer
    {
        public ScriptContainer(IScriptEnvironment environment)
        {
            this.environment = environment;
        }

      
        /// <summary>
        /// Запуск выполнения скрипта
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        public abstract void StartScript(string[] args);

        
        // Сущности для использовании в коде скрипта:

        public int ExitCode { get; protected set; }

        public bool GUIForceExit { get; protected set; }

        protected string ScriptPath => environment.ScriptPath;

        protected Settings Settings => environment.Settings;

        protected void WriteMessage(object value, Color? color = null) => environment.WriteMessage(value, color);

        protected void WriteMessageLine(object value, Color? color = null) => environment.WriteMessageLine(value, color);

        protected void WriteMessageLine() => environment.WriteMessageLine();

        protected Process CreateManagedProcess() => environment.CreateManagedProcess();

        
        private readonly IScriptEnvironment environment;
    }
}
