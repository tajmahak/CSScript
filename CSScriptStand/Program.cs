using CSScript.Core;
using System;
using System.Diagnostics;

namespace CSScriptStand
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            ScriptEnvironment scriptEnvironment = new ScriptEnvironment(null, args);
            scriptEnvironment.MessageAdded += (sender, message) => Write(message.Text, message.ForeColor);
            scriptEnvironment.InputTextRequred += (sender) => Console.ReadLine();

            ScriptContainer scriptContainer = new Stand(scriptEnvironment);
            scriptContainer.Execute();
            Console.ReadKey();
        }

        private static void Write(string text, ConsoleColor consoleColor)
        {
            Debug.Write(text);

            ConsoleColor stockColor = Console.ForegroundColor;
            Console.ForegroundColor = consoleColor;
            Console.Write(text);
            Console.ForegroundColor = stockColor;
        }
    }
}
