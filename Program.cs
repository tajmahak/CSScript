using CSScript.Properties;
using Microsoft.CSharp;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows.Forms;

namespace CSScript
{
    internal static class Program
    {
        [STAThread]
        private static int Main(string[] args)
        {
            OutputLog = new Log();
#if DEBUG
            return ExecuteDebugScript(args);
#else
            return ExecuteScript(args);
#endif
        }

        /// <summary>
        /// Запуск действий для работы скрипта
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        private static int ExecuteScript(string[] args)
        {
            int exitCode;
            InputArguments inputArguments = null;

            // для подгрузки библиотек рантаймом, которые он не может подгрузить самостоятельно
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;

            try
            {
                inputArguments = ParseArguments(args);

                if (inputArguments.IsEmpty)
                {
                    WriteHelpInfo();
                    ShowModalForm();
                    exitCode = 0;
                }
                else
                {
                    if (!inputArguments.HideMode)
                    {
                        ShowModalForm();
                    }

                    string script = GetScriptText(inputArguments);

                    WriteStartInfo(inputArguments);

                    // после загрузки содержимого скрипта переключаемся на его рабочую директорию вместо рабочей директории программы
                    // (для возможности указания коротких путей к файлам, в т.ч. загружаемым в скрипте сборкам)
                    Environment.CurrentDirectory = GetScriptWorkDirectory(inputArguments);

                    ScriptParsingInfo scriptParsingInfo = ParseScriptInfo(script);
                    resolvedAssemblies = LoadAssembliesForResolve(scriptParsingInfo);
                    CompilerResults compilerResults = Compile(scriptParsingInfo);

                    if (compilerResults.Errors.Count == 0)
                    {
                        ScriptRuntime scriptRuntime = GetCompiledScript(compilerResults);
                        exitCode = scriptRuntime.RunScript(inputArguments.ScriptArgument);
                    }
                    else
                    {
                        WriteCompileErrors(compilerResults);
                        string sourceCode = GetSourceCode(scriptParsingInfo);

                        OutputLog.WriteLine();

                        WriteSourceCode(sourceCode, compilerResults);
                        exitCode = 1;
                    }
                }
            }
            catch (Exception ex)
            {
                WriteException(ex);
                exitCode = 1;
            }

            OutputLog.WriteLine();
            OutputLog.WriteLine($"# Выполнено с кодом возврата: {exitCode}", Settings.InfoColor);

            if (inputArguments != null && !string.IsNullOrEmpty(inputArguments.LogPath))
            {
                try
                {
                    SaveLogs(inputArguments.LogPath);
                }
                catch (Exception ex)
                {
                    OutputLog.WriteLine($"# Не удалось сохранить лог: {ex.Message}", Settings.ErrorColor);
                }
            }

            Finished = true;

            if (inputArguments != null && (inputArguments.WaitMode || inputArguments.IsEmpty))
            {
                WaitFormForExit();
            }

            return exitCode;
        }

#if DEBUG
        /// <summary>
        /// Запуск действий для работы отладочного скрипта
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        private static int ExecuteDebugScript(string[] args)
        {
            int exitCode;
            InputArguments inputArguments = null;

            // для подгрузки библиотек рантаймом, которые он не может подгрузить самостоятельно
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;

            try
            {
                inputArguments = ParseArguments(args);
                ShowModalForm();

                var debugScript = new DebugScript();
                exitCode = debugScript.RunScript(inputArguments.ScriptArgument);
            }
            catch (Exception ex)
            {
                WriteException(ex);
                exitCode = 1;
            }

            OutputLog.WriteLine();
            OutputLog.WriteLine($"# Выполнено с кодом возврата: {exitCode}", Settings.InfoColor);

            Finished = true;
            WaitFormForExit();
            return exitCode;
        }
#endif


        /// <summary>
        /// Извлечение структурированных аргументов из аргументов командной строки
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        private static InputArguments ParseArguments(string[] args)
        {
            InputArguments inputArguments = new InputArguments();
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
            return inputArguments;
        }

        /// <summary>
        /// Получение полного пути к скрипту
        /// </summary>
        /// <param name="inputArguments"></param>
        /// <returns></returns>
        private static string GetScriptPath(InputArguments inputArguments)
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

        /// <summary>
        /// Получение полного пути к рабочей директории, в которой находится скрипт
        /// </summary>
        /// <param name="inputArguments"></param>
        /// <returns></returns>
        public static string GetScriptWorkDirectory(InputArguments inputArguments)
        {
            string scriptPath = GetScriptPath(inputArguments);
            return Path.GetDirectoryName(scriptPath);
        }

        /// <summary>
        /// Получение текста скрипта из внешнего источника (файла)
        /// </summary>
        /// <param name="inputArguments"></param>
        /// <returns></returns>
        private static string GetScriptText(InputArguments inputArguments)
        {
            string scriptPath = GetScriptPath(inputArguments);
            string script = File.ReadAllText(scriptPath, Encoding.UTF8);
            return script;
        }

        /// <summary>
        /// Парсинг скрипта на отдельные блоки
        /// </summary>
        /// <param name="scriptText"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Сборка блоков скрипта в исходный код, готовый к динамической компиляции
        /// </summary>
        /// <param name="scriptParsingInfo"></param>
        /// <returns></returns>
        private static string GetSourceCode(ScriptParsingInfo scriptParsingInfo)
        {
            string source = Resources.SourceCodeTemplate;
            source = source.Replace("##using##", scriptParsingInfo.UsingBlock);
            source = source.Replace("##function##", scriptParsingInfo.FunctionBlock);
            source = source.Replace("##class##", scriptParsingInfo.ClassBlock);
            source = source.Replace("##namespace##", scriptParsingInfo.NamespaceBlock);
            return source;
        }

        /// <summary>
        /// Динамическая компиляция блоков скрипта в сборку
        /// </summary>
        /// <param name="scriptParsingInfo"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Получение скомпилированного скрипта для дальнейшего запуска через интерфейс
        /// </summary>
        /// <param name="compilerResults"></param>
        /// <returns></returns>
        private static ScriptRuntime GetCompiledScript(CompilerResults compilerResults)
        {
            Assembly compiledAssembly = compilerResults.CompiledAssembly;
            object instance = compiledAssembly.CreateInstance("CSScript.CompileScript");
            return (ScriptRuntime)instance;
        }


        // Работа со сборками

        /// <summary>
        /// Получение списка всех связанных с выполнением скрипта сборок
        /// </summary>
        /// <param name="scriptParsingInfo"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Загрузка используемых в скрипте сборок для последующей подгрузки в рантайм
        /// </summary>
        /// <param name="scriptParsingInfo"></param>
        /// <returns></returns>
        private static Dictionary<string, Assembly> LoadAssembliesForResolve(ScriptParsingInfo scriptParsingInfo)
        {
            string[] assemblies = GetReferencedAssemblies(scriptParsingInfo);

            Dictionary<string, Assembly> assemblyDict = new Dictionary<string, Assembly>();
            foreach (string assembly in assemblies)
            {
                if (File.Exists(assembly))
                {
                    // загружаются только те сборки, которые рантайм не может подгрузить автоматически
                    Assembly loadedAssembly = Assembly.LoadFrom(assembly);
                    assemblyDict.Add(loadedAssembly.FullName, loadedAssembly);
                }
            }

            return assemblyDict;
        }

        /// <summary>
        /// Подгрузка сборок рантаймом по мере необходимости
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            if (resolvedAssemblies != null && resolvedAssemblies.ContainsKey(args.Name))
            {
                // подгружаются только те сборки, которые рантайм не может подцепить автоматически
                Assembly assembly = resolvedAssemblies[args.Name];
                return assembly;
            }
            return null;
        }


        // Работа с логами 

        /// <summary>
        /// Вывод на консоль справочной информации о функциях программы
        /// </summary>
        private static void WriteHelpInfo()
        {
            string[] lines = Resources.HelpText.Split(new string[] { "\r\n" }, StringSplitOptions.None);
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                if (line.StartsWith("<c>"))
                {
                    line = line.Substring(3); //<c>
                    OutputLog.WriteLine(line, Settings.CaptionColor);
                }
                else if (line.StartsWith("<i>"))
                {
                    line = line.Substring(3); //<i>
                    OutputLog.WriteLine(line, Settings.InfoColor);
                }
                else
                {
                    OutputLog.WriteLine(line);
                }
            }

        }

        /// <summary>
        /// Вывод начальной информации при обработке скрипта
        /// </summary>
        /// <param name="inputArguments"></param>
        private static void WriteStartInfo(InputArguments inputArguments)
        {
            OutputLog.WriteLine($"## {GetScriptPath(inputArguments)}", Settings.InfoColor);
            OutputLog.WriteLine($"## {DateTime.Now}", Settings.InfoColor);
            OutputLog.WriteLine();
        }

        /// <summary>
        /// Вывод на консоль Exception
        /// </summary>
        /// <param name="ex"></param>
        private static void WriteException(Exception ex)
        {
            OutputLog.WriteLine($"# Ошибка: {ex.Message}", Settings.ErrorColor);
        }

        /// <summary>
        /// Вывод на консоль ошибок динамической компиляции
        /// </summary>
        /// <param name="compilerResults"></param>
        private static void WriteCompileErrors(CompilerResults compilerResults)
        {
            OutputLog.WriteLine($"# Ошибок: {compilerResults.Errors.Count}", Settings.ErrorColor);
            int errorNumber = 1;
            foreach (CompilerError error in compilerResults.Errors)
            {
                if (error.Line > 0)
                {
                    OutputLog.WriteLine($"# {errorNumber++} (cтрока {error.Line}): {error.ErrorText}", Settings.ErrorColor);
                }
                else
                {
                    OutputLog.WriteLine($"# {errorNumber++}: {error.ErrorText}", Settings.ErrorColor);
                }
            }
        }

        /// <summary>
        /// Вывод на консоль текста исходного кода с подсветкой ошибочных строк
        /// </summary>
        /// <param name="sourceCode"></param>
        /// <param name="compilerResults"></param>
        private static void WriteSourceCode(string sourceCode, CompilerResults compilerResults = null)
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

                OutputLog.Write(lineNumber.ToString().PadLeft(4) + ": ");
                if (errorLines.Contains(lineNumber))
                {
                    OutputLog.WriteLine(lines[i], Settings.ErrorColor);
                }
                else
                {
                    OutputLog.WriteLine(lines[i], Settings.SourceCodeColor);
                }
            }
        }

        /// <summary>
        /// Сохранение логов программы в файл
        /// </summary>
        /// <param name="logPath"></param>
        private static void SaveLogs(string logPath)
        {
            string logText = OutputLog.ToString();
            using (StreamWriter writer = new StreamWriter(logPath, true, Encoding.UTF8))
            {
                writer.WriteLine(logText);
                writer.WriteLine();
                writer.WriteLine();
            }
        }


        // Работа с GUI

        /// <summary>
        /// Отображение окна вывода лога в модальном представлении
        /// </summary>
        private static void ShowModalForm()
        {
            if (form == null)
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                form = new MainForm();
                form.Show();
            }
        }

        /// <summary>
        /// Переключение из модального режима в диалоговый, чтобы программа автоматически не закрывалась
        /// </summary>
        private static void WaitFormForExit()
        {
            ShowModalForm();
            Application.Run(form);
        }


        // Переменные

        /// <summary>
        /// Лог работы программы
        /// </summary>
        internal static Log OutputLog { get; private set; }

        /// <summary>
        /// Указывает на окончание работы программы
        /// </summary>
        internal static bool Finished { get; private set; }

        /// <summary>
        /// Настройки программы
        /// </summary>
        internal static Settings Settings { get; private set; } = Settings.Default;

        /// <summary>
        /// Загруженные сборки, используемые скриптом
        /// </summary>
        private static Dictionary<string, Assembly> resolvedAssemblies;

        /// <summary>
        /// Форма для вывода лога
        /// </summary>
        private static MainForm form;
    }
}
