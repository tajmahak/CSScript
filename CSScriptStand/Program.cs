using CSScript;
using CSScript.Core;
using System;
using System.Runtime.Remoting.Contexts;

namespace CSScriptStand
{
    public class Program
    {
        // true  - выполнение кода без перехвата исключений
        // false - выполнение кода с помощью обработчика CSCript
        private static readonly bool UseSimpleExecutor = true;
        private static ConsoleScriptHandler handler;

        private static Type defaultStand = typeof(Stand);

        [STAThread]
        private static void Main(string[] args) {
            ExecuteScriptContainer(defaultStand, args);
        }

        private static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e) {
            handler?.Abort();
            e.Cancel = true;
        }

        public static void ExecuteScriptContainer(Type containerType, string[] args) {
            Console.CancelKeyPress += Console_CancelKeyPress;
            ConsoleScriptContext context = new ConsoleScriptContext {
                Args = args,
                Pause = true,
            };
            ScriptContainer stand = (ScriptContainer)Activator.CreateInstance(containerType, context);

            if (UseSimpleExecutor) {
                stand.Start();

                if (context.Pause) {
                    context.WriteLine();
                    context.WriteLine("# Выполнено (" + context.ExitCode + ")",
                        context.ExitCode == 0 ? context.ColorScheme.Success : context.ColorScheme.Error);
                    Console.Read(); // при .ReadKey() не срабатывает комбинация Ctrl+C для остановки
                }

            }
            else {
                handler = new ConsoleScriptHandler(stand);
                handler.Start();
            }
        }
    }
}
