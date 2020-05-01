using CSScript.Properties;
using Microsoft.CSharp;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace CSScript
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            // для динамической подгрузки библиотек, используемых скриптом (#define)
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;

            int exitCode;
            InputArguments inputArguments = null;
            try
            {
                inputArguments = ParseArguments(args);

                if (inputArguments.IsEmpty)
                {
                    PrintHelpInfo();
                    WaitForExitConsole();
                    exitCode = 0;
                }
                else
                {
                    if (inputArguments.HideMode)
                    {
                        HideConsole();
                    }

                    string script = GetScriptContent(inputArguments);

                    // после загрузки содержимого скрипта переключаемся на его рабочую директорию вместо рабочей директории программы
                    // (для возможности указания коротких путей к файлам, в т.ч. загружаемым в скрипте сборкам)
                    Environment.CurrentDirectory = GetScriptWorkDirectory(inputArguments);

                    ScriptData scriptData = ParseScriptData(script);
                    definedAssemblies = LoadDefinedAssemblies(scriptData);
                    CompilerResults compilerResults = Compile(scriptData);

                    if (compilerResults.Errors.Count == 0)
                    {
                        ICompile compiledClass = GetCompiledObject(compilerResults);
                        exitCode = compiledClass.Execute(inputArguments.ScriptArgument);
                    }
                    else
                    {
                        PrintCompileErrors(compilerResults);
                        string sourceCode = GetSourceCode(scriptData);

                        Console.WriteLine();

                        PrintSourceCode(sourceCode, compilerResults);
                        exitCode = 1;
                    }
                }
            }
            catch (Exception ex)
            {
                PrintException(ex);
                exitCode = 1;
            }

            if (inputArguments != null && inputArguments.WaitMode)
            {
                WaitForExitConsole();
            }

            return exitCode;
        }


        // Работа со сборками

        /// <summary>
        /// Загрузка используемых в скрипте сборок для последующей подгрузки в рантайм
        /// </summary>
        /// <param name="scriptData"></param>
        /// <returns></returns>
        private static Dictionary<string, Assembly> LoadDefinedAssemblies(ScriptData scriptData)
        {
            string[] definedAssemblies = GetDefinedAssemblies(scriptData);

            Dictionary<string, Assembly> assemblyDict = new Dictionary<string, Assembly>();
            foreach (string defineAssembly in definedAssemblies)
            {
                if (File.Exists(defineAssembly))
                {
                    // загружаются только те сборки, которые рантайм не может подгрузить автоматически
                    Assembly assembly = Assembly.LoadFrom(defineAssembly);
                    assemblyDict.Add(assembly.FullName, assembly);
                }
            }

            return assemblyDict;
        }

        /// <summary>
        /// Получение списка всех связанных с выполнением скрипта сборок
        /// </summary>
        /// <param name="scriptData"></param>
        /// <returns></returns>
        private static string[] GetDefinedAssemblies(ScriptData scriptData)
        {
            List<string> definedAssemblies = new List<string>();
            definedAssemblies.Add(Assembly.GetExecutingAssembly().Location);
            foreach (string defineAssembly in scriptData.DefineList)
            {
                definedAssemblies.Add(defineAssembly);
            }
            return definedAssemblies.ToArray();
        }

        /// <summary>
        /// Подгрузка сборок рантаймом по мере необходимости
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            if (definedAssemblies != null && definedAssemblies.ContainsKey(args.Name))
            {
                // загружаются только те сборки, которые рантайм не может подцепить автоматически
                Assembly assembly = definedAssemblies[args.Name];
                return assembly;
            }
            return null;
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
                    else
                    {
                        inputArguments.ScriptPath = arg;
                    }
                }
            }
            return inputArguments;
        }

        /// <summary>
        /// Получение текста скрипта из внешнего источника (файла)
        /// </summary>
        /// <param name="inputArguments"></param>
        /// <returns></returns>
        private static string GetScriptContent(InputArguments inputArguments)
        {
            string scriptPath = GetScriptPath(inputArguments);
            string script = File.ReadAllText(scriptPath, Encoding.UTF8);
            return script;
        }

        /// <summary>
        /// Парсинг скрипта на отдельные блоки
        /// </summary>
        /// <param name="script"></param>
        /// <returns></returns>
        private static ScriptData ParseScriptData(string script)
        {
            List<string> defineList = new List<string>();
            List<string> usingList = new List<string>();

            StringBuilder sourceCodeBlock = new StringBuilder();
            StringBuilder functionBlock = new StringBuilder();
            StringBuilder classBlock = new StringBuilder();

            StringBuilder currentBlock = sourceCodeBlock;

            string[] opLines = script.Split(new string[] { ";" }, StringSplitOptions.None);
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
                            else if (trimLine.StartsWith("#func"))
                            {
                                currentBlock = functionBlock;
                                continue;
                            }
                            else if (trimLine.StartsWith("#class"))
                            {
                                currentBlock = classBlock;
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
            return new ScriptData()
            {
                DefineList = defineList.ToArray(),
                UsingBlock = string.Join("\r\n", usingList.ToArray()),
                SourceCodeBlock = sourceCodeBlock.ToString(),
                FunctionBlock = functionBlock.ToString(),
                ClassBlock = classBlock.ToString(),
            };
        }

        /// <summary>
        /// Сборка блоков скрипта в исходный код, готовый к динамической компиляции
        /// </summary>
        /// <param name="scriptData"></param>
        /// <returns></returns>
        private static string GetSourceCode(ScriptData scriptData)
        {
            string source = Resources.SourceCodeTemplate;
            source = source.Replace("##using##", scriptData.UsingBlock);
            source = source.Replace("##source##", scriptData.SourceCodeBlock);
            source = source.Replace("##func##", scriptData.FunctionBlock);
            source = source.Replace("##class##", scriptData.ClassBlock);
            return source;
        }

        /// <summary>
        /// Динамическая компиляция блоков скрипта в сборку
        /// </summary>
        /// <param name="scriptData"></param>
        /// <returns></returns>
        private static CompilerResults Compile(ScriptData scriptData)
        {
            string[] assemblies = GetDefinedAssemblies(scriptData);

            using (CSharpCodeProvider provider = new CSharpCodeProvider())
            {
                CompilerParameters compileParameters = new CompilerParameters(assemblies);
                compileParameters.GenerateInMemory = true;
                compileParameters.GenerateExecutable = false;

                string source = GetSourceCode(scriptData);
                CompilerResults compilerResults = provider.CompileAssemblyFromSource(compileParameters, source);
                return compilerResults;
            }
        }

        /// <summary>
        /// Получение скомпилированного скрипта для дальнейшего запуска через интерфейс
        /// </summary>
        /// <param name="compilerResults"></param>
        /// <returns></returns>
        private static ICompile GetCompiledObject(CompilerResults compilerResults)
        {
            Assembly compiledAssembly = compilerResults.CompiledAssembly;
            object instance = compiledAssembly.CreateInstance("CSScript.CompileClass");
            return (ICompile)instance;
        }


        // Работа с консолью

        /// <summary>
        /// Вывод на консоль справочной информации о функциях программы
        /// </summary>
        private static void PrintHelpInfo()
        {
            ConsoleColor startColor = Console.ForegroundColor;

            string[] lines = Resources.HelpText.Split(new string[] { "\r\n" }, StringSplitOptions.None);
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                if (line.StartsWith("<r>"))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    line = line.Substring(3); //<r>
                }
                else
                {
                    Console.ForegroundColor = startColor;
                }
                Console.WriteLine(line);
            }

            Console.ForegroundColor = startColor;
        }

        /// <summary>
        /// Вывод на консоль Exception
        /// </summary>
        /// <param name="ex"></param>
        private static void PrintException(Exception ex)
        {
            ConsoleColor startColor = Console.ForegroundColor;

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("# Ошибка: {0}", ex.Message);

            Console.ForegroundColor = startColor;
        }

        /// <summary>
        /// Вывод на консоль ошибок динамической компиляции
        /// </summary>
        /// <param name="compilerResults"></param>
        private static void PrintCompileErrors(CompilerResults compilerResults)
        {
            ConsoleColor startColor = Console.ForegroundColor;

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("# Ошибок: {0}", compilerResults.Errors.Count);
            int errorNumber = 1;
            foreach (CompilerError error in compilerResults.Errors)
            {
                if (error.Line > 0)
                {
                    Console.WriteLine($"# {errorNumber++} (cтрока {error.Line}): {error.ErrorText}");
                }
                else
                {
                    Console.WriteLine($"# {errorNumber++}: {error.ErrorText}");
                }
            }

            Console.ForegroundColor = startColor;
        }

        /// <summary>
        /// Вывод на консоль текста исходного кода с подсветкой ошибочных строк
        /// </summary>
        /// <param name="sourceCode"></param>
        /// <param name="compilerResults"></param>
        private static void PrintSourceCode(string sourceCode, CompilerResults compilerResults = null)
        {
            ConsoleColor startColor = Console.ForegroundColor;

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

                Console.ForegroundColor = startColor;
                Console.Write(lineNumber.ToString().PadLeft(4) + ": ");
                if (errorLines.Contains(lineNumber))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Blue;
                }
                Console.WriteLine(lines[i]);
            }

            Console.ForegroundColor = startColor;
        }

        /// <summary>
        /// Скрытие окна консоли
        /// </summary>
        private static void HideConsole()
        {
            IntPtr handle = NativeMethods.GetConsoleWindow();
            NativeMethods.ShowWindow(handle, NativeMethods.SW_HIDE);
            consoleVisible = false;
        }

        /// <summary>
        /// Отображение окна консоли после скрытия
        /// </summary>
        private static void ShowConsole()
        {
            IntPtr handle = NativeMethods.GetConsoleWindow();
            NativeMethods.ShowWindow(handle, NativeMethods.SW_SHOW);
            consoleVisible = true;
        }

        /// <summary>
        /// Предупреждение о выходе из программы
        /// </summary>
        private static void WaitForExitConsole()
        {
            if (!consoleVisible)
            {
                ShowConsole();
            }

            Console.WriteLine();
            Console.WriteLine("Для выхода нажмите любую клавишу...");
            Console.ReadKey();
        }


        // Переменные

        /// <summary>
        /// Текущее состоянии видимости консоли
        /// </summary>
        private static bool consoleVisible = true;

        /// <summary>
        /// Загруженные сборки, используемые скриптом
        /// </summary>
        private static Dictionary<string, Assembly> definedAssemblies;
    }
}
