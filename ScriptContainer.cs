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
        private readonly IScriptEnvironment environment;
       
        public ScriptContainer(IScriptEnvironment environment)
        {
            this.environment = environment;
        }

        public abstract void Execute(string[] args);
       
       
        // Сущности для использовании в коде скрипта:
       
        public int ExitCode { get; protected set; }

        public bool GUIForceExit { get; protected set; }

        protected string ScriptPath => environment.ScriptPath;

        protected Settings Settings => environment.Settings;

        protected void WriteMessage(object value, Color? foreColor = null) => environment.WriteMessage(value, foreColor);

        protected void WriteMessageLine(object value, Color? foreColor = null) => environment.WriteMessageLine(value, foreColor);

        protected void WriteMessageLine() => environment.WriteMessageLine();

        protected Process CreateManagedProcess() => environment.CreateManagedProcess();
    }
}
