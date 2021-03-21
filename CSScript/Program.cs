using CSScript.Core;
using CSScript.Properties;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security.Principal;
using System.Threading;

namespace CSScript
{
    internal class Program
    {
        private readonly ColorScheme colorScheme = ColorScheme.Default;
        private Dictionary<string, Assembly> importedAssemblies;
        private Thread scriptThread;
        private volatile bool scriptThreadAborted;

        private static void Main(string[] args) {
            new Program().Start(args);
        }


        private void Start(string[] args) {

            // При запуске рабочая папка должна быть папкой с программой, для разрешения путей к импортируемым файлам.
            Environment.CurrentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            // Для остановки скрипта комбинацией Ctrl+C
            Console.CancelKeyPress += Console_CancelKeyPress;

            Console.ForegroundColor = colorScheme.Foreground;

            InputArguments arguments = null;
            ScriptContext scriptContext = null;
            try {
                arguments = InputArguments.FromProgramArgs(args);

                WriteHeader();
                if (arguments.IsEmpty) {
                    WriteHelpInfo();
                    ReadKeyByExit();

                } else if (arguments.RegisterMode) {
                    RegisterProgram();

                } else if (arguments.UnregisterMode) {
                    UnregisterProgram();

                } else {
                    if (arguments.HideMode) {
                        // Скрытие окна консоли во время исполнения программы
                        Native.ShowWindow(Native.GetConsoleWindow(), Native.SW_HIDE);
                    }
                    WriteStartInfo(arguments.ScriptPath);

                    // Создание контекста, через который скомпилированный скрипт будет взаимодействовать с окружением
                    scriptContext = new ScriptContext(arguments.ScriptPath, arguments.ScriptArguments.ToArray());
                    scriptContext.OutputLogFragmentAdded += (sender, log) => Write(log.Text, log.Color);
                    scriptContext.ErrorLogFragmentAdded += (sender, log) => WriteError(log.Text, log.Color);
                    scriptContext.ReadLineRequred += (sender, color) => {
                        Console.ForegroundColor = color;
                        return arguments.HideMode ? null : Console.ReadLine();
                    };

                    ScriptInfo scriptInfo = ScriptUtils.CreateScriptInfo(arguments.ScriptPath, ResolveImportFilePath);
                    CompilerResults compiledScript = ScriptUtils.CompileScript(scriptInfo);
                    if (compiledScript.Errors.Count == 0) {

                        // При использовании в скрипте сторонних сборок, необходимо их разрешать вручную
                        importedAssemblies = ScriptUtils.GetImportedAssemblies(scriptInfo);
                        AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;

                        ScriptContainer scriptContainer = ScriptUtils.CreateScriptContainer(compiledScript, scriptContext);

                        // Для использования в скрипте относительных путей к файлам
                        Environment.CurrentDirectory = Path.GetDirectoryName(Path.GetFullPath(arguments.ScriptPath));

                        // Запуск скрипта в отдельном потоке (для возможности прерывания во время исполнения)
                        Exception scriptException = null;
                        scriptThread = new Thread(() => {
                            try { scriptContainer.Start(); } catch (Exception ex) { scriptException = ex; }
                        }) {
                            IsBackground = true
                        };
                        scriptThread.Start();
                        scriptThread.Join();
                        if (scriptException != null) {
                            throw new ScriptRuntimeException(scriptException);
                        }

                    } else {
                        scriptContext.ExitCode = 1;
                        WriteCompileErrors(compiledScript);
                        WriteLine();
                        WriteSourceCode(ScriptUtils.CreateSourceCode(scriptInfo), compiledScript);
                    }
                }

            } catch (Exception ex) {
                if (scriptContext != null) {
                    scriptContext.ExitCode = 1;
                }
                WriteException(ex);

            } finally {
                if (scriptContext != null) {
                    bool pause = scriptContext.Pause || arguments.Pause;
                    int exitCode = scriptContext.ExitCode;
                    scriptContext.Dispose();

                    WriteLine();
                    if (!scriptThreadAborted) {
                        WriteExitCode(exitCode);
                    } else {
                        WriteAbort();
                    }
                    if (pause && !arguments.HideMode) {
                        ReadKeyByExit();
                    }
                    Environment.ExitCode = exitCode;
                }
                Console.ForegroundColor = ConsoleColor.White; // восстановление цвета консоли
            }
        }

        private void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e) {
            e.Cancel = true;
            scriptThreadAborted = true;
            scriptThread?.Abort();
        }

        private Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args) {
            importedAssemblies.TryGetValue(args.Name, out Assembly assembly);
            return assembly;
        }

        private string ResolveImportFilePath(string importFilePath, string workingDirectory) {
            return Path.Combine(Settings.Default.ScriptLibDirectory, importFilePath);
        }


        private void Write(string text, ConsoleColor? color = null) {
            Debug.Write(text);
            Console.ForegroundColor = color ?? colorScheme.Foreground;
            Console.Out.Write(text);
        }

        private void WriteLine(string text, ConsoleColor? color = null) {
            Write(text + Environment.NewLine, color);
        }

        private void WriteLine() {
            Write(Environment.NewLine);
        }

        private void WriteError(string text, ConsoleColor? color = null) {
            Debug.Write(text);
            Console.ForegroundColor = color ?? colorScheme.Error;
            Console.Error.Write(text);
        }

        public void ReadKeyByExit() {
            WriteLine();
            Write("Для выхода нажмите любую клавишу...", colorScheme.Info);
            Console.ReadKey();
        }


        private void WriteHeader() {
            Version version = Assembly.GetExecutingAssembly().GetName().Version;
            string versionText = $"{version.Major:0}.{version.Minor:00}";
            DateTime buildDate = new DateTime(2000, 1, 1).AddDays(version.Build).AddSeconds(version.Revision * 2);

            WriteLine($"## {DateTime.Now} (version {versionText} build {buildDate.ToShortDateString()})", colorScheme.Info);
        }

        private void WriteHelpInfo() {
            string[] split = Resource.HelpText.Split('`');
            ConsoleColor consoleColor = colorScheme.Foreground;
            foreach (string fragment in split) {
                switch (fragment) {
                    case "": consoleColor = colorScheme.Foreground; break;
                    case "c": consoleColor = colorScheme.Caption; break;
                    case "i": consoleColor = colorScheme.Info; break;
                    case "//": consoleColor = ConsoleColor.Green; break; // комментарий
                    case "#": consoleColor = ConsoleColor.Yellow; break; // директива
                    default: Write(fragment, consoleColor); break;
                }
            }
            WriteLine();
        }

        private void WriteStartInfo(string scriptPath) {
            WriteLine($"## {scriptPath}", colorScheme.Info);
            WriteLine();
        }

        private void WriteException(Exception ex) {
            if (ex is ScriptRuntimeException) {
                ex = ex.InnerException; // для корректного отображения StackTrace
            }

            WriteLine($"# Ошибка: {ex.Message}", colorScheme.Error);
            WriteLine($"# {ex.GetType().Name}:", ConsoleColor.Gray);
            foreach (string stackTraceLine in ex.StackTrace.Split(new string[] { Environment.NewLine }, StringSplitOptions.None)) {
                WriteLine("#" + stackTraceLine, ConsoleColor.Gray);
            }

            if (ex.InnerException != null) {
                WriteLine();
                WriteException(ex.InnerException);
            }
        }

        private void WriteCompileErrors(CompilerResults compilerResults) {
            WriteLine($"# Ошибок компиляции: {compilerResults.Errors.Count}", colorScheme.Error);
            int errorNumber = 1;
            foreach (CompilerError error in compilerResults.Errors) {
                if (error.Line > 0) {
                    WriteLine($"# {errorNumber++} (cтрока {error.Line}): {error.ErrorText}", colorScheme.Error);
                } else {
                    WriteLine($"# {errorNumber++}: {error.ErrorText}", colorScheme.Error);
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

                Write(lineNumber.ToString().PadLeft(4) + ": ", colorScheme.Foreground);
                if (errorLines.Contains(lineNumber)) {
                    WriteLine(line, colorScheme.Error);
                } else if (line.TrimStart().StartsWith("//")) {
                    WriteLine(line, ConsoleColor.Green);
                } else {
                    WriteLine(line, ConsoleColor.Cyan);
                }
            }
        }

        private void WriteExitCode(int exitCode) {
            ConsoleColor color = exitCode == 0 ? colorScheme.Success : colorScheme.Error;
            WriteLine($"# Выполнено ({exitCode})", color);
        }

        private void WriteAbort() {
            WriteLine($"# Прервано", colorScheme.Error);
        }

        private void RegisterProgram() {
            Validate.IsTrue(HasAdministativePrivilegies(), "Для работы с реестром необходимы права администратора.");

            WriteLine("Регистрация программы в реестре...");
            RegistryManager.RegisterFileAssociation();

            string executingPath = Assembly.GetExecutingAssembly().Location;
            string path = Path.GetDirectoryName(executingPath);
            string shellExtensionAssemblyPath = Path.Combine(path, "CSScript.ShellExtension.dll");

            if (File.Exists(shellExtensionAssemblyPath)) {
                WriteLine("Регистрация расширения оболочки...");
                Assembly shellExtensionAssembly = Assembly.LoadFrom(shellExtensionAssemblyPath);
                RegistryManager.RegisterShellExtension(shellExtensionAssembly);
            }
            WriteLine("Перезапуск 'Проводник'...");
            RestartWindowsExplorer();

            WriteLine("Успешно", colorScheme.Success);
        }

        private void UnregisterProgram() {
            Validate.IsTrue(HasAdministativePrivilegies(), "Для работы с реестром необходимы права администратора.");

            WriteLine("Удаление регистрации программы в реестре...");
            RegistryManager.UnregisterFileAssociation();

            string shellExtensionAssemblyPath = "CSScript.ShellExtension.dll";
            if (File.Exists(shellExtensionAssemblyPath)) {
                WriteLine("Удаление регистрации расширения оболочки...");
                Assembly shellExtensionAssembly = Assembly.LoadFrom(shellExtensionAssemblyPath);
                RegistryManager.UnregisterShellExtension(shellExtensionAssembly);
            }
            WriteLine("Перезапуск 'Проводник'...");
            RestartWindowsExplorer();

            WriteLine("Успешно", colorScheme.Success);
        }

        private bool HasAdministativePrivilegies() {
            bool isElevated;
            using (WindowsIdentity identity = WindowsIdentity.GetCurrent()) {
                WindowsPrincipal principal = new WindowsPrincipal(identity);
                isElevated = principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            return isElevated;
        }

        private void RestartWindowsExplorer() {
            Process[] explorer = Process.GetProcessesByName("explorer");
            foreach (Process process in explorer) {
                process.Kill();
            }
            // Process.Start("explorer.exe"); - запускается автоматически
        }
    }
}
