using CSScript.Core;
using CSScript.Core.Manage;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace CSScript
{
    public class ConsoleScriptHandler
    {
        private readonly ConsoleScriptContext context;
        private ScriptContainer container;
        private ScriptThread thread;
        private readonly object threadLock = new object();
        private bool executed = false;

        public ConsoleScriptHandler(ConsoleScriptContext context) {
            this.context = context;
        }

        public ConsoleScriptHandler(ScriptContainer container) {
            this.container = container;
            context = (ConsoleScriptContext)container.Context;
        }

        public void Start() {
            executed = true;
            Monitor.Enter(threadLock); // установка блокировки для возможности окончания работы принудительно остановленного процесса
            try {
                WriteHeader();
                WriteStartInfo();

                // При запуске рабочая папка должна быть папкой с программой, для разрешения путей к импортируемым файлам.
                Environment.CurrentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

                if (container == null) {
                    LoadScriptContainer();
                }

                if (context.ScriptPath != null) {
                    // Для использования в скрипте относительных путей к файлам
                    Environment.CurrentDirectory = Path.GetDirectoryName(Path.GetFullPath(context.ScriptPath));
                }

                thread = new ScriptThread(container);
                thread.Start();
                thread.Join();
                if (thread.ThreadException != null) {
                    throw thread.ThreadException;
                }

            } catch (ThreadAbortException) {
                context.ExitCode = 1;

            } catch (Exception ex) {
                context.ExitCode = 1;
                context.WriteLine();
                WriteException(ex);

            } finally {
                Environment.ExitCode = context.ExitCode;

                AbortRegisteredProcesses();

                if (thread != null && thread.Aborted) {
                    context.WriteErrorLine($"# Прервано");
                    executed = false;

                } else if (!context.HiddenMode && context.Pause) {
                    executed = false;

                    context.WriteLine();
                    context.Write($"# Выполнено ({context.ExitCode}). Для выхода нажмите любую клавишу или закройте окно ...", context.ColorScheme.Info);
                    Console.Read(); // при .ReadKey() не срабатывает комбинация Ctrl+C для остановки
                }

                Monitor.Exit(threadLock);
            }
        }

        public void Abort() {
            if (executed) {
                context.WriteError("# Прерывание выполнения...");

                thread?.Abort();

                // Ожидание завершения остановленного потока
                Monitor.Enter(threadLock);
                Monitor.Exit(threadLock);

                context.WriteError("# Прервано пользователем");
            }
        }


        private void LoadScriptContainer() {
            Validate.IsNotBlank(context.ScriptPath, "Не указан путь к запускаемому скрипту.");

            ScriptBuilder builder = CreateScriptBuilder();
            string compiledScriptPath = GetCompiledScriptPath(builder.GetSourceCode());
            string compiledSctriptDirectory = Path.GetDirectoryName(compiledScriptPath);

            if (!Directory.Exists(compiledSctriptDirectory)) {
                Directory.CreateDirectory(compiledSctriptDirectory);
            }

            container = CreateScriptContainerFromAssembly(compiledScriptPath, true);

            if (container == null) {
                // Компилирование скрипта из исходников
                CompilerParameters parameters = builder.CreateCompilerParameters();

                // Если неработоспособная сборка уже загружена в домен, перезаписать её невозиожно.
                if (!File.Exists(compiledScriptPath)) {
                    parameters.OutputAssembly = compiledScriptPath;
                    parameters.GenerateInMemory = false;
                }

                CompilerResults compilerResults = builder.Compile(parameters);
                if (compilerResults.Errors.Count > 0) {
                    WriteCompileErrors(builder, compilerResults);
                    throw new Exception("Не удалось выполнить сборку скрипта.");
                }
                container = ScriptContainerFactory.Create(compilerResults.CompiledAssembly, context);
            }
        }

        private ScriptContainer CreateScriptContainerFromAssembly(string assemblyPath, bool createBrokenMark) {
            ScriptContainer container = null;
            string brokenMakPath = assemblyPath + ".broken";

            if (File.Exists(assemblyPath) && !File.Exists(brokenMakPath)) {
                Assembly compiledAssembly = Assembly.LoadFrom(assemblyPath);
                container = ScriptContainerFactory.Create(compiledAssembly, context);
                if (container == null) {
                    /// Сборка становится неработоспособной после изменения номера версии CSScript.Core
                    /// После загрузки сборки в домен удалить её при текущем запуске программы уже невозможно.
                    /// В случае, если сборка не была загружена корректно, она помечается как неработоспособная.
                    /// При следующем запуске программы она будет удалена без загрузки её в домен.
                    if (createBrokenMark) {
                        File.Create(brokenMakPath).Close();
                    }
                }

            } else {
                // Удаление неработоспособной сбоки скрипта, без попытки загрузки её в домен
                File.Delete(assemblyPath);
                File.Delete(brokenMakPath);
            }
            return container;
        }

        private ScriptBuilder CreateScriptBuilder() {
            ScriptParser parser = new ScriptParser {
                ScriptLibraryPath = Settings.Default.ScriptLibDirectory
            };

            foreach (string usingItem in Settings.Default.Usings) {
                parser.BaseUsings.Add(usingItem);
            }

            foreach (string importItem in Settings.Default.Imports) {
                parser.BaseImports.Add(importItem);
            }

            return parser.ParseFromFile(context.ScriptPath);
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
            // StackTrace бесполезен, т.к. не отображает вложенность исключения внутри скрипта
            if (ex.InnerException != null) {
                context.WriteErrorLine();
                WriteException(ex.InnerException);
            }
        }

        private void WriteCompileErrors(ScriptBuilder builder, CompilerResults compilerResults) {
            List<CompilerError> errors = new List<CompilerError>();
            HashSet<int> errorLines = new HashSet<int>();
            foreach (CompilerError error in compilerResults.Errors) {
                errors.Add(error);
                errorLines.Add(error.Line);
            }

            string sourceCode = builder.GetSourceCode();
            string[] lines = sourceCode.Split(new string[] { "\r\n" }, StringSplitOptions.None);
            for (int i = 0; i < lines.Length; i++) {
                string line = lines[i];
                int lineNumber = i + 1;

                context.Write(lineNumber.ToString().PadLeft(4) + ": ", context.ColorScheme.Info);
                if (errorLines.Contains(lineNumber)) {
                    context.Write(line, context.ColorScheme.Error);
                    string errorTexts = string.Join("; ", errors.FindAll(x => x.Line == lineNumber).Select(x => x.ErrorText));
                    context.WriteLine(" // # Ошибка: " + errorTexts, context.ColorScheme.Info);
                } else if (line.TrimStart().StartsWith("//")) {
                    context.WriteLine(line, ConsoleColor.Green);
                } else {
                    context.WriteLine(line);
                }
            }

            context.WriteLine();
            int errorNumber = 1;
            foreach (CompilerError error in compilerResults.Errors) {
                if (error.Line > 0) {
                    context.WriteErrorLine($"# {errorNumber++} (cтрока {error.Line}): {error.ErrorText}");
                } else {
                    context.WriteErrorLine($"# {errorNumber++}: {error.ErrorText}");
                }
            }
        }

        private string GetCompiledScriptPath(string sourceCode) {
            byte[] hash;
            using (MD5 md5 = MD5.Create()) {
                byte[] rawSourceCode = Encoding.UTF8.GetBytes(sourceCode);
                hash = md5.ComputeHash(rawSourceCode);
            }

            StringBuilder nameBuilder = new StringBuilder();
            foreach (byte hashByte in hash) {
                nameBuilder.Append(hashByte.ToString("x2"));
            }
            nameBuilder.Append(".dll");

            return Path.Combine(
                 Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Temp\\CSScript",
                nameBuilder.ToString());
        }

        private void AbortRegisteredProcesses() {
            foreach (Process process in context.RegisteredProcesses) {
                if (!process.HasExited) {
                    if (process.MainWindowHandle != (IntPtr)0) {
                        // если процесс не интегрирован в текущую консоль, он не будет остановлен с текущей консолью
                        process.Kill();
                    }
                    process.WaitForExit();
                }
            }
        }
    }
}
