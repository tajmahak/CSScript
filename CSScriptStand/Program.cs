using CSScript.Core;
using System;
using System.Diagnostics;

namespace CSScriptStand
{
    internal class Program
    {
        private static void Main(string[] args) {
            ScriptContext scriptEnvironment = new ScriptContext(null, args);
            scriptEnvironment.MessageAdded += (sender, message) => Write(message.Text, message.ForeColor);
            scriptEnvironment.InputTextRequred += (sender, foreColor) => {
                Console.ForegroundColor = foreColor;
                return Console.ReadLine();
            };

            ScriptContainer scriptContainer = new Stand(scriptEnvironment);
            scriptContainer.Execute();
            Console.ReadKey();
        }

        private static void Write(string text, ConsoleColor consoleColor) {
            Debug.Write(text);
            Console.ForegroundColor = consoleColor;
            Console.Write(text);
        }
    }
}
