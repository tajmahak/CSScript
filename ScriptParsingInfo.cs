using System.Collections.Generic;
using System.Text;

namespace CSScript
{
    /// <summary>
    /// Данные скрипта после парсинга
    /// </summary>
    internal class ScriptParsingInfo
    {
        public string ScriptPath { get; set; }
        public List<string> DefinedAssemblyList { get; set; } = new List<string>();
        public List<string> UsingList { get; set; } = new List<string>();
        public StringBuilder ProcedureBlock { get; set; } = new StringBuilder();
        public StringBuilder ClassBlock { get; set; } = new StringBuilder();
        public StringBuilder NamespaceBlock { get; set; } = new StringBuilder();
    }
}
