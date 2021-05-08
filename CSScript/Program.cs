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
    internal class Program
    {
        private Dictionary<string, Assembly> importedAssemblies;
        private ConsoleContext context;
        private ScriptExecutionContext executionContext;

        private static void Main(string[] args) {
            new Program().Start(args);
        }

        private void Start(string[] args) {
            bool forcedPause = false;
            try {
                // Создание контекста программы для его передачи в исполняемый скрипт
                context = new ConsoleContext {
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

                // При запуске рабочая папка должна быть папкой с программой, для разрешения путей к импортируемым файлам.
                Environment.CurrentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

                // Для остановки скрипта комбинацией Ctrl+C
                Console.CancelKeyPress += Console_CancelKeyPress;

                // При использовании в скрипте сторонних сборок, необходимо их разрешать вручную
                AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;

                WriteHeader();
                WriteStartInfo();

                ScriptContainer script = null;
                switch (arguments.Mode) {

                    case InputArguments.WorkMode.Help:
                        script = new HelpScript(context);
                        break;

                    case InputArguments.WorkMode.Script:
                        script = CreateScriptContainer();
                        break;

                    case InputArguments.WorkMode.Register:
                        script = new RegistrationScript(context);
                        break;

                    case InputArguments.WorkMode.Unregister:
                        script = new UnregistrationScript(context);
                        break;

                    default: throw new Exception("Неподдерживаемый режим работы.");
                }

                if (context.ScriptPath != null) {
                    // Для использования в скрипте относительных путей к файлам
                    Environment.CurrentDirectory = Path.GetDirectoryName(Path.GetFullPath(context.ScriptPath));
                }

                executionContext = new ScriptExecutionContext(script);
                executionContext.Start();
                executionContext.Join();
                if (executionContext.ThreadException != null) {
                    throw executionContext.ThreadException;
                }

            } catch (Exception ex) {
                context.ExitCode = 1;
                WriteException(ex);

            } finally {
                context.KillManagedProcesses();

                context.WriteLine();
                if (executionContext != null && executionContext.Aborted) {
                    WriteAbort();
                } else {
                    WriteExitCode();
                }

                Environment.ExitCode = context.ExitCode;

                if (!context.Hidden && (context.Pause || forcedPause)) {
                    ReadKeyForExit();
                }
            }
        }

        private ScriptContainer CreateScriptContainer() {
            ScriptInfo scriptInfo = ScriptUtils.CreateScriptInfo(context.ScriptPath, ResolveImportFilePath);
            CompilerResults compiledScript = ScriptUtils.CompileScript(scriptInfo);
            if (compiledScript.Errors.Count == 0) {
                importedAssemblies = ScriptUtils.GetImportedAssemblies(scriptInfo);
                return ScriptUtils.CreateScriptContainer(compiledScript, context);

            } else {
                WriteCompileErrors(compiledScript);
                context.WriteLine();
                WriteSourceCode(ScriptUtils.CreateSourceCode(scriptInfo), compiledScript);
                throw new Exception("Не удалось выполнить сборку скрипта.");
            }
        }


        private void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e) {
            executionContext?.Abort();
            e.Cancel = true;
        }

        private Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args) {
            if (importedAssemblies != null) {
                importedAssemblies.TryGetValue(args.Name, out Assembly assembly);
                return assembly;
            }
            return null;
        }

        private string ResolveImportFilePath(string importFilePath, string workingDirectory) {
            return Path.Combine(Settings.Default.ScriptLibDirectory, importFilePath);
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
                context.WriteLine();
            }
        }

        private void WriteException(Exception ex) {
            context.WriteLine($"# Ошибка: {ex.Message}", context.ColorScheme.Error);
            context.WriteLine($"# {ex.GetType().Name}:", ConsoleColor.Gray);
            foreach (string stackTraceLine in ex.StackTrace.Split(new string[] { Environment.NewLine }, StringSplitOptions.None)) {
                context.WriteLine("#" + stackTraceLine, ConsoleColor.Gray);
            }

            if (ex.InnerException != null) {
                context.WriteLine();
                WriteException(ex.InnerException);
            }
        }

        private void WriteCompileErrors(CompilerResults compilerResults) {
            context.WriteLine($"# Ошибок компиляции: {compilerResults.Errors.Count}", context.ColorScheme.Error);
            int errorNumber = 1;
            foreach (CompilerError error in compilerResults.Errors) {
                if (error.Line > 0) {
                    context.WriteLine($"# {errorNumber++} (cтрока {error.Line}): {error.ErrorText}", context.ColorScheme.Error);
                } else {
                    context.WriteLine($"# {errorNumber++}: {error.ErrorText}", context.ColorScheme.Error);
                }
            }
        }

        private void WriteSourceCode(string sourceCode, CompilerResults compiledScript) {
            HashSet<int> errorLines = new HashSet<int>();
            foreach (CompilerError error in compiledScript.Errors) {
                errorLines.Add(error.Line);
            }

            string[] lines = sourceCode.Split(new string[] { "\r\n" }, StringSplitOptions.None);
            for (int i = 0; i < lines.Length; i++) {
                string line = lines[i];
                int lineNumber = i + 1;

                context.Write(lineNumber.ToString().PadLeft(4) + ": ", context.ColorScheme.Foreground);
                if (errorLines.Contains(lineNumber)) {
                    context.WriteLine(line, context.ColorScheme.Error);
                } else if (line.TrimStart().StartsWith("//")) {
                    context.WriteLine(line, ConsoleColor.Green);
                } else {
                    context.WriteLine(line, ConsoleColor.Cyan);
                }
            }
        }

        private void WriteExitCode() {
            ConsoleColor color = context.ExitCode == 0 ? context.ColorScheme.Success : context.ColorScheme.Error;
            context.WriteLine($"# Выполнено ({context.ExitCode})", color);
        }

        private void WriteAbort() {
            context.WriteLine($"# Прервано", context.ColorScheme.Error);
        }

        private void ReadKeyForExit() {
            context.WriteLine();
            context.Write("Для выхода нажмите любую клавишу...", context.ColorScheme.Info);
            Console.ReadKey();
        }
    }
}
