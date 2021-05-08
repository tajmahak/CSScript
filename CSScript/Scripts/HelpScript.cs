using CSScript.Core;
using CSScript.Properties;
using System;

namespace CSScript.Scripts
{
    public class HelpScript : ScriptContainer
    {
        public HelpScript(IScriptContext context) : base(context) {
        }

        public override void Start() {
            string[] split = Resource.HelpText.Split('`');
            ConsoleColor consoleColor = Colors.Foreground;
            foreach (string fragment in split) {
                switch (fragment) {
                    case "": consoleColor = Colors.Foreground; break;
                    case "c": consoleColor = Colors.Caption; break;
                    case "i": consoleColor = Colors.Info; break;
                    case "//": consoleColor = ConsoleColor.Green; break; // комментарий
                    case "#": consoleColor = ConsoleColor.Yellow; break; // директива
                    default:
                        Context.Write(fragment, consoleColor); break;
                }
            }
            Context.WriteLine();
        }
    }
}
