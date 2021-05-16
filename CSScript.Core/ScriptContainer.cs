namespace CSScript.Core
{
    /// <summary>
    /// Представляет оболочку для выполнения скомпилированного скрипта. 
    /// </summary>
    public abstract class ScriptContainer
    {
        public IScriptContext Context { get; }

        public ColorScheme Colors => Context.ColorScheme;

        public ScriptContainer(IScriptContext context) {
            Context = context;
        }

        public abstract void Start();
    }
}
