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

            ScriptContainer stand = new Stand(context);
            stand.Start();

            context.WriteLine();
            context.WriteLine("# Выполнено (" + context.ExitCode + ")",
                context.ExitCode == 0 ? context.ColorScheme.Success : context.ColorScheme.Error);
            Console.ReadKey();
        }
    }
}
