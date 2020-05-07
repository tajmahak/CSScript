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
    /// <summary>
    /// Представляет модель программы.
    /// </summary>
    internal class ProgramModel : IScriptEnvironment, IDisposable
    {
        public bool GUIMode { get; private set; }

        public bool GUIForceExit { get; private set; }

        public bool Finished { get; private set; }

        public int ExitCode { get; private set; }

        public Settings Settings { get; private set; }

        public string ScriptPath { get; private set; }

        public ReadOnlyCollection<Message> MessageList => messageList.AsReadOnly();

        private Thread executingThread;

        private readonly List<Message> messageList = new List<Message>();

        private readonly InputArgumentsInfo inputArguments;

        private readonly Dictionary<string, Assembly> resolvedAssemblies = new Dictionary<string, Assembly>();

        private readonly List<Process> managedProcesses = new List<Process>();



        public delegate void AddMessageEventHandler(object sender, Message message);

        public event AddMessageEventHandler AddMessageEvent;

        public delegate void FinishedEventHandler(object sender);

        public event FinishedEventHandler FinishedEvent;



        public ProgramModel(Settings settings, string[] args)
        {
            Settings = settings;
            inputArguments = InputArgumentsInfo.Parse(args);
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

        public void WriteMessage(object value, Color? foreColor = null)
        {
            if (value != null)
            {
                string text = value.ToString();
                if (!string.IsNullOrEmpty(text))
                {
                    Message message = new Message(text, DateTime.Now, foreColor);
                    lock (messageList)
                    {
                        messageList.Add(message);
                    }
                    AddMessageEvent?.Invoke(this, message);
                }
            }
        }

        public void WriteMessageLine(object value, Color? foreColor = null)
        {
            WriteMessage(value + Environment.NewLine, foreColor);
        }

        public void WriteMessageLine()
        {
            WriteMessage(Environment.NewLine);
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
                    WriteHelpInfoMessage();
                    ExitCode = 0;
                }
                else
                {
                    ScriptPath = GetAndCheckFullPath(inputArguments.ScriptPath, Environment.CurrentDirectory);
                    string scriptText = GetScriptText(ScriptPath);

                    WriteStartInfoMessage(ScriptPath);

                    // после загрузки содержимого скрипта переключаемся на его рабочую директорию вместо рабочей директории программы
                    // (для возможности указания относительных путей к файлам)
                    Environment.CurrentDirectory = GetWorkDirectoryPath(ScriptPath);

                    ScriptInfo scriptInfo = ParseScriptInfo(scriptText, ScriptPath);
                    LoadAssembliesForResolve(scriptInfo);
                    CompilerResults compilerResults = Compile(scriptInfo);

                    if (compilerResults.Errors.Count == 0)
                    {
                        IScriptEnvironment scriptEnvironment = CreateScriptEnvironment(ScriptPath);
                        ScriptContainer scriptContainer = CreateCompiledScriptContainer(compilerResults, scriptEnvironment);
                        scriptContainer.Execute(inputArguments.ScriptArguments.ToArray());
                        ExitCode = scriptContainer.ExitCode;
                        scriptGUIForceExit = scriptContainer.GUIForceExit;
                    }
                    else
                    {
                        WriteCompileErrorsMessage(compilerResults);
                        string sourceCode = GetSourceCode(scriptInfo);

                        WriteMessageLine();

                        WriteSourceCodeMessage(sourceCode, compilerResults);
                        ExitCode = 1;
                    }
                }
            }
            catch (Exception ex)
            {
                WriteExceptionMessage(ex);
                ExitCode = 1;
            }

            WriteMessageLine();
            WriteMessageLine($"# Выполнено с кодом возврата: {ExitCode}", Settings.InfoColor);

            if (inputArguments != null && !string.IsNullOrEmpty(inputArguments.LogPath))
            {
                try
                {
                    SaveMessageLog(inputArguments.LogPath);
                }
                catch (Exception ex)
                {
                    WriteMessageLine($"# Не удалось сохранить лог: {ex.Message}", Settings.ErrorColor);
                }
            }

            Finished = true;
            GUIForceExit = GUIMode && scriptGUIForceExit;
            FinishedEvent?.Invoke(this);
        }

        private void StartDebugScript()
        {
#if DEBUG
            try
            {
                IScriptEnvironment scriptEnvironment = CreateScriptEnvironment(null);
                ScriptContainer debugScript = CreateDebugScriptContainer(scriptEnvironment);
                debugScript.Execute(inputArguments.ScriptArguments.ToArray());
                ExitCode = debugScript.ExitCode;
            }
            catch (Exception ex)
            {
                WriteExceptionMessage(ex);
                ExitCode = 1;
            }

            WriteMessageLine();
            WriteMessageLine($"# Выполнено с кодом возврата: {ExitCode}", Settings.InfoColor);

            Finished = true;
            FinishedEvent?.Invoke(this);
#endif
        }

        private ScriptContainer CreateCompiledScriptContainer(CompilerResults compilerResults, IScriptEnvironment scriptEnvironment)
        {
            Assembly compiledAssembly = compilerResults.CompiledAssembly;
            object instance = compiledAssembly.CreateInstance("CSScript.CompiledScript",
                false,
                BindingFlags.Public | BindingFlags.Instance,
                null,
                new object[] { scriptEnvironment },
                CultureInfo.CurrentCulture,
                null);
            return (ScriptContainer)instance;
        }

        private ScriptContainer CreateDebugScriptContainer(IScriptEnvironment scriptEnvironment)
        {
            DebugScriptStand debugScript = new DebugScriptStand(scriptEnvironment);
            return debugScript;
        }

        private CompilerResults Compile(ScriptInfo scriptInfo)
        {
            string[] referencedAssemblies = GetReferencedAssemblies(scriptInfo, true);
            string sourceCode = GetSourceCode(scriptInfo);

            using (CSharpCodeProvider provider = new CSharpCodeProvider())
            {
                CompilerParameters compileParameters = new CompilerParameters(referencedAssemblies)
                {
                    GenerateInMemory = true,
                    GenerateExecutable = false,
                };
                CompilerResults compilerResults = provider.CompileAssemblyFromSource(compileParameters, sourceCode);
                return compilerResults;
            }
        }

        private ScriptInfo ParseScriptInfo(string scriptText, string scriptPath)
        {
            ScriptInfo scriptInfo = new ScriptInfo(scriptPath);

            StringBuilder currentBlock = scriptInfo.ProcedureBlock;

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
                                scriptInfo.DefinedScriptList.Add(preparedLine);
                            }
                            else if (trimLine.StartsWith("#define"))
                            {
                                string preparedLine = line.Replace("#define", "").Trim();
                                scriptInfo.DefinedAssemblyList.Add(preparedLine);
                            }

                            else if (trimLine.StartsWith("#using"))
                            {
                                string preparedLine = line.Replace("#", "").Trim() + ";";
                                scriptInfo.UsingList.Add(preparedLine);
                            }
                            else if (trimLine.StartsWith("#class"))
                            {
                                currentBlock = scriptInfo.ClassBlock;
                                continue;
                            }
                            else if (trimLine.StartsWith("#ns") || trimLine.StartsWith("#namespace"))
                            {
                                currentBlock = scriptInfo.NamespaceBlock;
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
            for (int i = 0; i < scriptInfo.DefinedAssemblyList.Count; i++)
            {
                scriptInfo.DefinedAssemblyList[i]
                    = GetCorrectAssemblyPath(scriptInfo.DefinedAssemblyList[i], scriptWorkDirectory);
            }

            if (scriptInfo.DefinedScriptList.Count == 0)
            {
                return scriptInfo;
            }
            else
            {
                // слияние с другими скриптами, включённые в текущий скрипт
                ScriptInfo mergedScriptInfo = scriptInfo;
                foreach (string definedScriptPath in scriptInfo.DefinedScriptList)
                {
                    mergedScriptInfo = MergeScripts(mergedScriptInfo, definedScriptPath);
                }
                return mergedScriptInfo;
            }
        }

        private ScriptInfo MergeScripts(ScriptInfo scriptInfo, string definedScriptPath)
        {
            string definedScriptFullPath = GetAndCheckFullPath(definedScriptPath, GetWorkDirectoryPath(scriptInfo.ScriptPath));
            string definedScriptText = GetScriptText(definedScriptFullPath);
            ScriptInfo definedScriptInfo = ParseScriptInfo(definedScriptText, definedScriptFullPath);

            return MergeScripts(scriptInfo, definedScriptInfo);
        }

        private ScriptInfo MergeScripts(ScriptInfo mainScriptInfo, ScriptInfo definedScriptInfo)
        {
            ScriptInfo mergedScriptInfo = new ScriptInfo(mainScriptInfo.ScriptPath);

            mergedScriptInfo.DefinedAssemblyList.AddRange(mainScriptInfo.DefinedAssemblyList);
            mergedScriptInfo.DefinedAssemblyList.AddRange(definedScriptInfo.DefinedAssemblyList);

            // удаление дублирующихся позиций сборок
            Utils.DeleteDuplicates(mergedScriptInfo.DefinedAssemblyList,
                (x, y) => string.Equals(x, y, StringComparison.OrdinalIgnoreCase));

            mergedScriptInfo.UsingList.AddRange(mainScriptInfo.UsingList);
            mergedScriptInfo.UsingList.AddRange(definedScriptInfo.UsingList);

            // удаление дублирующихся конструкций using
            Utils.DeleteDuplicates(mergedScriptInfo.UsingList,
                (x, y) => string.Equals(x, y));

            mergedScriptInfo.ProcedureBlock.Append(mainScriptInfo.ProcedureBlock);

            mergedScriptInfo.ClassBlock.Append(mainScriptInfo.ClassBlock);
            if (definedScriptInfo.ClassBlock.Length > 0)
            {
                mergedScriptInfo.ClassBlock.AppendLine();
                mergedScriptInfo.ClassBlock.AppendLine("// <<< " + definedScriptInfo.ScriptPath);
                mergedScriptInfo.ClassBlock.AppendLine(definedScriptInfo.ClassBlock.ToString());
                mergedScriptInfo.ClassBlock.AppendLine("// >>> " + definedScriptInfo.ScriptPath);
            }

            mergedScriptInfo.NamespaceBlock.Append(mainScriptInfo.NamespaceBlock);
            if (definedScriptInfo.NamespaceBlock.Length > 0)
            {
                mergedScriptInfo.NamespaceBlock.AppendLine();
                mergedScriptInfo.NamespaceBlock.AppendLine("// <<< " + definedScriptInfo.ScriptPath);
                mergedScriptInfo.NamespaceBlock.AppendLine(definedScriptInfo.NamespaceBlock.ToString());
                mergedScriptInfo.NamespaceBlock.AppendLine("// >>> " + definedScriptInfo.ScriptPath);
            }

            return mergedScriptInfo;
        }

        private IScriptEnvironment CreateScriptEnvironment(string scriptPath)
        {
            return this;
        }

        private void LoadAssembliesForResolve(ScriptInfo scriptInfo)
        {
            string[] assemblies = GetReferencedAssemblies(scriptInfo, false);
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

        private string[] GetReferencedAssemblies(ScriptInfo scriptInfo, bool includeCurrentAssembly)
        {
            List<string> definedAssemblies = new List<string>();
            definedAssemblies.Add("System.dll"); // библиотека для работы множества основных функций
            definedAssemblies.Add("System.Drawing.dll"); // для работы команд вывода информации в лог
            if (includeCurrentAssembly)
            {
                definedAssemblies.Add(Assembly.GetExecutingAssembly().Location); // для взаимодействия с программой, запускающей скрипт
            }
            foreach (string definedAssemblyPath in scriptInfo.DefinedAssemblyList) // дополнительные библиотеки, указанные в #define
            {
                definedAssemblies.Add(definedAssemblyPath);
            }
            return definedAssemblies.ToArray();
        }

        private string GetCorrectAssemblyPath(string assemblyPath, string workDirectory)
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

        private string GetSourceCode(ScriptInfo scriptInfo)
        {
            string source = Resources.SourceCodeTemplate;
            source = source.Replace("##using##", string.Join("\r\n", scriptInfo.UsingList.ToArray()));
            source = source.Replace("##procedure##", scriptInfo.ProcedureBlock.ToString());
            source = source.Replace("##class##", scriptInfo.ClassBlock.ToString());
            source = source.Replace("##namespace##", scriptInfo.NamespaceBlock.ToString());
            return source;
        }

        private string GetScriptText(string scriptPath)
        {
            return File.ReadAllText(scriptPath, Encoding.UTF8);
        }

        private string GetAndCheckFullPath(string path, string workDirectory)
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

        private string GetWorkDirectoryPath(string path)
        {
            return Path.GetDirectoryName(path);
        }

        private string[] ParseHelpInfoMessageLine(string line)
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

        private void WriteHelpInfoMessage()
        {
            string[] lines = Resources.HelpText.Split(new string[] { "\r\n" }, StringSplitOptions.None);
            for (int i = 0; i < lines.Length; i++)
            {
                Color? color = null;
                string[] line = ParseHelpInfoMessageLine(lines[i]);
                switch (line[0])
                {
                    case "c": color = Settings.CaptionColor; break;
                    case "i": color = Settings.InfoColor; break;
                }
                WriteMessageLine(line[1], color);
            }
        }

        private void WriteStartInfoMessage(string scriptPath)
        {
            WriteMessageLine($"## {scriptPath}", Settings.InfoColor);
            WriteMessageLine($"## {DateTime.Now}", Settings.InfoColor);
            WriteMessageLine();
        }

        private void WriteExceptionMessage(Exception ex)
        {
            WriteMessageLine($"# Ошибка: {ex.Message}", Settings.ErrorColor);
            foreach (string stackTraceLine in ex.StackTrace.Split(new string[] { Environment.NewLine }, StringSplitOptions.None))
            {
                WriteMessageLine("#" + stackTraceLine, Settings.StackTraceColor);
            }
        }

        private void WriteCompileErrorsMessage(CompilerResults compilerResults)
        {
            WriteMessageLine($"# Ошибок компиляции: {compilerResults.Errors.Count}", Settings.ErrorColor);
            int errorNumber = 1;
            foreach (CompilerError error in compilerResults.Errors)
            {
                if (error.Line > 0)
                {
                    WriteMessageLine($"# {errorNumber++} (cтрока {error.Line}): {error.ErrorText}", Settings.ErrorColor);
                }
                else
                {
                    WriteMessageLine($"# {errorNumber++}: {error.ErrorText}", Settings.ErrorColor);
                }
            }
        }

        private void WriteSourceCodeMessage(string sourceCode, CompilerResults compilerResults = null)
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

                WriteMessage(lineNumber.ToString().PadLeft(4) + ": ");
                if (errorLines.Contains(lineNumber))
                {
                    WriteMessageLine(line, Settings.ErrorColor);
                }
                else
                {
                    if (line.TrimStart().StartsWith("//"))
                    {
                        WriteMessageLine(line, Settings.CommentColor);
                    }
                    else
                    {
                        WriteMessageLine(line, Settings.SourceCodeColor);
                    }
                }
            }
        }

        private string GetMessageLog()
        {
            StringBuilder messageLog = new StringBuilder();
            foreach (Message message in messageList)
            {
                messageLog.Append(message.Text);
            }
            return messageLog.ToString();
        }

        private void SaveMessageLog(string logPath)
        {
            string messageLog = GetMessageLog();
            using (StreamWriter writer = new StreamWriter(logPath, true, Encoding.UTF8))
            {
                writer.WriteLine(messageLog);
                writer.WriteLine();
                writer.WriteLine();
            }
        }
    }
}
