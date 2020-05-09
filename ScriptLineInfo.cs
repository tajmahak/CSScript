namespace CSScript
{
    internal class ScriptLineInfo
    {
        private ScriptLineInfo(string sourceLine)
        {
            SourceLine = sourceLine;
        }

        public string SourceLine { get; }

        public bool IsEmpty { get; private set; }

        public string OperatorName { get; private set; }

        public string OperatorValue { get; private set; }

        public static ScriptLineInfo Parse(string line)
        {
            ScriptLineInfo scriptLine = new ScriptLineInfo(line);
            string trimLine = line.TrimStart();
            if (trimLine.Length == 0)
            {
                scriptLine.IsEmpty = true;
            }
            else if (trimLine.StartsWith("#"))
            {
                int index = trimLine.IndexOf(" ");
                if (index != -1)
                {
                    scriptLine.OperatorName = trimLine.Substring(1, index - 1).ToLower();
                    scriptLine.OperatorValue = trimLine.Substring(index + 1).TrimEnd(';');
                }
                else
                {
                    scriptLine.OperatorName = trimLine.Substring(1).ToLower();
                }
            }
            return scriptLine;
        }
    }
}
