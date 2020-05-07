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
        protected readonly IScriptEnvironment env;

        public ScriptContainer(IScriptEnvironment environment)
        {
            this.env = environment;
        }

        public abstract void Execute(string[] args);
    }
}
