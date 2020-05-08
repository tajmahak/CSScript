using System.Collections.Generic;

namespace CSScript
{
    /// <summary>
    /// Представляет структурированные аргументы командной строки.
    /// </summary>
    internal class InputArgumentsInfo
    {
        private InputArgumentsInfo() { }

        public bool IsEmpty { get; private set; }

        public bool HideMode { get; private set; }

        public string LogPath { get; private set; }

        public string ScriptPath { get; private set; }

        public bool UseDebugStand { get; private set; }

        public List<string> ScriptArguments { get; private set; } = new List<string>();

        public static InputArgumentsInfo Parse(string[] args)
        {
            InputArgumentsInfo inputArguments = new InputArgumentsInfo();

            if (args.Length == 0)
            {
                inputArguments.IsEmpty = true;
            }
            else
            {
                string currentArgument = null;
                for (int i = 0; i < args.Length; i++)
                {
                    string arg = args[i];
                    string preparedArg = arg.Trim().ToLower();
                    if (preparedArg == "/h" || preparedArg == "/hide")
                    {
                        inputArguments.HideMode = true;
                        currentArgument = null;
                    }
                    else if (preparedArg == "/l" || preparedArg == "/log")
                    {
                        inputArguments.LogPath = args[++i];
                        currentArgument = null;
                    }
                    else if (preparedArg == "/a" || preparedArg == "/arg")
                    {
                        currentArgument = "a";
                    }
                    else if (preparedArg == "/debugstand")
                    {
                        inputArguments.UseDebugStand = true;
                        currentArgument = null;
                    }
                    else
                    {
                        if (currentArgument == null && inputArguments.ScriptPath == null)
                        {
                            inputArguments.ScriptPath = arg;
                        }
                        else if (currentArgument == "a")
                        {
                            inputArguments.ScriptArguments.Add(arg);
                        }
                    }
                }
            }

            return inputArguments;
        }
    }
}
