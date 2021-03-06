﻿using CSScript;
using CSScript.Core;
using System;

namespace CSScriptStand
{
    internal class Program
    {
        // true  - выполнение кода без перехвата исключений
        // false - выполнение кода с помощью обработчика CSCript
        private static readonly bool UseSimpleExecutor = false;
        private static ConsoleScriptHandler handler;

        private static void Main(string[] args) {
            Console.CancelKeyPress += Console_CancelKeyPress;
            ConsoleScriptContext context = new ConsoleScriptContext {
                Args = args,
            };
            ScriptContainer stand = new Stand(context);

            if (UseSimpleExecutor) {
                stand.Start();

                if (context.Pause) {
                    context.WriteLine();
                    context.WriteLine("# Выполнено (" + context.ExitCode + ")",
                        context.ExitCode == 0 ? context.ColorScheme.Success : context.ColorScheme.Error);
                    Console.ReadKey();
                }

            } else {
                handler = new ConsoleScriptHandler(stand);
                handler.Start();
            }
        }

        private static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e) {
            handler?.Abort();
            e.Cancel = true;
        }
    }
}
