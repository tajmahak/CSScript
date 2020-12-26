using CSScript.Core;
using System;
using System.Diagnostics;
using System.IO;

namespace CSScriptStand
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            ScriptHandler scriptHandler = new ScriptHandler();
            scriptHandler.Messages.MessageAdded += Messages_MessageAdded;
            scriptHandler.ScriptFinished += ScriptHandler_ScriptFinished;

            //IScriptEnvironment env = scriptHandler.CreateScriptEnvironment(null, null);
            //MessageColorScheme colors = MessageColorScheme.Default;
            //ScriptContainer scriptContainer = new Stand(env, colors);
            //scriptHandler.Execute(scriptContainer, true);

            var a = scriptHandler.CompileScript(@"D:\Хранилище\Разработка\Проекты\Scripts\Резервное копирование проектов.cssc");
        }

        private static void ScriptHandler_ScriptFinished(ScriptContainer scriptContainer, bool success)
        {
            if (!scriptContainer.env.AutoClose) {
                Console.Read();
            }
        }

        private static void Messages_MessageAdded(object sender, Message message)
        {
            Debug.Write(message.Text);

            ConsoleColor stockColor = Console.ForegroundColor;
            Console.ForegroundColor = message.ForeColor;
            Console.Write(message.Text);
            Console.ForegroundColor = stockColor;
        }
    }
}
