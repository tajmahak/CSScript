#define DEBUG_USE_SCRIPT_STAND // использовать стенд 'DebugScriptStand' для отладки скриптов
#define DEBUG_SKIP_EXCEPTION_HANDLING // обрабатывать исключения программы в отладчике

using CSScript.Properties;
using Microsoft.CSharp;
using System;
using System.CodeDom.Compiler;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Security.Principal;
using System.Text;
using System.Threading;

namespace CSScript
{
    /// <summary>
    /// Представляет модель программы.
    /// </summary>
    internal class ProgramModel : IDisposable
    {
        public ProgramModel(InputArgumentsInfo inputArguments, Settings settings)
        {
            MessageManager = new MessageManager(this);
            ProcessManager = new ProcessManager();
            assemblyManager = new AssemblyManager();

            Settings = settings;
            this.inputArguments = inputArguments;

            HideMode = !inputArguments.IsEmpty && inputArguments.HideMode;
        }


        public int ExitCode { get; private set; }

        public bool HideMode { get; private set; }

        public bool Finished { get; private set; }

        public Settings Settings { get; private set; }

        public MessageManager MessageManager { get; private set; }

        public ProcessManager ProcessManager { get; private set; }

        private readonly AssemblyManager assemblyManager;

        private readonly InputArgumentsInfo inputArguments;

        private bool needAutoClose;


        public delegate void FinishedEventHandler(object sender, bool autoClose);

        public delegate string InputTextEventHandler(object sender, string caption);

        public event FinishedEventHandler FinishedEvent;

        public event InputTextEventHandler InputTextEvent;


        public Thread StartAsync()
        {
            Thread executingThread = new Thread(Start);
            executingThread.IsBackground = true;
            executingThread.Start();
            return executingThread;
        }

        public Assembly ResolveAssembly(string assemblyName)
        {
            return assemblyManager.ResolveAssembly(assemblyName);
        }

        public string GetInputText(string caption)
        {
            if (!HideMode)
            {
                return InputTextEvent.Invoke(this, caption);
            }
            return null;
        }

        public void Dispose()
        {
            ProcessManager.KillManagedProcesses();
        }


        private void Start()
        {
            try
            {
#if DEBUG_USE_SCRIPT_STAND && DEBUG
                ExecuteScript();
#else
                if (inputArguments.IsEmpty)
                {
                    WriteProgramInformation();
                }
                else if (inputArguments.RegisterMode)
                {
                    RegistryProgram();
                }
                else if (inputArguments.UnregisterMode)
                {
                    UnregistryProgram();
                }
                else
                {
                    ExecuteScript();
                }
#endif
            }
#if !DEBUG_SKIP_EXCEPTION_HANDLING || !DEBUG
            catch (Exception ex)
            {
                MessageManager.WriteException(ex);
                ExitCode = 1;
            }
#endif
            finally
            {
                MessageManager.WriteExitCode(ExitCode);

                MessageManager.SaveLog(inputArguments?.LogPath);

                Finished = true;
                FinishedEvent?.Invoke(this, needAutoClose && !HideMode);
            }
        }

        private void WriteProgramInformation()
        {
            MessageManager.WriteHelpInfo();
            ExitCode = 0;
        }

        private void RegistryProgram()
        {
            if (HasAdministativePrivilegies())
            {
                MessageManager.WriteLine("Регистрация программы в реестре...");

                RegistryManager.RegisterFileAssociation();

                string shellExtensionAssemblyPath =
                    PathManager.FromLocalDirectoryPath("CSScript.ShellExtension.dll");
                if (File.Exists(shellExtensionAssemblyPath))
                {
                    MessageManager.WriteLine("Регистрация расширения оболочки...");
                    Assembly shellExtensionAssembly = Assembly.LoadFrom(shellExtensionAssemblyPath);
                    RegistryManager.RegisterShellExtension(shellExtensionAssembly);
                }
                MessageManager.WriteLine("Перезапуск 'Проводник'...");
                ProcessManager.RestartWindowsExplorer();

                MessageManager.WriteLine("Успешно", Settings.SuccessColor);
            }
            else
            {
                throw new Exception("Для работы с реестром необходимы права администратора.");
            }
        }

        private void UnregistryProgram()
        {
            if (HasAdministativePrivilegies())
            {
                MessageManager.WriteLine("Удаление регистрации программы в реестре...");

                RegistryManager.UnregisterFileAssociation();

                string shellExtensionAssemblyPath = "CSScript.ShellExtension.dll";
                if (File.Exists(shellExtensionAssemblyPath))
                {
                    MessageManager.WriteLine("Удаление регистрации расширения оболочки...");
                    Assembly shellExtensionAssembly = Assembly.LoadFrom(shellExtensionAssemblyPath);
                    RegistryManager.UnregisterShellExtension(shellExtensionAssembly);
                }
                MessageManager.WriteLine("Перезапуск 'Проводник'...");
                ProcessManager.RestartWindowsExplorer();

                MessageManager.WriteLine("Успешно", Settings.SuccessColor);
            }
            else
            {
                throw new Exception("Для работы с реестром необходимы права администратора.");
            }
        }

        private void ExecuteScript()
        {
            IScriptEnvironment scriptEnvironment;

#if DEBUG_USE_SCRIPT_STAND && DEBUG

            scriptEnvironment = CreateScriptEnvironment(null, inputArguments.ScriptArguments.ToArray());
            ScriptContainer debugScript = new _DebugScriptStand(scriptEnvironment);
            debugScript.Execute();

#else

            string scriptPath = PathManager.GetAndCheckFullPath(inputArguments.ScriptPath, Environment.CurrentDirectory);
            MessageManager.WriteStartInfo(scriptPath);

            // после загрузки содержимого скрипта переключаемся на его рабочую директорию вместо рабочей директории программы
            // (для возможности указания относительных путей к файлам)
            Environment.CurrentDirectory = PathManager.GetWorkDirectoryPath(scriptPath);

            scriptEnvironment = CreateScriptEnvironment(scriptPath, inputArguments.ScriptArguments.ToArray());

            string scriptText = GetScriptText(scriptPath);

            ScriptInfo scriptInfo = ParseScriptInfo(scriptText, scriptPath);
            assemblyManager.LoadAssembliesForResolve(scriptInfo);
            CompilerResults compilerResults = Compile(scriptInfo);

            if (compilerResults.Errors.Count == 0)
            {
                ScriptContainer scriptContainer = CreateCompiledScriptContainer(compilerResults, scriptEnvironment);
                scriptContainer.Execute();
            }
            else
            {
                MessageManager.WriteCompileErrors(compilerResults);
                MessageManager.WriteLine();

                string sourceCode = GetSourceCode(scriptInfo);
                MessageManager.WriteSourceCode(sourceCode, compilerResults);
                scriptEnvironment.ExitCode = 1;
            }

#endif
            needAutoClose = scriptEnvironment.AutoClose;
            ExitCode = scriptEnvironment.ExitCode;
        }

        private ScriptContainer CreateCompiledScriptContainer(CompilerResults compilerResults, IScriptEnvironment scriptEnvironment)
        {
            Assembly compiledAssembly = compilerResults.CompiledAssembly;
            object instance = compiledAssembly.CreateInstance("CSScript.CompiledScriptContainer",
                false,
                BindingFlags.Public | BindingFlags.Instance,
                null,
                new object[] { scriptEnvironment },
                CultureInfo.CurrentCulture,
                null);
            return (ScriptContainer)instance;
        }

        private CompilerResults Compile(ScriptInfo scriptInfo)
        {
            string[] referencedAssemblies = assemblyManager.GetReferencedAssemblies(scriptInfo, true);
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

            string[] textLines = scriptText.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string textLine in textLines)
            {
                ScriptLineInfo scriptLine = ScriptLineInfo.Parse(textLine);
                if (!scriptLine.IsEmpty)
                {
                    switch (scriptLine.OperatorName)
                    {
                        case "define":
                            AddScriptDependence(scriptInfo, scriptLine.OperatorValue);
                            break;

                        case "using":
                            scriptInfo.UsingList.Add($"using {scriptLine.OperatorValue};");
                            break;

                        case "class":
                            currentBlock = scriptInfo.ClassBlock;
                            break;

                        case "ns":
                        case "namespace":
                            currentBlock = scriptInfo.NamespaceBlock;
                            break;

                        default:
                            if (currentBlock.Length > 0)
                            {
                                currentBlock.AppendLine();
                            }
                            currentBlock.Append(scriptLine.SourceLine);
                            break;
                    }
                }
            }

            // преобразование из относительных в абсолютные пути для подключаемых сборок
            string scriptWorkDirectory = PathManager.GetWorkDirectoryPath(scriptPath);
            for (int i = 0; i < scriptInfo.DefinedAssemblyList.Count; i++)
            {
                scriptInfo.DefinedAssemblyList[i]
                    = PathManager.GetAssemblyPath(scriptInfo.DefinedAssemblyList[i], scriptWorkDirectory);
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

        public void AddScriptDependence(ScriptInfo scriptInfo, string dependencePath)
        {
            string dependenceFileExtension = Path.GetExtension(dependencePath).ToLower();
            switch (dependenceFileExtension)
            {
                case ".exe":
                case ".dll":
                    scriptInfo.DefinedAssemblyList.Add(dependencePath);
                    break;

                default:
                    scriptInfo.DefinedScriptList.Add(dependencePath);
                    break;
            }
        }

        private ScriptInfo MergeScripts(ScriptInfo scriptInfo, string definedScriptPath)
        {
            string definedScriptFullPath = PathManager.GetAndCheckFullPath(definedScriptPath,
                PathManager.GetWorkDirectoryPath(scriptInfo.ScriptPath));
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

        private IScriptEnvironment CreateScriptEnvironment(string scriptPath, string[] scriptArgs)
        {
            return new ProgramScriptEnvironment(this, scriptPath, scriptArgs);
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

        private bool HasAdministativePrivilegies()
        {
            bool isElevated;
            using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
            {
                WindowsPrincipal principal = new WindowsPrincipal(identity);
                isElevated = principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            return isElevated;
        }
    }
}
