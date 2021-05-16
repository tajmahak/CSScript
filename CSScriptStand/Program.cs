using CSScript;
using CSScript.Core;
using System;

namespace CSScriptStand
{
    internal class Program
    {
        // true  - выполнение кода без перехвата исключений
        // false - выполнение кода с помощью обработчика CSCript
        private static readonly bool UseSimpleExecutor = true;

        // Запускаемый проект типа ScriptContainer
        private static Type RunningContainer = typeof(Stand);


        private static void Main(string[] args) {
            ConsoleScriptContext context = new ConsoleScriptContext {
                ColorScheme = ColorScheme.Default,
                Args = args,
                Pause = true
            };

            ScriptContainer stand = (ScriptContainer)Activator.CreateInstance(RunningContainer, context);

            if (UseSimpleExecutor) {
                stand.Start();

                //context.WriteLine();
                //context.WriteLine("# Выполнено (" + context.ExitCode + ")",
                //    context.ExitCode == 0 ? context.ColorScheme.Success : context.ColorScheme.Error);
                //Console.ReadKey();

            } else {
                CSScriptHandler handler = new CSScriptHandler();
                handler.Start(stand);
            }
        }
    }
}
