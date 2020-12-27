﻿using CSScript.Core;
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
        private static ScriptContext scriptContext;
        private static readonly ColorScheme ColorScheme = ColorScheme.Default;
        private static Dictionary<string, Assembly> definedAssemblies;
        private static Thread scriptThread;
        private static volatile bool scriptThreadAborted;


        private static void Main(string[] args) {
            Console.CancelKeyPress += Console_CancelKeyPress;
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;

            InputArguments arguments = null;
            try {
                arguments = InputArguments.FromProgramArgs(args);

                if (arguments.IsEmpty) {
                    WriteHelpInfo();
                    Console.ReadKey();
                } else if (arguments.RegisterMode) {
                    RegisterProgram();
                } else if (arguments.UnregisterMode) {
                    UnregistryProgram();
                } else {
                    if (arguments.HideMode) {
                        // Скрытие окна консоли во время исполнения программы
                        Native.ShowWindow(Native.GetConsoleWindow(), Native.SW_HIDE);
                    }

                    scriptContext = new ScriptContext(arguments.ScriptPath, arguments.ScriptArguments.ToArray());
                    scriptContext.LogAdded += (sender, log) => Write(log.Text, log.Color);
                    scriptContext.ReadLineRequred += (sender, color) => {
                        Console.ForegroundColor = color;
                        return arguments.HideMode ? null : Console.ReadLine();
                    };

                    WriteStartInfo(arguments.ScriptPath);

                    ScriptInfo scriptInfo = ScriptUtils.CreateScriptInfo(arguments.ScriptPath);
                    CompilerResults compiledScript = ScriptUtils.CompileScript(scriptInfo);
                    if (compiledScript.Errors.Count == 0) {
                        Environment.CurrentDirectory = Utils.GetDirectoryName(scriptInfo.ScriptPath);
                        definedAssemblies = ScriptUtils.GetDefinedAssemblies(scriptInfo);
                        ScriptContainer scriptContainer = ScriptUtils.CreateScriptContainer(compiledScript, scriptContext);

                        scriptThread = new Thread(scriptContainer.Start) {
                            IsBackground = true
                        };
                        scriptThread.Start();
                        scriptThread.Join();

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
                    bool autoClose = scriptContext.AutoClose;
                    scriptContext.Dispose();

                    WriteLine();
                    if (!scriptThreadAborted) {
                        WriteExitCode(scriptContext.ExitCode);
                    } else {
                        WriteAbort();
                    }
                    if (!arguments.HideMode && !autoClose || scriptThreadAborted) {
                        Console.ReadKey();
                    }
                }
                Console.ForegroundColor = ConsoleColor.White; // восстановление цвета консоли
            }
        }

        private static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e) {
            e.Cancel = true;
            scriptThreadAborted = true;
            scriptThread.Abort();
        }

        private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args) {
            definedAssemblies.TryGetValue(args.Name, out Assembly assembly);
            return assembly;
        }


        private static void Write(string text, ConsoleColor? color = null) {
            Debug.Write(text);
            Console.ForegroundColor = color ?? ColorScheme.Foreground;
            Console.Write(text);
        }

        private static void WriteLine(string text, ConsoleColor? color = null) {
            Write(text + Environment.NewLine, color);
        }

        private static void WriteLine() {
            Debug.WriteLine(null);
            Console.WriteLine();
        }


        private static void WriteHelpInfo() {
            string[] lines = Resource.HelpText.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
            for (int i = 0; i < lines.Length; i++) {
                ConsoleColor color = ColorScheme.Foreground;
                string[] line = ParseHelpInfoLine(lines[i]);
                switch (line[0]) {
                    case "c": color = ColorScheme.Caption; break;
                    case "i": color = ColorScheme.Info; break;
                }
                WriteLine(line[1], color);
            }
        }

        private static void WriteStartInfo(string scriptPath) {
            WriteLine($"## {scriptPath}", ColorScheme.Info);
            WriteLine($"## {DateTime.Now}", ColorScheme.Info);
            WriteLine();
        }

        private static void WriteException(Exception ex) {
            WriteLine($"# Ошибка: {ex.Message}", ColorScheme.Error);
            foreach (string stackTraceLine in ex.StackTrace.Split(new string[] { Environment.NewLine }, StringSplitOptions.None)) {
                WriteLine("#" + stackTraceLine, ColorScheme.StackTrace);
            }

            if (ex.InnerException != null) {
                WriteLine();
                WriteException(ex.InnerException);
            }
        }

        private static void WriteCompileErrors(CompilerResults compilerResults) {
            WriteLine($"# Ошибок компиляции: {compilerResults.Errors.Count}", ColorScheme.Error);
            int errorNumber = 1;
            foreach (CompilerError error in compilerResults.Errors) {
                if (error.Line > 0) {
                    WriteLine($"# {errorNumber++} (cтрока {error.Line}): {error.ErrorText}", ColorScheme.Error);
                } else {
                    WriteLine($"# {errorNumber++}: {error.ErrorText}", ColorScheme.Error);
                }
            }
        }

        private static void WriteSourceCode(string sourceCode, CompilerResults compiledScript) {
            HashSet<int> errorLines = new HashSet<int>();
            foreach (CompilerError error in compiledScript.Errors) {
                errorLines.Add(error.Line);
            }

            string[] lines = sourceCode.Split(new string[] { "\r\n" }, StringSplitOptions.None);
            for (int i = 0; i < lines.Length; i++) {
                string line = lines[i];
                int lineNumber = i + 1;

                Write(lineNumber.ToString().PadLeft(4) + ": ", ColorScheme.Foreground);
                if (errorLines.Contains(lineNumber)) {
                    WriteLine(line, ColorScheme.Error);
                } else {
                    if (line.TrimStart().StartsWith("//")) {
                        WriteLine(line, ColorScheme.Comment);
                    } else {
                        WriteLine(line, ColorScheme.SourceCode);
                    }
                }
            }
        }

        private static void WriteExitCode(int exitCode) {
            ConsoleColor color = exitCode == 0 ? ColorScheme.Success : ColorScheme.Error;
            WriteLine($"# Выполнено ({exitCode})", color);
        }

        private static void WriteAbort() {
            WriteLine($"# Прервано", ColorScheme.Error);
        }

        private static void RegisterProgram() {
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

            WriteLine("Успешно", ColorScheme.Success);
        }

        private static void UnregistryProgram() {
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

            WriteLine("Успешно", ColorScheme.Success);
        }

        private static bool HasAdministativePrivilegies() {
            bool isElevated;
            using (WindowsIdentity identity = WindowsIdentity.GetCurrent()) {
                WindowsPrincipal principal = new WindowsPrincipal(identity);
                isElevated = principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            return isElevated;
        }

        private static void RestartWindowsExplorer() {
            Process[] explorer = Process.GetProcessesByName("explorer");
            foreach (Process process in explorer) {
                process.Kill();
            }
            // Запускается автоматически
            // Process.Start("explorer.exe");
        }


        private static string[] ParseHelpInfoLine(string line) {
            if (line.StartsWith("`")) {
                int index = line.IndexOf("`", 1);
                return new string[]
                {
                    line.Substring(1, index - 1),
                    line.Substring(index + 1),
                };
            } else {
                return new string[] { null, line };
            }
        }
    }
}
