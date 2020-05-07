using System.Collections.Generic;
using System.Text;

namespace CSScript
{
    /// <summary>
    /// Представляет структурированную информацию о скрипте.
    /// </summary>
    internal class ScriptInfo
    {
        public ScriptInfo(string scriptPath)
        {
            ScriptPath = scriptPath;
        }


        public string ScriptPath { get; }

        public List<string> DefinedScriptList { get; private set; } = new List<string>();

        public List<string> DefinedAssemblyList { get; private set; } = new List<string>();

        public List<string> UsingList { get; private set; } = new List<string>();

        public StringBuilder ProcedureBlock { get; private set; } = new StringBuilder();

        public StringBuilder ClassBlock { get; private set; } = new StringBuilder();

        public StringBuilder NamespaceBlock { get; private set; } = new StringBuilder();
    }
}
