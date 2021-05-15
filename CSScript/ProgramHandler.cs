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
    public class ProgramHandler
    {
        private Dictionary<string, Assembly> importedAssemblies;
        private ConsoleContext context;
        private ScriptExecutionContext executionContext;

        public void Start(string[] args) {
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

                ScriptContainer script = GetScriptContainer(arguments);

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
                context.WriteLine();
                WriteException(ex);

            } finally {
                Environment.ExitCode = context.ExitCode;

                context.KillManagedProcesses();

                if (executionContext != null && executionContext.Aborted) {
                    WriteAbort();
                } else {
                    if (!context.Hidden && (context.Pause || forcedPause)) {
                        ReadKeyForExit();
                    }
                }
            }
        }

        public void Abort() {
            executionContext?.Abort();
        }

        // Используется для возможности выполнения скрипта из стенда
        public event Func<ConsoleContext, ScriptContainer> GetScriptContainerEvent;


        private void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e) {
            Abort();
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
            return Path.Combine(Settings.Default.ScriptLibDirectory, importFilePath + RegistryManager.ScriptFileExtension);
        }


        private ScriptContainer GetScriptContainer(InputArguments arguments) {
            if (GetScriptContainerEvent != null) {
                return GetScriptContainerEvent.Invoke(context);
            }

            switch (arguments.Mode) {

                case InputArguments.WorkMode.Script:
                    return CreateScriptContainer();

                case InputArguments.WorkMode.Help:
                    return new HelpScript(context);

                case InputArguments.WorkMode.Register:
                    return new RegistrationScript(context);

                case InputArguments.WorkMode.Unregister:
                    return new UnregistrationScript(context);

                default: throw new Exception("Неподдерживаемый режим работы.");
            }
        }

        private ScriptContainer CreateScriptContainer() {
            ScriptInfo scriptInfo = ScriptUtils.CreateScriptInfo(context.ScriptPath, ResolveImportFilePath);
            CompilerResults compiledScript = ScriptUtils.CompileScript(scriptInfo);
            if (compiledScript.Errors.Count == 0) {
                importedAssemblies = ScriptUtils.GetImportedAssemblies(scriptInfo);
                return ScriptUtils.CreateScriptContainer(compiledScript, context);

            } else {
                WriteCompileErrors(scriptInfo, compiledScript);
                throw new Exception("Не удалось выполнить сборку скрипта.");
            }
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

        private void WriteCompileErrors(ScriptInfo scriptInfo, CompilerResults compilerResults) {
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
            string sourceCode = ScriptUtils.CreateSourceCode(scriptInfo);
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
