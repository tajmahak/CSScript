using Microsoft.CSharp;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace CSScript.Core
{
    public static class ScriptUtils
    {
        private const string compiledScriptName = "CompiledScriptContainer";
        private const string compiledScriptNamespace = "CompiledScriptContainerNamespace";


        public delegate string ImportResolveHandler(string importFilePath, string workingDirectory);


        public static ScriptInfo CreateScriptInfo(string scriptPath, string[] baseUsingList, ImportResolveHandler importResolver) {
            ScriptInfo mainScript = new ScriptInfo(scriptPath);
            mainScript.UsingList.AddRange(baseUsingList);

            AppendScript(mainScript, scriptPath, 0, importResolver);

            DeleteDuplicates(mainScript.ImportList, (a, b) => a.Equals(b));
            DeleteDuplicates(mainScript.UsingList, (a, b) => a.Equals(b));

            return mainScript;
        }

        public static CompilerResults CompileScript(ScriptInfo scriptInfo, string[] baseAssemblyList) {
            // Компиляция C# 6 с помощью Roslyn на Net Framework 4.5
            // https://stackoverflow.com/questions/31639602/using-c-sharp-6-features-with-codedomprovider-roslyn
            // https://www.nuget.org/packages/Microsoft.CodeDom.Providers.DotNetCompilerPlatform/
            // CodeDomProvider objCodeCompiler = new Microsoft.CodeDom.Providers.DotNetCompilerPlatform.CSharpCodeProvider();

            string sourceCode = CreateSourceCode(scriptInfo);
            string[] referencedAssemblies = GetReferencedAssemblies(scriptInfo, baseAssemblyList);
            using (CSharpCodeProvider provider = new CSharpCodeProvider()) {
                CompilerParameters compileParameters = new CompilerParameters(referencedAssemblies) {
                    GenerateInMemory = true,
                    GenerateExecutable = false,
                };
                return provider.CompileAssemblyFromSource(compileParameters, sourceCode);
            }
        }

        public static ScriptContainer CreateScriptContainer(CompilerResults compiledScript, IScriptContext environment) {
            Validate.IsTrue(compiledScript.Errors.Count == 0);
            return (ScriptContainer)compiledScript.CompiledAssembly.CreateInstance(
                compiledScriptNamespace + "." + compiledScriptName,
                false,
                BindingFlags.Public | BindingFlags.Instance,
                null,
                new object[] { environment },
                CultureInfo.CurrentCulture,
                null);
        }

        public static string CreateSourceCode(ScriptInfo scriptInfo) {
            Type scriptContainerType = typeof(ScriptContainer);

            StringBuilder code = new StringBuilder();
            foreach (string usingItem in scriptInfo.UsingList) {
                code.AppendLine("using " + usingItem + ";");
            }
            code.AppendLine();
            code.AppendLine("namespace " + compiledScriptNamespace + " {");
            code.AppendLine();
            code.AppendLine("public class " + compiledScriptName + " : " + scriptContainerType.FullName + " {");

            // Создание конструктора
            ConstructorInfo[] consructors = scriptContainerType.GetConstructors();
            Validate.IsTrue(consructors.Length == 1);
            code.AppendLine();
            code.Append("public " + compiledScriptName + "(");
            ConstructorInfo constructor = consructors[0];
            ParameterInfo[] constructorParams = constructor.GetParameters();

            List<string> stringList = new List<string>(constructorParams.Length);
            foreach (ParameterInfo constructorParam in constructorParams) {
                stringList.Add(constructorParam.ParameterType.FullName + " " + constructorParam.Name);
            }
            code.Append(string.Join(", ", stringList));

            stringList.Clear();
            foreach (ParameterInfo constructorParam in constructorParams) {
                stringList.Add(constructorParam.Name);
            }
            code.Append(") : base(" + string.Join(", ", stringList) + ") { }");
            code.AppendLine();

            // Переопределение метода
            MethodInfo[] abstractMethods = scriptContainerType.GetMethods()
                .Where(x => x.IsAbstract && x.ReturnType == typeof(void))
                .ToArray();
            Validate.IsTrue(abstractMethods.Length == 1);
            MethodInfo abstractMethod = abstractMethods[0];
            code.AppendLine();
            code.Append("public override void " + abstractMethod.Name + "(");

            ParameterInfo[] abstractMethodParams = abstractMethod.GetParameters();
            stringList.Clear();
            foreach (ParameterInfo abstractMethodParam in abstractMethodParams) {
                stringList.Add(abstractMethodParam.ParameterType.FullName + " " + abstractMethodParam.Name);
            }
            code.Append(string.Join(", ", stringList) + ") {");
            code.AppendLine();
            code.AppendLine("// === init block ===");
            code.AppendLine(scriptInfo.InitBlock.ToString().Trim());
            code.AppendLine("// === procedure block ===");
            code.AppendLine(scriptInfo.ProcedureBlock.ToString().Trim());
            code.AppendLine("}");

            code.AppendLine();
            code.AppendLine("// === class block ===");
            code.AppendLine(scriptInfo.ClassBlock.ToString().Trim());
            code.AppendLine("}");

            code.AppendLine();
            code.AppendLine("// === namespace block ===");
            code.AppendLine(scriptInfo.NamespaceBlock.ToString().Trim());
            code.AppendLine("}");

            return code.ToString();
        }

        public static Dictionary<string, Assembly> GetImportedAssemblies(ScriptInfo scriptInfo) {
            Dictionary<string, Assembly> assemblies = new Dictionary<string, Assembly>();
            foreach (string assemblyPath in scriptInfo.ImportList) {
                if (File.Exists(assemblyPath)) { // если сборки не существует по указанному пути, вероятно она имеется в GAC
                    Assembly assembly = Assembly.LoadFrom(assemblyPath);
                    if (!assemblies.ContainsKey(assembly.FullName)) {
                        assemblies.Add(assembly.FullName, assembly);
                    }
                }
            }
            return assemblies;
        }


        private static void AppendScript(ScriptInfo mainScript, string scriptPath, int level, ImportResolveHandler importResolver) {
            string workingDirectory = GetDirectoryName(scriptPath);

            ScriptInfo script = LoadScriptInfo(scriptPath);
            foreach (string importItem in script.ImportList) {
                string importFilePath = GetImportFilePath(importItem, workingDirectory, importResolver);
                if (IsWindowsAssembly(importFilePath)) {
                    mainScript.ImportList.Add(importFilePath);
                } else {
                    AppendScript(mainScript, importFilePath, level + 1, importResolver);
                }
            }

            if (level == 0) { // выполнение процедуры допустимо только в скрипте самого высокого уровня
                mainScript.ProcedureBlock.AppendLine(script.ProcedureBlock.ToString());
            }
            mainScript.InitBlock.AppendLine(script.InitBlock.ToString());
            mainScript.ClassBlock.AppendLine(script.ClassBlock.ToString());
            mainScript.NamespaceBlock.AppendLine(script.NamespaceBlock.ToString());
            mainScript.UsingList.AddRange(script.UsingList);
        }

        private static string GetImportFilePath(string filePath, string workingDirectory, ImportResolveHandler importResolver) {
            if (Path.IsPathRooted(filePath)) {
                return filePath;
            }

            string testFilePath = Path.Combine(workingDirectory, filePath);
            if (File.Exists(testFilePath)) {
                return filePath;
            }

            testFilePath = importResolver(filePath, workingDirectory);
            testFilePath = Path.GetFullPath(testFilePath);
            if (File.Exists(testFilePath)) {
                return testFilePath;
            }

            if (IsWindowsAssembly(filePath)) {
                return filePath; // вероятно, сборка содержится в GAC
            }

            throw new FileNotFoundException("Файл '" + testFilePath + "' не найден.", testFilePath);
        }

        private static string[] GetReferencedAssemblies(ScriptInfo scriptContent, string[] baseList) {
            List<string> importedAssemblies = new List<string>();
            importedAssemblies.AddRange(baseList);
            importedAssemblies.Add(Assembly.GetExecutingAssembly().Location); // для взаимодействия с программой, запускающей скрипт
            foreach (string importedAssemblyPath in scriptContent.ImportList) { // дополнительные библиотеки, указанные в #import
                importedAssemblies.Add(importedAssemblyPath);
            }
            return importedAssemblies.ToArray();
        }

        private static ScriptInfo LoadScriptInfo(string scriptPath) {
            string scriptText = File.ReadAllText(scriptPath, Encoding.UTF8);
            return ScriptInfo.FromFile(scriptPath, scriptText);
        }

        private static bool IsWindowsAssembly(string filePath) {
            string extension = Path.GetExtension(filePath).ToLower();
            return extension.Equals(".exe") || extension.Equals(".dll");
        }

        private static void DeleteDuplicates<T>(List<T> list, Func<T, T, bool> comparison) {
            for (int i = 0; i < list.Count - 1; i++) {
                T value1 = list[i];
                for (int j = i + 1; j < list.Count; j++) {
                    T value2 = list[j];
                    if (comparison(value1, value2)) {
                        list.RemoveAt(j--);
                    }
                }
            }
        }

        private static string GetDirectoryName(string filePath) {
            string path = Path.GetFullPath(filePath);
            return Path.GetDirectoryName(path);
        }
    }
}
