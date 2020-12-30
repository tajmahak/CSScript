using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CSScript.Core
{
    /// <summary>
    /// Представляет структурированную информацию о скрипте.
    /// </summary>
    public class ScriptInfo
    {
        public string ScriptPath { get; }
        public List<string> ImportList { get; private set; } = new List<string>();
        public List<string> UsingList { get; private set; } = new List<string>();
        public StringBuilder ProcedureBlock { get; private set; } = new StringBuilder();
        public StringBuilder ClassBlock { get; private set; } = new StringBuilder();
        public StringBuilder NamespaceBlock { get; private set; } = new StringBuilder();


        public ScriptInfo(string scriptPath) {
            ScriptPath = scriptPath;
        }

        public static ScriptInfo FromFile(string scriptPath, string text) {
            return new ScriptInfo(scriptPath).LoadFromText(text);
        }

        private ScriptInfo LoadFromText(string text) {
            StringBuilder currentBlock = ProcedureBlock;

            string[] textLines = text.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string textLine in textLines) {
                ScriptLine scriptLine = ScriptLine.Parse(textLine);
                if (!scriptLine.IsEmpty) {
                    switch (scriptLine.OperatorName) {
                        case "import":
                            ImportList.Add(scriptLine.OperatorValue);
                            break;

                        case "using":
                            UsingList.Add(scriptLine.OperatorValue);
                            break;

                        case "class":
                            currentBlock = ClassBlock;
                            break;

                        case "ns":
                        case "namespace":
                            currentBlock = NamespaceBlock;
                            break;

                        default:
                            if (currentBlock.Length > 0) {
                                currentBlock.AppendLine();
                            }
                            currentBlock.Append(scriptLine.SourceLine);
                            break;
                    }
                }
            }
            return this;
        }


        /// <summary>
        /// Представляет строку скрипта.
        /// </summary>
        private class ScriptLine
        {
            public string SourceLine { get; }
            public bool IsEmpty { get; private set; }
            public string OperatorName { get; private set; }
            public string OperatorValue { get; private set; }


            private ScriptLine(string sourceLine) {
                SourceLine = sourceLine;
            }

            public static ScriptLine Parse(string line) {
                ScriptLine scriptLine = new ScriptLine(line);
                string trimLine = line.TrimStart();
                if (trimLine.Length == 0) {
                    scriptLine.IsEmpty = true;
                } else if (trimLine.StartsWith("#")) {
                    int index = trimLine.IndexOf(" ");
                    if (index != -1) {
                        scriptLine.OperatorName = trimLine.Substring(1, index - 1).ToLower();
                        scriptLine.OperatorValue = trimLine.Substring(index + 1).TrimEnd(';');
                    } else {
                        scriptLine.OperatorName = trimLine.Substring(1).ToLower();
                    }
                }
                return scriptLine;
            }
        }
    }
}
