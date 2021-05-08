using CSScript;
using CSScript.Core;
using System;

namespace CSScriptStand
{
    internal class Program
    {
        private static void Main(string[] args) {
            ConsoleContext context = new ConsoleContext {
                ColorScheme = ColorScheme.Default,
                Args = args,
                Pause = true
            };

            ScriptContainer scriptContainer = new Stand(context);
            scriptContainer.Start();

            context.WriteLine();
            context.WriteLine("# Выполнено (" + context.ExitCode + ")",
                context.ExitCode == 0 ? scriptContainer.Colors.Success : scriptContainer.Colors.Error);
            Console.ReadKey();
        }
    }
}
