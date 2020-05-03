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

        public bool NeedShowGUI { get; private set; }

        public bool AutoCloseGUI { get; private set; }

        public bool Finished { get; private set; }

        public int ExitCode { get; private set; }

        public ReadOnlyCollection<LogItem> LogItems => logItems.AsReadOnly();



        private readonly List<LogItem> logItems = new List<LogItem>();

        private readonly InputArgumentsInfo inputArguments = new InputArgumentsInfo();

        private readonly Dictionary<string, Assembly> resolvedAssemblies = new Dictionary<string, Assembly>();

        private readonly Queue<Process> managedProcesses = new Queue<Process>();

        private Thread executingThread;

        private readonly bool startDebugScript;



        public event Action FinishedEvent;

        public event Action ShowGUIEvent;

        public event Action<LogItem> AddLogEvent;



        public ProgramModel()
        {
            startDebugScript = true;
            GUIMode = true;
        }

        public ProgramModel(string[] args)
        {
            ParseInputArguments(args);
            GUIMode = !inputArguments.HideMode || inputArguments.IsEmpty || inputArguments.WaitMode;
            AutoCloseGUI = !(inputArguments.IsEmpty || inputArguments.WaitMode);
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
            LogItem item = new LogItem(text, foreColor);
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
                    OnShowGUI();
                    ExitCode = 0;
                }
                else
                {
                    if (!inputArguments.HideMode)
                    {
                        OnShowGUI();
                    }

                    string script = GetScriptText();

                    WriteLogStartInfo();

                    // после загрузки содержимого скрипта переключаемся на его рабочую директорию вместо рабочей директории программы
                    // (для возможности указания коротких путей к файлам, в т.ч. загружаемым в скрипте сборкам)
                    Environment.CurrentDirectory = GetScriptDirectoryPath();

                    ScriptParsingInfo scriptParsingInfo = ParseScriptInfo(script);
                    LoadAssembliesForResolve(scriptParsingInfo);
                    CompilerResults compilerResults = Compile(scriptParsingInfo);

                    if (compilerResults.Errors.Count == 0)
                    {
                        ScriptRuntime scriptRuntime = GetCompiledScript(compilerResults);
                        ExitCode = scriptRuntime.StartScript(inputArguments.ScriptArgument);
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

            if (GUIMode)
            {
                OnShowGUI();
            }

            FinishedEvent?.Invoke();
        }

        private void StartDebugScript()
        {
#if DEBUG
            try
            {
                OnShowGUI();

                DebugScript debugScript = new DebugScript();
                debugScript.StartScript(null);
                ExitCode = debugScript.StartScript(inputArguments.ScriptArgument);
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
                    else if (preparedArg == "/w" || preparedArg == "/wait")
                    {
                        inputArguments.WaitMode = true;
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

        private string GetScriptPath()
        {
            if (string.IsNullOrEmpty(inputArguments.ScriptPath))
            {
                throw new Exception("Отсутствует путь к скрипту.");
            }

            string scriptPath = Path.GetFullPath(inputArguments.ScriptPath);

            if (!File.Exists(scriptPath))
            {
                throw new Exception("Скрипт по указанному пути не найден.");
            }

            return scriptPath;
        }

        private string GetScriptDirectoryPath()
        {
            string scriptPath = GetScriptPath();
            return Path.GetDirectoryName(scriptPath);
        }

        private string GetScriptText()
        {
            string scriptPath = GetScriptPath();
            string script = File.ReadAllText(scriptPath, Encoding.UTF8);
            return script;
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

        private void WriteLogStartInfo()
        {
            WriteLineLog($"## {GetScriptPath()}", Settings.Default.InfoColor);
            WriteLineLog($"## {DateTime.Now}", Settings.Default.InfoColor);
            WriteLineLog();
        }

        private void WriteLogException(Exception ex)
        {
            WriteLineLog($"# Ошибка: {ex.Message}", Settings.Default.ErrorColor);
        }

        private void WriteLogCompileErrors(CompilerResults compilerResults)
        {
            WriteLineLog($"# Ошибок: {compilerResults.Errors.Count}", Settings.Default.ErrorColor);
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
                int lineNumber = i + 1;

                WriteLog(lineNumber.ToString().PadLeft(4) + ": ");
                if (errorLines.Contains(lineNumber))
                {
                    WriteLineLog(lines[i], Settings.Default.ErrorColor);
                }
                else
                {
                    WriteLineLog(lines[i], Settings.Default.SourceCodeColor);
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

        private void OnShowGUI()
        {
            if (!NeedShowGUI)
            {
                NeedShowGUI = true;
                ShowGUIEvent?.Invoke();
            }
        }



        private static ScriptParsingInfo ParseScriptInfo(string scriptText)
        {
            List<string> defineList = new List<string>();
            List<string> usingList = new List<string>();

            StringBuilder functionBlock = new StringBuilder();
            StringBuilder classBlock = new StringBuilder();
            StringBuilder namespaceBlock = new StringBuilder();

            StringBuilder currentBlock = functionBlock;

            string[] opLines = scriptText.Split(new string[] { ";" }, StringSplitOptions.None);
            for (int i = 0; i < opLines.Length; i++)
            {
                string opLine = opLines[i];
                string[] lines = opLine.Split(new string[] { "\r\n" }, StringSplitOptions.None);
                for (int j = 0; j < lines.Length; j++)
                {
                    string line = lines[j];
                    string trimLine = line.TrimStart();
                    if (trimLine.Length > 0)
                    {
                        if (trimLine.StartsWith("#")) // служебные конструкции
                        {
                            if (trimLine.StartsWith("#define"))
                            {
                                string preparedLine = line.Replace("#define", "").Trim();
                                defineList.Add(preparedLine);
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
                if (i < opLines.Length - 1)
                {
                    currentBlock.Append(';');
                }
            }
            return new ScriptParsingInfo()
            {
                DefineList = defineList.ToArray(),
                UsingBlock = string.Join("\r\n", usingList.ToArray()),
                FunctionBlock = functionBlock.ToString(),
                ClassBlock = classBlock.ToString(),
                NamespaceBlock = namespaceBlock.ToString(),
            };
        }

        private static string GetSourceCode(ScriptParsingInfo scriptParsingInfo)
        {
            string source = Resources.SourceCodeTemplate;
            source = source.Replace("##using##", scriptParsingInfo.UsingBlock);
            source = source.Replace("##function##", scriptParsingInfo.FunctionBlock);
            source = source.Replace("##class##", scriptParsingInfo.ClassBlock);
            source = source.Replace("##namespace##", scriptParsingInfo.NamespaceBlock);
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
            foreach (string defineAssembly in scriptParsingInfo.DefineList) // дополнительные библиотеки, указанные в #define
            {
                definedAssemblies.Add(defineAssembly);
            }
            return definedAssemblies.ToArray();
        }
    }
}
