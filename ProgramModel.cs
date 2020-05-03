using CSScript.Properties;
using Microsoft.CSharp;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;

namespace CSScript
{
    internal class ProgramModel
    {
        public bool GUIMode { get; private set; }

        public bool Finished { get; private set; }

        public int ExitCode { get; private set; }

        public ReadOnlyCollection<LogItem> LogItems => logItems.AsReadOnly();



        private readonly List<LogItem> logItems = new List<LogItem>();

        private readonly InputArgumentsInfo inputArguments = new InputArgumentsInfo();

        private readonly Dictionary<string, Assembly> resolvedAssemblies = new Dictionary<string, Assembly>();

        private readonly Queue<Process> managedProcesses = new Queue<Process>();

        private Thread executingThread;

        private readonly bool startDebugScript;



        public event Action<LogItem> AddLogEvent;

        public event Action FinishedEvent;



        public ProgramModel()
        {
            startDebugScript = true;
            GUIMode = true;
        }

        public ProgramModel(string[] args)
        {
            ParseInputArguments(args);
            GUIMode = inputArguments.IsEmpty || !inputArguments.HideMode;
        }



        public void ExecuteScriptAsync()
        {
            executingThread = startDebugScript ? new Thread(StartDebugScript) : new Thread(StartScript);
            executingThread.IsBackground = true;
            executingThread.Start();
        }

        public void JoinExecutingThread()
        {
            if (executingThread != null)
            {
                executingThread.Join();
            }
        }

        public Assembly ResolveAssembly(string assemblyName)
        {
            resolvedAssemblies.TryGetValue(assemblyName, out Assembly assembly);
            return assembly;
        }

        public Process CreateManagedProcess()
        {
            Process process = new Process();
            lock (managedProcesses)
            {
                managedProcesses.Enqueue(process);
            }
            return process;
        }

        public void KillManagedProcesses()
        {
            lock (managedProcesses)
            {
                while (managedProcesses.Count > 0)
                {
                    Process process = managedProcesses.Dequeue();
                    try
                    {
                        process.Kill();
                    }
                    catch
                    {
                    }
                }
            }
        }

        public void WriteLog(string text, Color? foreColor = null)
        {
            text = text ?? string.Empty;
            LogItem item = new LogItem(text, DateTime.Now, foreColor);
            logItems.Add(item);
            AddLogEvent?.Invoke(item);
        }

        public void WriteLineLog(string text, Color? foreColor = null)
        {
            WriteLog(text + Environment.NewLine, foreColor);
        }

        public void WriteLineLog()
        {
            WriteLog(Environment.NewLine);
        }



        private void StartScript()
        {
            try
            {
                if (inputArguments.IsEmpty)
                {
                    WriteLogHelpInfo();
                    ExitCode = 0;
                }
                else
                {
                    string scriptPath = GetFullPath(inputArguments.ScriptPath, Environment.CurrentDirectory);
                    string scriptText = GetScriptText(scriptPath);

                    WriteLogStartInfo(scriptPath);

                    // после загрузки содержимого скрипта переключаемся на его рабочую директорию вместо рабочей директории программы
                    // (для возможности указания коротких путей к файлам)
                    Environment.CurrentDirectory = GetWorkDirectoryPath(scriptPath);

                    ScriptParsingInfo scriptParsingInfo = ParseScriptInfo(scriptText, scriptPath);
                    LoadAssembliesForResolve(scriptParsingInfo);
                    CompilerResults compilerResults = Compile(scriptParsingInfo);

                    if (compilerResults.Errors.Count == 0)
                    {
                        ScriptRuntime scriptRuntime = GetCompiledScript(compilerResults);
                        scriptRuntime.StartScript(inputArguments.ScriptArgument);
                        ExitCode = scriptRuntime.ExitCode;
                    }
                    else
                    {
                        WriteLogCompileErrors(compilerResults);
                        string sourceCode = GetSourceCode(scriptParsingInfo);

                        WriteLineLog();

                        WriteLogSourceCode(sourceCode, compilerResults);
                        ExitCode = 1;
                    }
                }
            }
            catch (Exception ex)
            {
                WriteLogException(ex);
                ExitCode = 1;
            }

            WriteLineLog();
            WriteLineLog($"# Выполнено с кодом возврата: {ExitCode}", Settings.Default.InfoColor);

            if (inputArguments != null && !string.IsNullOrEmpty(inputArguments.LogPath))
            {
                try
                {
                    SaveLogs(inputArguments.LogPath);
                }
                catch (Exception ex)
                {
                    WriteLineLog($"# Не удалось сохранить лог: {ex.Message}", Settings.Default.ErrorColor);
                }
            }

            Finished = true;
            FinishedEvent?.Invoke();
        }

        private void StartDebugScript()
        {
#if DEBUG
            try
            {
                DebugScript debugScript = new DebugScript();
                debugScript.StartScript(inputArguments.ScriptArgument);
                ExitCode = debugScript.ExitCode;
            }
            catch (Exception ex)
            {
                WriteLogException(ex);
                ExitCode = 1;
            }

            WriteLineLog();
            WriteLineLog($"# Выполнено с кодом возврата: {ExitCode}", Settings.Default.InfoColor);

            Finished = true;
            FinishedEvent?.Invoke();
#endif
        }

        private void ParseInputArguments(string[] args)
        {
            if (args.Length == 0)
            {
                inputArguments.IsEmpty = true;
            }
            else
            {
                for (int i = 0; i < args.Length; i++)
                {
                    string arg = args[i];
                    string preparedArg = arg.Trim().ToLower();
                    if (preparedArg == "/h" || preparedArg == "/hide")
                    {
                        inputArguments.HideMode = true;
                    }
                    else if (preparedArg == "/a" || preparedArg == "/arg")
                    {
                        inputArguments.ScriptArgument = args[++i];
                    }
                    else if (preparedArg == "/l" || preparedArg == "/log")
                    {
                        inputArguments.LogPath = args[++i];
                    }
                    else
                    {
                        if (inputArguments.ScriptPath == null)
                        {
                            inputArguments.ScriptPath = arg;
                        }
                    }
                }
            }
        }

        private void LoadAssembliesForResolve(ScriptParsingInfo scriptParsingInfo)
        {
            string[] assemblies = GetReferencedAssemblies(scriptParsingInfo);
            foreach (string assembly in assemblies)
            {
                if (File.Exists(assembly))
                {
                    // загружаются только те сборки, которые рантайм не может подгрузить автоматически
                    Assembly loadedAssembly = Assembly.LoadFrom(assembly);
                    resolvedAssemblies.Add(loadedAssembly.FullName, loadedAssembly);
                }
            }
        }

        private void WriteLogHelpInfo()
        {
            string[] lines = Resources.HelpText.Split(new string[] { "\r\n" }, StringSplitOptions.None);
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                if (line.StartsWith("<c>"))
                {
                    line = line.Substring(3); //<c>
                    WriteLineLog(line, Settings.Default.CaptionColor);
                }
                else if (line.StartsWith("<i>"))
                {
                    line = line.Substring(3); //<i>
                    WriteLineLog(line, Settings.Default.InfoColor);
                }
                else
                {
                    WriteLineLog(line);
                }
            }

        }

        private void WriteLogStartInfo(string scriptPath)
        {
            WriteLineLog($"## {scriptPath}", Settings.Default.InfoColor);
            WriteLineLog($"## {DateTime.Now}", Settings.Default.InfoColor);
            WriteLineLog();
        }

        private void WriteLogException(Exception ex)
        {
            WriteLineLog($"# Ошибка: {ex.Message}", Settings.Default.ErrorColor);
        }

        private void WriteLogCompileErrors(CompilerResults compilerResults)
        {
            WriteLineLog($"# Ошибок компиляции: {compilerResults.Errors.Count}", Settings.Default.ErrorColor);
            int errorNumber = 1;
            foreach (CompilerError error in compilerResults.Errors)
            {
                if (error.Line > 0)
                {
                    WriteLineLog($"# {errorNumber++} (cтрока {error.Line}): {error.ErrorText}", Settings.Default.ErrorColor);
                }
                else
                {
                    WriteLineLog($"# {errorNumber++}: {error.ErrorText}", Settings.Default.ErrorColor);
                }
            }
        }

        private void WriteLogSourceCode(string sourceCode, CompilerResults compilerResults = null)
        {
            HashSet<int> errorLines = new HashSet<int>();
            if (compilerResults != null)
            {
                foreach (CompilerError error in compilerResults.Errors)
                {
                    errorLines.Add(error.Line);
                }
            }

            string[] lines = sourceCode.Split(new string[] { "\r\n" }, StringSplitOptions.None);
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                int lineNumber = i + 1;

                WriteLog(lineNumber.ToString().PadLeft(4) + ": ");
                if (errorLines.Contains(lineNumber))
                {
                    WriteLineLog(line, Settings.Default.ErrorColor);
                }
                else
                {
                    if (line.TrimStart().StartsWith("//"))
                    {
                        WriteLineLog(line, Settings.Default.CommentColor);
                    }
                    else
                    {
                        WriteLineLog(line, Settings.Default.SourceCodeColor);
                    }
                }
            }
        }

        private string GetLogText()
        {
            StringBuilder str = new StringBuilder();
            foreach (LogItem logItem in logItems)
            {
                str.Append(logItem.Text);
            }
            return str.ToString();
        }

        private void SaveLogs(string logPath)
        {
            string logText = GetLogText();
            using (StreamWriter writer = new StreamWriter(logPath, true, Encoding.UTF8))
            {
                writer.WriteLine(logText);
                writer.WriteLine();
                writer.WriteLine();
            }
        }



        private static ScriptParsingInfo ParseScriptInfo(string scriptText, string scriptPath)
        {
            List<string> definedScriptsList = new List<string>();
            List<string> definedAssemblyList = new List<string>();
            List<string> usingList = new List<string>();

            StringBuilder functionBlock = new StringBuilder();
            StringBuilder classBlock = new StringBuilder();
            StringBuilder namespaceBlock = new StringBuilder();

            StringBuilder currentBlock = functionBlock;

            string[] operatorBlocks = scriptText.Split(new string[] { ";" }, StringSplitOptions.None);
            for (int i = 0; i < operatorBlocks.Length; i++)
            {
                string operatorBlock = operatorBlocks[i];
                string[] lines = operatorBlock.Split(new string[] { "\r\n" }, StringSplitOptions.None);
                for (int j = 0; j < lines.Length; j++)
                {
                    string line = lines[j];
                    string trimLine = line.TrimStart();
                    if (trimLine.Length > 0)
                    {
                        if (trimLine.StartsWith("#")) // служебные конструкции
                        {
                            if (trimLine.StartsWith("#definescript"))
                            {
                                string preparedLine = line.Replace("#definescript", "").Trim();
                                definedScriptsList.Add(preparedLine);
                            }
                            else if (trimLine.StartsWith("#define"))
                            {
                                string preparedLine = line.Replace("#define", "").Trim();
                                definedAssemblyList.Add(preparedLine);
                            }

                            else if (trimLine.StartsWith("#using"))
                            {
                                string preparedLine = line.Replace("#", "").Trim() + ";";
                                usingList.Add(preparedLine);
                            }
                            else if (trimLine.StartsWith("#class"))
                            {
                                currentBlock = classBlock;
                                continue;
                            }
                            else if (trimLine.StartsWith("#ns") || trimLine.StartsWith("#namespace"))
                            {
                                currentBlock = namespaceBlock;
                                continue;
                            }
                        }
                        else
                        {
                            if (currentBlock.Length > 0)
                            {
                                currentBlock.AppendLine();
                            }
                            currentBlock.Append(line);
                        }
                    }
                }
                if (i < operatorBlocks.Length - 1)
                {
                    currentBlock.Append(';');
                }
            }

            // преобразование из относительных в абсолютные пути для подключаемых сборок
            string scriptWorkDirectory = GetWorkDirectoryPath(scriptPath);
            for (int i = 0; i < definedAssemblyList.Count; i++)
            {
                definedAssemblyList[i] = GetCorrectAssemblyPath(definedAssemblyList[i], scriptWorkDirectory);
            }

            ScriptParsingInfo scriptParsingInfo = new ScriptParsingInfo()
            {
                ScriptPath = scriptPath,
                DefinedAssemblyList = definedAssemblyList,
                UsingList = usingList,
                ProcedureBlock = functionBlock,
                ClassBlock = classBlock,
                NamespaceBlock = namespaceBlock,
            };

            foreach (string definedScript in definedScriptsList)
            {
                MergeScripts(scriptParsingInfo, definedScript);
            }

            // удаление дублирующихся позиций в списке

            DeleteDuplicates(scriptParsingInfo.DefinedAssemblyList,
                (x, y) => string.Equals(x, y, StringComparison.OrdinalIgnoreCase));

            DeleteDuplicates(scriptParsingInfo.UsingList,
                (x, y) => string.Equals(x, y));

            return scriptParsingInfo;
        }

        private static void MergeScripts(ScriptParsingInfo scriptParsingInfo, string definedScriptPath)
        {
            string scriptPath = GetFullPath(definedScriptPath, GetWorkDirectoryPath(scriptParsingInfo.ScriptPath));
            string scriptText = GetScriptText(scriptPath);

            ScriptParsingInfo definedScriptParsingInfo = ParseScriptInfo(scriptText, scriptPath);

            foreach (string definedAssembly in definedScriptParsingInfo.DefinedAssemblyList)
            {
                scriptParsingInfo.DefinedAssemblyList.Add(definedAssembly);
            }
            foreach (string usingLine in definedScriptParsingInfo.UsingList)
            {
                scriptParsingInfo.UsingList.Add(usingLine);
            }

            if (definedScriptParsingInfo.ClassBlock.Length > 0)
            {
                scriptParsingInfo.ClassBlock.AppendLine();
                scriptParsingInfo.ClassBlock.AppendLine("// <<< " + scriptPath);
                scriptParsingInfo.ClassBlock.AppendLine(definedScriptParsingInfo.ClassBlock.ToString());
                scriptParsingInfo.ClassBlock.AppendLine("// >>> " + scriptPath);
            }
            if (definedScriptParsingInfo.NamespaceBlock.Length > 0)
            {
                scriptParsingInfo.NamespaceBlock.AppendLine();
                scriptParsingInfo.NamespaceBlock.AppendLine("// <<< " + scriptPath);
                scriptParsingInfo.NamespaceBlock.AppendLine(definedScriptParsingInfo.NamespaceBlock.ToString());
                scriptParsingInfo.NamespaceBlock.AppendLine("// >>> " + scriptPath);
            }
        }

        private static string GetScriptText(string scriptPath)
        {
            return File.ReadAllText(scriptPath, Encoding.UTF8);
        }

        private static string GetFullPath(string path, string workDirectory)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new Exception("Отсутствует путь к файлу.");
            }

            if (!Path.IsPathRooted(path))
            {
                path = Path.Combine(workDirectory, path);
            }

            if (!File.Exists(path))
            {
                throw new Exception($"Файл '{path}' не найден.");
            }

            return path;
        }

        private static string GetWorkDirectoryPath(string path)
        {
            return Path.GetDirectoryName(path);
        }

        private static string GetCorrectAssemblyPath(string assemblyPath, string workDirectory)
        {
            if (!Path.IsPathRooted(assemblyPath))
            {
                // библиотека, указанная по относительному пути, находится либо в рабочей папке, либо в GAC
                string fullPath = Path.Combine(workDirectory, assemblyPath);
                if (File.Exists(fullPath))
                {
                    return fullPath;
                }
            }
            return assemblyPath;
        }

        private static void DeleteDuplicates<T>(List<T> list, Func<T, T, bool> comparison)
        {
            for (int i = 0; i < list.Count - 1; i++)
            {
                T value1 = list[i];
                for (int j = i + 1; j < list.Count; j++)
                {
                    T value2 = list[j];
                    if (comparison(value1, value2))
                    {
                        list.RemoveAt(j--);
                    }
                }
            }
        }

        private static string GetSourceCode(ScriptParsingInfo scriptParsingInfo)
        {
            string source = Resources.SourceCodeTemplate;
            source = source.Replace("##using##", string.Join("\r\n", scriptParsingInfo.UsingList.ToArray()));
            source = source.Replace("##procedure##", scriptParsingInfo.ProcedureBlock.ToString());
            source = source.Replace("##class##", scriptParsingInfo.ClassBlock.ToString());
            source = source.Replace("##namespace##", scriptParsingInfo.NamespaceBlock.ToString());
            return source;
        }

        private static CompilerResults Compile(ScriptParsingInfo scriptParsingInfo)
        {
            string[] assemblies = GetReferencedAssemblies(scriptParsingInfo);
            string sourceCode = GetSourceCode(scriptParsingInfo);

            using (CSharpCodeProvider provider = new CSharpCodeProvider())
            {
                CompilerParameters compileParameters = new CompilerParameters(assemblies)
                {
                    GenerateInMemory = true,
                    GenerateExecutable = false
                };
                CompilerResults compilerResults = provider.CompileAssemblyFromSource(compileParameters, sourceCode);
                return compilerResults;
            }
        }

        private static ScriptRuntime GetCompiledScript(CompilerResults compilerResults)
        {
            Assembly compiledAssembly = compilerResults.CompiledAssembly;
            object instance = compiledAssembly.CreateInstance("CSScript.CompiledScript");
            return (ScriptRuntime)instance;
        }

        private static string[] GetReferencedAssemblies(ScriptParsingInfo scriptParsingInfo)
        {
            List<string> definedAssemblies = new List<string>();
            definedAssemblies.Add("System.dll"); // библиотека для работы множества основных функций
            definedAssemblies.Add("System.Drawing.dll"); // для работы команд вывода в лог
            definedAssemblies.Add(Assembly.GetExecutingAssembly().Location); // для взаимодействия с программой
            foreach (string defineAssembly in scriptParsingInfo.DefinedAssemblyList) // дополнительные библиотеки, указанные в #define
            {
                definedAssemblies.Add(defineAssembly);
            }
            return definedAssemblies.ToArray();
        }
    }
}
