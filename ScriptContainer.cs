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
            env = environment;
        }

        public abstract void Execute();
    }
}
