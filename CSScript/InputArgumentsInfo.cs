using System.Collections.Generic;

namespace CSScript
{
    /// <summary>
    /// Представляет структурированные аргументы командной строки.
    /// </summary>
    internal class InputArgumentsInfo
    {
        public static InputArgumentsInfo FromProgramArgs(string[] args)
        {
            InputArgumentsInfo inputArguments = new InputArgumentsInfo();

            if (args.Length == 0) {
                inputArguments.IsEmpty = true;
            }
            else {
                string currentArgument = null;
                for (int i = 0; i < args.Length; i++) {
                    string arg = args[i];
                    string preparedArg = arg.Trim().ToLower();
                    if (preparedArg == "/h" || preparedArg == "/hide") {
                        inputArguments.HideMode = true;
                        currentArgument = null;
                    }
                    else if (preparedArg == "/l" || preparedArg == "/log") {
                        inputArguments.LogPath = args[++i];
                        currentArgument = null;
                    }
                    else if (preparedArg == "/reg") {
                        inputArguments.RegisterMode = true;
                        currentArgument = null;
                    }
                    else if (preparedArg == "/unreg") {
                        inputArguments.UnregisterMode = true;
                        currentArgument = null;
                    }
                    else if (preparedArg == "/a" || preparedArg == "/arg") {
                        currentArgument = "a";
                    }
                    else {
                        if (currentArgument == null && inputArguments.ScriptPath == null) {
                            inputArguments.ScriptPath = arg;
                        }
                        else if (currentArgument == "a") {
                            inputArguments.ScriptArguments.Add(arg);
                        }
                    }
                }
            }

            return inputArguments;
        }


        public bool IsEmpty { get; set; }

        public bool HideMode { get; set; }

        public bool RegisterMode { get; set; }

        public bool UnregisterMode { get; set; }

        public string LogPath { get; set; }

        public string ScriptPath { get; set; }

        public List<string> ScriptArguments { get; private set; } = new List<string>();
    }
}
