using CSScript.Core;
using System;
using System.Diagnostics;

namespace CSScriptStand
{
    internal class Program
    {
        private static void Main(string[] args) {
            ScriptContext scriptEnvironment = new ScriptContext(null, args);
            scriptEnvironment.OutputLogFragmentAdded += (sender, message) => Write(message.Text, message.Color);
            scriptEnvironment.ErrorLogFragmentAdded += (sender, message) => WriteError(message.Text, message.Color);
            scriptEnvironment.ReadLineRequred += (sender, color) => {
                Console.ForegroundColor = color;
                return Console.ReadLine();
            };

            ScriptContainer scriptContainer = new Stand(scriptEnvironment);
            scriptContainer.Start();

            scriptEnvironment.WriteLine();
            scriptEnvironment.WriteLine("# Выполнено (" + scriptEnvironment.ExitCode + ")",
                scriptEnvironment.ExitCode == 0 ? scriptContainer.Colors.Success : scriptContainer.Colors.Error);
            Console.ReadKey();
        }

        private static void Write(string text, ConsoleColor color) {
            Debug.Write(text);
            Console.ForegroundColor = color;
            Console.Out.Write(text);
        }

        private static void WriteError(string text, ConsoleColor color) {
            Debug.Write(text);
            Console.ForegroundColor = color;
            Console.Error.Write(text);
        }
    }
}
