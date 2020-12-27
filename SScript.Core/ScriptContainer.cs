namespace CSScript.Core
{
    /// <summary>
    /// Представляет оболочку для выполнения скомпилированного скрипта. 
    /// </summary>
    public abstract class ScriptContainer
    {
        public IScriptContext env { get; }
        public ColorScheme colors => env.ColorScheme;

        public ScriptContainer(IScriptContext env) {
            this.env = env;
        }

        public abstract void Execute();
    }
}
