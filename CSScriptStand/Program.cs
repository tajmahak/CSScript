using CSScript.Core;
using CSScriptStand.Properties;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;

namespace CSScriptStand
{
    internal class Program
    {
        private static readonly ColorScheme ColorScheme = ColorScheme.Default;

        private static void Main(string[] args)
        {
            ScriptEnvironment scriptEnvironment = null;
            try {
                InputArguments arguments = InputArguments.FromProgramArgs(args);
                if (arguments.IsEmpty) {
                    WriteHelpInfo();
                    Console.ReadKey();
                }
                else {
                    scriptEnvironment = new ScriptEnvironment(arguments.ScriptPath, arguments.ScriptArguments.ToArray());
                    scriptEnvironment.MessageAdded += (sender, message) => Write(message.Text, message.ForeColor);

                    WriteStartInfo(arguments.ScriptPath);

                    ScriptContent scriptContent = ScriptManager.CreateScriptContent(arguments.ScriptPath);
                    CompilerResults compiledScript = ScriptManager.CompileScript(scriptContent);
                    if (compiledScript.Errors.Count > 0) {
                        scriptEnvironment.ExitCode = 1;
                        WriteCompileErrors(compiledScript);
                        WriteLine();
                        WriteSourceCode(ScriptManager.CreateSourceCode(scriptContent), compiledScript);
                    }
                    else {
                        ScriptContainer scriptContainer = ScriptManager.CreateScriptContainer(compiledScript, scriptEnvironment);
                        scriptContainer.Execute();
                    }
                }
            }
            catch (Exception ex) {
                if (scriptEnvironment != null) {
                    scriptEnvironment.ExitCode = 1;
                }
                WriteException(ex);
            }
            finally {
                if (scriptEnvironment != null) {
                    WriteExitCode(scriptEnvironment.ExitCode);

                    if (!scriptEnvironment.AutoClose) {
                        Console.ReadKey();
                    }
                }
            }
        }



        private static void Write(string text, ConsoleColor consoleColor)
        {
            Debug.Write(text);

            ConsoleColor stockColor = Console.ForegroundColor;
            Console.ForegroundColor = consoleColor;
            Console.Write(text);
            Console.ForegroundColor = stockColor;
        }

        private static void WriteLine(string text, ConsoleColor consoleColor)
        {
            Write(text + Environment.NewLine, consoleColor);
        }

        private static void WriteLine()
        {
            Debug.WriteLine(null);
            Console.WriteLine();
        }


        public static void WriteHelpInfo()
        {
            string[] lines = Resource.HelpText.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
            for (int i = 0; i < lines.Length; i++) {
                ConsoleColor color = ColorScheme.Fore;
                string[] line = ParseHelpInfoLine(lines[i]);
                switch (line[0]) {
                    case "c": color = ColorScheme.Caption; break;
                    case "i": color = ColorScheme.Info; break;
                }
                WriteLine(line[1], color);
            }
        }

        public static void WriteStartInfo(string scriptPath)
        {
            WriteLine($"## {scriptPath}", ColorScheme.Info);
            WriteLine($"## {DateTime.Now}", ColorScheme.Info);
            WriteLine();
        }

        public static void WriteException(Exception ex)
        {
            WriteLine($"# Ошибка: {ex.Message}", ColorScheme.Error);
            foreach (string stackTraceLine in ex.StackTrace.Split(new string[] { Environment.NewLine }, StringSplitOptions.None)) {
                WriteLine("#" + stackTraceLine, ColorScheme.StackTrace);
            }

            if (ex.InnerException != null) {
                WriteLine();
                WriteException(ex.InnerException);
            }
        }

        public static void WriteCompileErrors(CompilerResults compilerResults)
        {
            WriteLine($"# Ошибок компиляции: {compilerResults.Errors.Count}", ColorScheme.Error);
            int errorNumber = 1;
            foreach (CompilerError error in compilerResults.Errors) {
                if (error.Line > 0) {
                    WriteLine($"# {errorNumber++} (cтрока {error.Line}): {error.ErrorText}", ColorScheme.Error);
                }
                else {
                    WriteLine($"# {errorNumber++}: {error.ErrorText}", ColorScheme.Error);
                }
            }
        }

        public static void WriteSourceCode(string sourceCode, CompilerResults compiledScript)
        {
            HashSet<int> errorLines = new HashSet<int>();
            foreach (CompilerError error in compiledScript.Errors) {
                errorLines.Add(error.Line);
            }

            string[] lines = sourceCode.Split(new string[] { "\r\n" }, StringSplitOptions.None);
            for (int i = 0; i < lines.Length; i++) {
                string line = lines[i];
                int lineNumber = i + 1;

                Write(lineNumber.ToString().PadLeft(4) + ": ", ColorScheme.Fore);
                if (errorLines.Contains(lineNumber)) {
                    WriteLine(line, ColorScheme.Error);
                }
                else {
                    if (line.TrimStart().StartsWith("//")) {
                        WriteLine(line, ColorScheme.Comment);
                    }
                    else {
                        WriteLine(line, ColorScheme.SourceCode);
                    }
                }
            }
        }

        public static void WriteExitCode(int exitCode)
        {
            WriteLine();
            WriteLine($"# Выполнено с кодом возврата: {exitCode}", ColorScheme.Info);
        }


        private static string[] ParseHelpInfoLine(string line)
        {
            if (line.StartsWith("`")) {
                int index = line.IndexOf("`", 1);
                return new string[]
                {
                    line.Substring(1, index - 1),
                    line.Substring(index + 1),
                };
            }
            else {
                return new string[] { null, line };
            }
        }
    }
}
