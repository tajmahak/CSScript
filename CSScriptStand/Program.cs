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
            if (UseSimpleExecutor) {
                ConsoleContext context = new ConsoleContext {
                    ColorScheme = ColorScheme.Default,
                    Args = args,
                    Pause = true
                };

                ScriptContainer stand = (ScriptContainer)Activator.CreateInstance(RunningContainer, context);
                stand.Start();

                context.WriteLine();
                context.WriteLine("# Выполнено (" + context.ExitCode + ")",
                    context.ExitCode == 0 ? context.ColorScheme.Success : context.ColorScheme.Error);
                Console.ReadKey();

            } else {
                ProgramHandler handler = new ProgramHandler();
                handler.GetScriptContainerEvent += (context) => new Stand(context);
                handler.Start(args);
            }
        }
    }
}
