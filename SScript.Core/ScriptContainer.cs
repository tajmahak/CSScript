namespace CSScript.Core
{
    /// <summary>
    /// Представляет оболочку для выполнения скомпилированного скрипта в программе. 
    /// </summary>
    public abstract class ScriptContainer
    {
        public IScriptEnvironment env { get; }
        public ColorScheme colors => env.ColorScheme;

        public ScriptContainer(IScriptEnvironment env)
        {
            this.env = env;
        }

        public abstract void Execute();
    }
}
