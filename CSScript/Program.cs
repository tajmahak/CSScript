using System;

namespace CSScript
{
    internal class Program
    {
        private static CSScriptHandler handler;

        private static void Main(string[] args) {
            Console.CancelKeyPress += Console_CancelKeyPress;

            handler = new CSScriptHandler();
            handler.Start(args);
        }

        private static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e) {
            handler?.Abort();
            e.Cancel = true;
        }
    }
}
