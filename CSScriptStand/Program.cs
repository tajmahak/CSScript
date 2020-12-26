using SScript.Core;
using System;
using System.Diagnostics;

namespace CSScriptStand
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            ScriptHandler scriptHandler = new ScriptHandler();
            scriptHandler.Messages.MessageAdded += Messages_MessageAdded;

            scriptHandler.Messages.WriteLine("123");
            Console.Read();
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
