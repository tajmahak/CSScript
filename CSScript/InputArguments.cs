using System;
using System.Collections.Generic;

namespace CSScript.Core
{
    /// <summary>
    /// Представляет структурированные аргументы командной строки.
    /// </summary>
    internal class InputArguments
    {
        public WorkMode Mode { get; set; } = WorkMode.Help;
        public bool Hidden { get; set; }
        public string ScriptPath { get; set; }
        public string[] ScriptArguments { get; private set; } = new string[0];

        public static InputArguments FromProgramArgs(string[] args) {
            InputArguments inputArguments = new InputArguments();
            if (args.Length > 0) {

                bool scriptArgsBlock = false;
                List<string> scriptArgs = new List<string>();
                for (int i = 0; i < args.Length; i++) {
                    string arg = args[i];

                    if (!scriptArgsBlock && arg == "-reg") {
                        inputArguments.Mode = WorkMode.Register;

                    } else if (!scriptArgsBlock && arg == "-unreg") {
                        inputArguments.Mode = WorkMode.Unregister;

                    } else if (!scriptArgsBlock && arg == "-h") {
                        inputArguments.Hidden = true;

                    } else if (!scriptArgsBlock && arg == "-a") {
                        inputArguments.Mode = WorkMode.Script;
                        scriptArgsBlock = true;

                    } else if (!scriptArgsBlock && !arg.StartsWith("-")) {
                        inputArguments.Mode = WorkMode.Script;
                        inputArguments.ScriptPath = arg;
                    } else if (scriptArgsBlock) {
                        scriptArgs.Add(arg);
                    } else {
                        throw new Exception("Строка аргументов не распознана: \"" + string.Join("\" \"", args) + "\"");
                    }
                }
                inputArguments.ScriptArguments = scriptArgs.ToArray();
            }
            return inputArguments;
        }

        public enum WorkMode
        {
            Help,
            Script,
            Register,
            Unregister,
        }
    }
}
