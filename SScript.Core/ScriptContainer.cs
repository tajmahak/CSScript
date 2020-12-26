using CSScript.Core;

namespace CSScript.Core
{
    /// <summary>
    /// Представляет оболочку для выполнения скомпилированного скрипта в программе. 
    /// </summary>
    public abstract class ScriptContainer
    {
        public IScriptEnvironment env { get; }
        public MessageColorScheme colors { get; }

        public ScriptContainer(IScriptEnvironment env, MessageColorScheme colors)
        {
            this.env = env;
            this.colors = colors;
        }

        public abstract void Execute();
    }
}
