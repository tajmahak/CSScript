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
        public List<string> DefinedAssemblyList { get; set; }
        public List<string> UsingList { get; set; }
        public StringBuilder ProcedureBlock { get; set; }
        public StringBuilder ClassBlock { get; set; }
        public StringBuilder NamespaceBlock { get; set; }
    }
}
