using CSScript.Core;
using CSScript.Scripts;
using System;

namespace CSScript
{
    internal class Program
    {
        private static ConsoleScriptHandler handler;

        private static void Main(string[] args) {
            Console.CancelKeyPress += Console_CancelKeyPress;

            InputArguments arguments = InputArguments.FromProgramArgs(args);

            ConsoleScriptContext context = new ConsoleScriptContext {
                ScriptPath = arguments.ScriptPath,
                Args = arguments.ScriptArguments,
                HiddenMode = arguments.Hidden,
                Pause = true,
            };

            if (arguments.Hidden) {
                // Скрытие окна консоли во время исполнения программы
                Native.ShowWindow(Native.GetConsoleWindow(), Native.SW_HIDE);
            }

            switch (arguments.Mode) {

                case InputArguments.WorkMode.Script:
                    handler = new ConsoleScriptHandler(context); break;

                case InputArguments.WorkMode.Help:
                    handler = new ConsoleScriptHandler(new HelpScript(context)); break;

                case InputArguments.WorkMode.Register:
                    handler = new ConsoleScriptHandler(new RegistrationScript(context)); break;

                case InputArguments.WorkMode.Unregister:
                    handler = new ConsoleScriptHandler(new UnregistrationScript(context)); break;

                default: throw new Exception("Неподдерживаемый режим работы.");
            }
            handler.Start();
        }

        private static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e) {
            handler?.Abort();
            e.Cancel = true;
        }
    }
}
