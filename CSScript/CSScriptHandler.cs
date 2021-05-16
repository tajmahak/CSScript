using CSScript.Core;
using CSScript.Properties;
using CSScript.Scripts;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace CSScript
{
    public class CSScriptHandler
    {
        private ConsoleScriptContext context;
        private ScriptThread scriptThread;

        public void Start(string[] args) {
          
        }

        public void Start(ScriptContainer container) {

            bool forcedPause = false;
            try {
                // Создание контекста программы для его передачи в исполняемый скрипт
                context = new ConsoleScriptContext {
                    ColorScheme = ColorScheme.Default,
                    Pause = true
                };

                InputArguments arguments = InputArguments.FromProgramArgs(args);
                forcedPause = arguments.ForcedPause;
                context.ScriptPath = arguments.ScriptPath;
                context.Args = arguments.ScriptArguments;
                context.Hidden = arguments.Hidden;

                if (context.Hidden) {
                    // Скрытие окна консоли во время исполнения программы
                    Native.ShowWindow(Native.GetConsoleWindow(), Native.SW_HIDE);
                }

                // При использовании в скрипте сторонних сборок, необходимо их разрешать вручную
                //AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;

                WriteHeader();
                WriteStartInfo();

                if (ScriptContainer == null) {
                    switch (arguments.Mode) {

                        case InputArguments.WorkMode.Script:
                            ScriptContainer = CreateScriptContainer(); break;

                        case InputArguments.WorkMode.Help:
                            ScriptContainer = new HelpScript(context); break;

                        case InputArguments.WorkMode.Register:
                            ScriptContainer = new RegistrationScript(context); break;

                        case InputArguments.WorkMode.Unregister:
                            ScriptContainer = new UnregistrationScript(context); break;

                        default: throw new Exception("Неподдерживаемый режим работы.");
                    }
                }

                if (context.ScriptPath != null) {
                    // Для использования в скрипте относительных путей к файлам
                    Environment.CurrentDirectory = Path.GetDirectoryName(Path.GetFullPath(context.ScriptPath));
                }

                scriptThread = new ScriptThread(ScriptContainer);
                scriptThread.Start();
                scriptThread.Join();
                if (scriptThread.ThreadException != null) {
                    throw scriptThread.ThreadException;
                }

            } catch (Exception ex) {
                context.ExitCode = 1;
                context.WriteLine();
                WriteException(ex);

            } finally {
                Environment.ExitCode = context.ExitCode;

                context.KillManagedProcesses();

                if (scriptThread != null && scriptThread.Aborted) {
                    WriteAbort();
                } else {
                    if (!context.Hidden && (context.Pause || forcedPause)) {
                        ReadKeyForExit();
                    }
                }
            }


        }

        public void Abort() {
            scriptThread?.Abort();
        }


        private ScriptContainer CreateScriptContainer() {

            ScriptParser parser = new ScriptParser {
                ScriptLibraryPath = Settings.Default.ScriptLibDirectory
            };
            foreach (string usingItem in ParseList(Settings.Default.Usings)) {
                parser.BaseUsings.Add(usingItem);
            }
            foreach (string importItem in ParseList(Settings.Default.Imports)) {
                parser.BaseImports.Add(importItem);
            }

            ScriptBuilder builder = parser.ParseFromFile(context.ScriptPath);
            CompilerResults compilerResults = builder.Compile();

            if (compilerResults.Errors.Count == 0) {
                return builder.CreateContainer(compilerResults.CompiledAssembly, context);

            } else {
                WriteCompileErrors(builder, compilerResults);
                throw new Exception("Не удалось выполнить сборку скрипта.");
            }
        }

        private string[] ParseList(string line) {
            return line.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        }

        private void WriteHeader() {
            Version version = Assembly.GetExecutingAssembly().GetName().Version;
            string versionText = $"{version.Major:0}.{version.Minor:00}";
            DateTime buildDate = new DateTime(2000, 1, 1).AddDays(version.Build).AddSeconds(version.Revision * 2);

            context.WriteLine($"## {DateTime.Now} (version {versionText} build {buildDate.ToShortDateString()})", context.ColorScheme.Info);
        }

        private void WriteStartInfo() {
            if (context.ScriptPath != null) {
                context.WriteLine($"## {context.ScriptPath}", context.ColorScheme.Info);
            }
            context.WriteLine();
        }

        private void WriteException(Exception ex) {
            context.WriteErrorLine($"# Ошибка ({ex.GetType().Name}): {ex.Message}");
            // StartTrace бесполезен, т.к. не отображает вложенность исключения внутри скрипта
            if (ex.InnerException != null) {
                context.WriteErrorLine();
                WriteException(ex.InnerException);
            }
        }

        private void WriteCompileErrors(ScriptBuilder builder, CompilerResults compilerResults) {
            int errorNumber = 1;
            foreach (CompilerError error in compilerResults.Errors) {
                if (error.Line > 0) {
                    context.WriteErrorLine($"# {errorNumber++} (cтрока {error.Line}): {error.ErrorText}");
                } else {
                    context.WriteErrorLine($"# {errorNumber++}: {error.ErrorText}");
                }
            }

            context.WriteLine();

            HashSet<int> errorLines = new HashSet<int>();
            foreach (CompilerError error in compilerResults.Errors) {
                errorLines.Add(error.Line);
            }
            string sourceCode = builder.GetSourceCode();
            string[] lines = sourceCode.Split(new string[] { "\r\n" }, StringSplitOptions.None);
            for (int i = 0; i < lines.Length; i++) {
                string line = lines[i];
                int lineNumber = i + 1;

                context.Write(lineNumber.ToString().PadLeft(4) + ": ", context.ColorScheme.Info);
                if (errorLines.Contains(lineNumber)) {
                    context.WriteLine(line, context.ColorScheme.Error);
                } else if (line.TrimStart().StartsWith("//")) {
                    context.WriteLine(line, ConsoleColor.Green);
                } else {
                    context.WriteLine(line);
                }
            }
        }

        private void WriteAbort() {
            context.WriteErrorLine($"# Прервано");
        }

        private void ReadKeyForExit() {
            Console.WriteLine();
            Console.Write("Для выхода нажмите любую клавишу...", context.ColorScheme.Info);
            Console.ReadKey();
        }
    }
}
