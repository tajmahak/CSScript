using CSScript.Core;
using CSScript.Properties;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

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

        private void LoadScriptContainer() {
            Validate.IsNotBlank(context.ScriptPath, "Не указан путь к запускаемому скрипту.");

            if (Path.GetExtension(context.ScriptPath).ToLower() == Constants.ScriptFileExtension) {
                // Обработка файла как текстового скрипта

                ScriptBuilder builder = CreateScriptBuilder();
                string compiledScriptPath = GetCompiledScriptPath(builder.GetSourceCode());
                container = CreateScriptContainerFromAssembly(compiledScriptPath, true);

                if (container == null) {
                    // Компилирование скрипта из исходников
                    CompilerParameters parameters = builder.CreateCompilerParameters();

                    // Если неработоспособная сборка уже загружена в домен, перезаписать её невозиожно.
                    if (!File.Exists(compiledScriptPath)) {
                        parameters.OutputAssembly = compiledScriptPath;
                        parameters.GenerateInMemory = false;
                    }

                    string compiledSctriptDirectory = Path.GetDirectoryName(compiledScriptPath);
                    if (!Directory.Exists(compiledSctriptDirectory)) {
                        Directory.CreateDirectory(compiledSctriptDirectory);
                    }

                    CompilerResults compilerResults = builder.Compile(parameters);
                    if (compilerResults.Errors.Count > 0) {
                        WriteCompileErrors(builder, compilerResults);
                        throw new Exception("Не удалось выполнить сборку скрипта.");
                    }
                    container = ScriptContainerFactory.Create(compilerResults.CompiledAssembly, context);
                }

            } else if (Path.GetExtension(context.ScriptPath).ToLower() == Constants.CompileScriptFileExtension) {
                // Обработка файла как скомпилированного скрипта
                container = CreateScriptContainerFromAssembly(context.ScriptPath, false);

            } else {
                throw new NotSupportedException("Неподдерживаемый формат скрипта.");
            }

            Validate.IsNotNull(container, "Не удалось выполнить инициализацию скрипта.");
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
                // Удаление неработоспособной сбоки скрипта, до загрузки её в домен
                File.Delete(assemblyPath);
                File.Delete(brokenMakPath);
            }
            return container;
        }

        private ScriptBuilder CreateScriptBuilder() {
            ScriptParser parser = new ScriptParser {
                ScriptLibraryPath = Settings.Default.ScriptLibDirectory
            };

            foreach (string usingItem in ParseList(Settings.Default.Usings)) {
                parser.BaseUsings.Add(usingItem);
            }

            foreach (string importItem in ParseList(Settings.Default.Imports)) {
                parser.BaseImports.Add(importItem);
            }

            return parser.ParseFromFile(context.ScriptPath);
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
            nameBuilder.Append(Constants.CompileScriptFileExtension);

            return Path.Combine(
                 Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Temp\\CSScript",
                nameBuilder.ToString());
        }
    }
}
