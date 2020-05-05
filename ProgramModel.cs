using CSScript.Properties;
using Microsoft.CSharp;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;

namespace CSScript
{
    internal class ProgramModel : IDisposable
    {
        public bool GUIMode { get; private set; }

        public bool Finished { get; private set; }

        public int ExitCode { get; private set; }

        public ReadOnlyCollection<LogItem> LogItems => logItems.AsReadOnly();



        private readonly List<LogItem> logItems = new List<LogItem>();

        private readonly InputArgumentsInfo inputArguments = new InputArgumentsInfo();

        private readonly Dictionary<string, Assembly> resolvedAssemblies = new Dictionary<string, Assembly>();

        private readonly List<Process> managedProcesses = new List<Process>();

        private readonly Settings settings;

        private Thread executingThread;



        public delegate void AddLogEventHandler(object sender, LogItem logItem);

        public event AddLogEventHandler AddLogEvent;

        public delegate void FinishedEventHandler(object sender, bool guiForceExit);

        public event FinishedEventHandler FinishedEvent;



        public ProgramModel(Settings settings, string[] args)
        {
            this.settings = settings;
            ParseInputArguments(args);
            GUIMode = inputArguments.IsEmpty || !inputArguments.HideMode;
        }



        public void ExecuteScriptAsync()
        {
            executingThread = inputArguments.StartDebugScript ? new Thread(StartDebugScript) : new Thread(StartScript);
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
                managedProcesses.Add(process);
            }
            return process;
        }

        public void WriteLog(string text, Color? foreColor = null)
        {
            if (!string.IsNullOrEmpty(text))
            {
                LogItem item = new LogItem(text, DateTime.Now, foreColor);
                lock (logItems)
                {
                    logItems.Add(item);
                }
                AddLogEvent?.Invoke(this, item);
            }
        }

        public void WriteLineLog(string text, Color? foreColor = null)
        {
            WriteLog(text + Environment.NewLine, foreColor);
        }

        public void WriteLineLog()
        {
            WriteLog(Environment.NewLine);
        }

        public void Dispose()
        {
            KillManagedProcesses();
        }



        private void StartScript()
        {
            bool scriptGUIForceExit = false;
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
                    // (для возможности указания относительных путей к файлам)
                    Environment.CurrentDirectory = GetWorkDirectoryPath(scriptPath);

                    ScriptParsingInfo scriptParsingInfo = ParseScriptInfo(scriptText, scriptPath);
                    LoadAssembliesForResolve(scriptParsingInfo);
                    CompilerResults compilerResults = Compile(scriptParsingInfo);

                    if (compilerResults.Errors.Count == 0)
                    {
                        ScriptContainer scriptContainer = GetCompiledScript(compilerResults, scriptParsingInfo);
                        scriptContainer.StartScript(inputArguments.ScriptArguments.ToArray());
                        ExitCode = scriptContainer.ExitCode;
                        scriptGUIForceExit = scriptContainer.GUIForceExit;
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
            WriteLineLog($"# Выполнено с кодом возврата: {ExitCode}", settings.InfoColor);

            if (inputArguments != null && !string.IsNullOrEmpty(inputArguments.LogPath))
            {
                try
                {
                    SaveLogs(inputArguments.LogPath);
                }
                catch (Exception ex)
                {
                    WriteLineLog($"# Не удалось сохранить лог: {ex.Message}", settings.ErrorColor);
                }
            }

            Finished = true;
            bool guiForceExit = GUIMode && scriptGUIForceExit;
            FinishedEvent?.Invoke(this, guiForceExit);
        }

        private void StartDebugScript()
        {
#if DEBUG
            try
            {
                DebugScript debugScript = new DebugScript();
                debugScript.StartScript(inputArguments.ScriptArguments.ToArray());
                ExitCode = debugScript.ExitCode;
            }
            catch (Exception ex)
            {
                WriteLogException(ex);
                ExitCode = 1;
            }

            WriteLineLog();
            WriteLineLog($"# Выполнено с кодом возврата: {ExitCode}", settings.InfoColor);

            Finished = true;
            FinishedEvent?.Invoke(this, false);
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
                string currentArgument = null;
                for (int i = 0; i < args.Length; i++)
                {
                    string arg = args[i];
                    string preparedArg = arg.Trim().ToLower();
                    if (preparedArg == "/h" || preparedArg == "/hide")
                    {
                        inputArguments.HideMode = true;
                        currentArgument = null;
                    }
                    else if (preparedArg == "/l" || preparedArg == "/log")
                    {
                        inputArguments.LogPath = args[++i];
                        currentArgument = null;
                    }
                    else if (preparedArg == "/a" || preparedArg == "/arg")
                    {
                        currentArgument = "a";
                    }
                    else if (preparedArg == "/debug")
                    {
                        inputArguments.StartDebugScript = true;
                        currentArgument = null;
                    }
                    else
                    {
                        if (currentArgument == null && inputArguments.ScriptPath == null)
                        {
                            inputArguments.ScriptPath = arg;
                        }
                        else if (currentArgument == "a")
                        {
                            inputArguments.ScriptArguments.Add(arg);
                        }
                    }
                }
            }
        }

        private void LoadAssembliesForResolve(ScriptParsingInfo scriptParsingInfo)
        {
            string currentAssembly = Assembly.GetExecutingAssembly().Location;

            string[] assemblies = GetReferencedAssemblies(scriptParsingInfo);
            foreach (string assembly in assemblies)
            {
                if (File.Exists(assembly) && !string.Equals(assembly, currentAssembly, StringComparison.OrdinalIgnoreCase))
                {
                    // загружаются только те сборки, которые рантайм не может подгрузить автоматически
                    Assembly loadedAssembly = Assembly.LoadFrom(assembly);
                    resolvedAssemblies.Add(loadedAssembly.FullName, loadedAssembly);
                }
            }
        }

        private ScriptContainer GetCompiledScript(CompilerResults compilerResults, ScriptParsingInfo scriptParsingInfo)
        {
            Assembly compiledAssembly = compilerResults.CompiledAssembly;
            object instance = compiledAssembly.CreateInstance("CSScript.CompiledScript",
                false,
                BindingFlags.Public | BindingFlags.Instance,
                null,
                new object[] { scriptParsingInfo.ScriptPath, settings },
                CultureInfo.CurrentCulture,
                null);
            return (ScriptContainer)instance;
        }

        private void WriteLogHelpInfo()
        {
            string[] lines = Resources.HelpText.Split(new string[] { "\r\n" }, StringSplitOptions.None);
            for (int i = 0; i < lines.Length; i++)
            {
                Color? color = null;
                string[] line = ParseHelpLine(lines[i]);
                switch (line[0])
                {
                    case "c": color = settings.CaptionColor; break;
                    case "i": color = settings.InfoColor; break;
                }
                WriteLineLog(line[1], color);
            }
        }

        private void WriteLogStartInfo(string scriptPath)
        {
            WriteLineLog($"## {scriptPath}", settings.InfoColor);
            WriteLineLog($"## {DateTime.Now}", settings.InfoColor);
            WriteLineLog();
        }

        private void WriteLogException(Exception ex)
        {
            WriteLineLog($"# Ошибка: {ex.Message}", settings.ErrorColor);
            foreach (string stackTraceLine in ex.StackTrace.Split(new string[] { Environment.NewLine }, StringSplitOptions.None))
            {
                WriteLineLog("#" + stackTraceLine, settings.StackTraceColor);
            }
        }

        private void WriteLogCompileErrors(CompilerResults compilerResults)
        {
            WriteLineLog($"# Ошибок компиляции: {compilerResults.Errors.Count}", settings.ErrorColor);
            int errorNumber = 1;
            foreach (CompilerError error in compilerResults.Errors)
            {
                if (error.Line > 0)
                {
                    WriteLineLog($"# {errorNumber++} (cтрока {error.Line}): {error.ErrorText}", settings.ErrorColor);
                }
                else
                {
                    WriteLineLog($"# {errorNumber++}: {error.ErrorText}", settings.ErrorColor);
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
                    WriteLineLog(line, settings.ErrorColor);
                }
                else
                {
                    if (line.TrimStart().StartsWith("//"))
                    {
                        WriteLineLog(line, settings.CommentColor);
                    }
                    else
                    {
                        WriteLineLog(line, settings.SourceCodeColor);
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

        private void KillManagedProcesses()
        {
            lock (managedProcesses)
            {
                for (int i = 0; i < managedProcesses.Count; i++)
                {
                    try
                    {
                        managedProcesses[i].Kill();
                    }
                    catch
                    {
                    }
                }
            }
        }



        private static ScriptParsingInfo ParseScriptInfo(string scriptText, string scriptPath)
        {
            ScriptParsingInfo scriptParsingInfo = new ScriptParsingInfo
            {
                ScriptPath = scriptPath
            };

            List<string> definedScriptsList = new List<string>();
            StringBuilder currentBlock = scriptParsingInfo.ProcedureBlock;

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
                                scriptParsingInfo.DefinedAssemblyList.Add(preparedLine);
                            }

                            else if (trimLine.StartsWith("#using"))
                            {
                                string preparedLine = line.Replace("#", "").Trim() + ";";
                                scriptParsingInfo.UsingList.Add(preparedLine);
                            }
                            else if (trimLine.StartsWith("#class"))
                            {
                                currentBlock = scriptParsingInfo.ClassBlock;
                                continue;
                            }
                            else if (trimLine.StartsWith("#ns") || trimLine.StartsWith("#namespace"))
                            {
                                currentBlock = scriptParsingInfo.NamespaceBlock;
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
            for (int i = 0; i < scriptParsingInfo.DefinedAssemblyList.Count; i++)
            {
                scriptParsingInfo.DefinedAssemblyList[i]
                    = GetCorrectAssemblyPath(scriptParsingInfo.DefinedAssemblyList[i], scriptWorkDirectory);
            }

            // слияние с другими скриптами, включённые в текущий скрипт
            foreach (string definedScript in definedScriptsList)
            {
                MergeScripts(scriptParsingInfo, definedScript);
            }

            // удаление дублирующихся позиций сборок
            DeleteDuplicates(scriptParsingInfo.DefinedAssemblyList,
                (x, y) => string.Equals(x, y, StringComparison.OrdinalIgnoreCase));

            // удаление дублирующихся конструкций using
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

        private static string[] GetReferencedAssemblies(ScriptParsingInfo scriptParsingInfo)
        {
            List<string> definedAssemblies = new List<string>();
            definedAssemblies.Add("System.dll"); // библиотека для работы множества основных функций
            definedAssemblies.Add("System.Drawing.dll"); // для работы команд вывода информации в лог
            definedAssemblies.Add(Assembly.GetExecutingAssembly().Location); // для взаимодействия с программой, запускающей скрипт
            foreach (string defineAssembly in scriptParsingInfo.DefinedAssemblyList) // дополнительные библиотеки, указанные в #define
            {
                definedAssemblies.Add(defineAssembly);
            }
            return definedAssemblies.ToArray();
        }

        private static string[] ParseHelpLine(string line)
        {
            if (line.StartsWith("`"))
            {
                int index = line.IndexOf("`", 1);
                return new string[]
                {
                    line.Substring(1, index - 1),
                    line.Substring(index + 1),
                };
            }
            else
            {
                return new string[] { null, line };
            }
        }
    }
}
