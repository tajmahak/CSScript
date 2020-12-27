namespace CSScript.Core
{
    /// <summary>
    /// Представляет оболочку для выполнения скомпилированного скрипта. 
    /// </summary>
    public abstract class ScriptContainer
    {
        public IScriptContext context { get; }
        public ColorScheme colors => context.ColorScheme;

        public ScriptContainer(IScriptContext context) {
            this.context = context;
        }

        public abstract void Execute();
    }
}
