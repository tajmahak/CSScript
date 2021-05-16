using CSScript.Core;
using CSScript.Properties;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace CSScript
{
    public class ConsoleScriptHandler
    {
        private readonly ConsoleScriptContext context;
        private ScriptContainer container;
        private ScriptThread thread;

        public ConsoleScriptHandler(ConsoleScriptContext context) {
            this.context = context;
        }

        public ConsoleScriptHandler(ScriptContainer container) {
            this.container = container;
            context = (ConsoleScriptContext)container.Context;
        }

        public void Start() {
            try {
                WriteHeader();
                WriteStartInfo();

                if (container == null) {
                    Validate.IsNotBlank(context.ScriptPath, "Не указан путь к запускаемому скрипту.");

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
                        container = builder.CreateContainer(compilerResults.CompiledAssembly, context);

                    } else {
                        WriteCompileErrors(builder, compilerResults);
                        throw new Exception("Не удалось выполнить сборку скрипта.");
                    }

                    // Для использования в скрипте относительных путей к файлам
                    Environment.CurrentDirectory = Path.GetDirectoryName(Path.GetFullPath(context.ScriptPath));
                }

                thread = new ScriptThread(container);
                thread.Start();
                thread.Join();
                if (thread.ThreadException != null) {
                    throw thread.ThreadException;
                }

            } catch (Exception ex) {
                context.ExitCode = 1;
                context.WriteLine();
                WriteException(ex);

            } finally {
                Environment.ExitCode = context.ExitCode;

                context.KillManagedProcesses();

                if (thread != null && thread.Aborted) {
                    WriteAbort();
                } else {
                    if (!context.HiddenMode && context.Pause) {
                        ReadKeyForExit();
                    }
                }
            }
        }

        public void Abort() {
            thread?.Abort();
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
