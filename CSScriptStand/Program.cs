using CSScript.Core;
using System;
using System.CodeDom.Compiler;
using System.Diagnostics;

namespace CSScriptStand
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            string scriptPath = @"test.txt";

            ScriptEnvironment scriptEnvironment = new ScriptEnvironment(scriptPath, null);
            scriptEnvironment.MessageAdded += ScriptEnvironment_MessageAdded;

            CompilerResults compiledScript = ScriptCompiler.CompileScript(scriptPath);
            ScriptContainer scriptContainer = ScriptCompiler.CreateScriptContainer(compiledScript, scriptEnvironment);
            scriptContainer.Execute();

            Console.ReadKey();

            { }

            //IScriptEnvironment env = scriptHandler.CreateScriptEnvironment(null, null);
            //MessageColorScheme colors = MessageColorScheme.Default;
            //ScriptContainer scriptContainer = new Stand(env, colors);
            //scriptHandler.Execute(scriptContainer, true);

            //System.CodeDom.Compiler.CompilerResults compileScript = scriptHandler.CompileScript(@"test.txt");
            //IScriptEnvironment env = scriptHandler.CreateScriptEnvironment(@"test.txt", null);
            //ScriptContainer scriptContainer = scriptHandler.CreateScriptContainer(compileScript, env);
            //scriptContainer.Execute();
            //Console.ReadKey();
        }

        private static void ScriptEnvironment_MessageAdded(object sender, Message message)
        {
            Debug.Write(message.Text);

            ConsoleColor stockColor = Console.ForegroundColor;
            Console.ForegroundColor = message.ForeColor;
            Console.Write(message.Text);
            Console.ForegroundColor = stockColor;
        }
    }
}
