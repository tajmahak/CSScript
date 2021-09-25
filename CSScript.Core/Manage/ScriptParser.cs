using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CSScript.Core.Manage
{
    public class ScriptParser
    {
        public HashSet<string> BaseUsings { get; private set; } = new HashSet<string>();

        public HashSet<string> BaseImports { get; private set; } = new HashSet<string>();

        public string ScriptLibraryPath { get; set; }


        private ScriptBuilder builder;

        private readonly HashSet<string> importedScripts = new HashSet<string>();


        public ScriptBuilder ParseFromFile(string scriptPath) {
            builder = new ScriptBuilder();
            importedScripts.Clear();

            // загрузка базовых using
            foreach (string usingItem in BaseUsings) {
                builder.AddUsing(usingItem);
            }

            // загрузка базовых зависимостей
            foreach (string importItem in BaseImports) {
                AddImport(importItem, scriptPath);
            }

            // загрузка содержимого скрипта и его зависимостей
            LoadScriptFile(scriptPath, false);

            return builder;
        }


        private void AddImport(string import, string scriptPath) {
            scriptPath = Path.GetFullPath(scriptPath);
            string scriptDirectory = Path.GetDirectoryName(scriptPath);

            string extension = Path.GetExtension(import).ToLowerInvariant();
            bool isWindowsAssembly = extension == ".dll" || extension == ".exe";
            bool isFullPath = Path.IsPathRooted(import);

            if (isWindowsAssembly) {
                if (isFullPath) {
                    // добавление Windows-сборки по полному пути
                    Validate.IsTrue(File.Exists(import), string.Format(
                        "'{0}': не удалось найти Windows-сборку '{1}'", scriptPath, import));
                    builder.AddAssembly(import);

                } else {
                    string importPath = Path.Combine(scriptDirectory, import);
                    if (File.Exists(importPath)) {
                        // добавление Windows-сборки по полученному полному пути
                        builder.AddAssembly(importPath);

                    } else {
                        // Windows-сборка вероятно может содержаться в GAC
                        builder.AddAssembly(import);
                    }
                }
            } else {

                if (isFullPath) {
                    // Загрузка подключаемого скрипта по полному пути
                    Validate.IsTrue(File.Exists(import), string.Format(
                           "'{0}': не удалось найти скрипт '{1}'", scriptPath, import));
                    LoadScriptFile(import, true);

                } else {
                    string importPath = Path.Combine(scriptDirectory, import);
                    if (File.Exists(importPath)) {
                        // Загрузка подключаемого скрипта по полученному полному имени
                        LoadScriptFile(importPath, true);

                    } else {
                        // Загрузка подключаемого скрипта из указанной папки с библиотеками
                        importPath = Path.Combine(ScriptLibraryPath, import + Constants.ScriptFileExtension);
                        if (!File.Exists(importPath)) {
                            // Попытка загрузки скрипта с оригинальным расширением C#
                            importPath = Path.Combine(ScriptLibraryPath, import + ".cs");
                        }

                        Validate.IsTrue(File.Exists(importPath), string.Format(
                           "'{0}': не удалось найти скрипт '{1}'", scriptPath, import));
                        LoadScriptFile(importPath, true);
                    }
                }
            }
        }

        private void LoadScriptFile(string scriptPath, bool imported) {
            scriptPath = Path.GetFullPath(scriptPath).ToLower();

            if (imported && importedScripts.Contains(scriptPath)) {
                return; // текущий скрипт уже импортирован
            }
            importedScripts.Add(scriptPath);

            string[] sourceCodeLines = File.ReadAllLines(scriptPath, Encoding.UTF8);

            HashSet<string> imports = new HashSet<string>();

            StringBuilder currentBlock = builder.ProcedureBlock;
            for (int i = 0; i < sourceCodeLines.Length; i++) {
                string sourceCodeLine = sourceCodeLines[i];

                // Идентификация исполняемого кода скрипта. В VisualStudio распознаётся как комментарий.
                int executingScriptCodeStartIndex = sourceCodeLine.LastIndexOf("/////");
                if (executingScriptCodeStartIndex != -1) {
                    sourceCodeLine = sourceCodeLine.Substring(executingScriptCodeStartIndex + "/////".Length);
                }

                string preparedLine = sourceCodeLine.TrimStart();

                if (preparedLine.StartsWith("#using") || (preparedLine.StartsWith("using") && preparedLine.EndsWith(";"))) {
                    string[] split = SplitServiceLine(preparedLine, " ");
                    string usingItem = split[1].Trim().TrimEnd(';');
                    builder.AddUsing(usingItem);

                } else if (preparedLine.StartsWith("#import")) {
                    string[] split = SplitServiceLine(preparedLine, " ");
                    string import = split[1].Trim().TrimEnd(';');
                    imports.Add(import);

                } else if (preparedLine.StartsWith("#init")) {
                    currentBlock = builder.InitBlock;

                } else if (preparedLine.StartsWith("#class")) {
                    currentBlock = builder.ClassBlock;

                } else if (preparedLine.StartsWith("#namespace")) {
                    currentBlock = builder.NamespaceBlock;

                } else {
                    if (!imported || currentBlock != builder.ProcedureBlock) {
                        currentBlock.AppendLine(sourceCodeLine);
                    }
                }
            }

            foreach (string import in imports) {
                AddImport(import, scriptPath);
            }
        }

        private static string[] SplitServiceLine(string value, string separator) {
            int index = value.IndexOf(separator);
            Validate.IsTrue(index != -1, "Некорректная строка: '" + value + "'");
            return new string[] { value.Remove(index), value.Substring(index + separator.Length) };
        }
    }
}
