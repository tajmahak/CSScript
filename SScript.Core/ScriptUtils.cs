using Microsoft.CSharp;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;

namespace CSScript.Core
{
    public static class ScriptUtils
    {
        private const string compiledScriptName = "CompiledScriptContainer";
        private const string compiledScriptNamespace = "CompiledScriptContainerNamespace";


        public static ScriptInfo CreateScriptInfo(string scriptPath) {
            ScriptInfo mainScript = new ScriptInfo(scriptPath);
            AppendScript(mainScript, scriptPath, 0);

            Utils.DeleteDuplicates(mainScript.DefinedList, (a, b) => a.Equals(b));
            Utils.DeleteDuplicates(mainScript.UsingList, (a, b) => a.Equals(b));

            return mainScript;
        }

        public static CompilerResults CompileScript(ScriptInfo scriptInfo) {
            string sourceCode = CreateSourceCode(scriptInfo);
            string[] referencedAssemblies = GetReferencedAssemblies(scriptInfo);
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
            code.AppendLine(scriptInfo.ProcedureBlock.ToString());
            code.AppendLine("}");

            code.AppendLine();
            code.AppendLine(scriptInfo.ClassBlock.ToString());
            code.AppendLine("}");

            code.AppendLine();
            code.AppendLine(scriptInfo.NamespaceBlock.ToString());
            code.AppendLine("}");

            return code.ToString();
        }

        public static Dictionary<string, Assembly> GetDefinedAssemblies(ScriptInfo scriptInfo) {
            Dictionary<string, Assembly> assemblies = new Dictionary<string, Assembly>();
            foreach (string assemblyPath in scriptInfo.DefinedList) {
                Assembly assembly = Assembly.LoadFrom(assemblyPath);
                if (!assemblies.ContainsKey(assembly.FullName)) {
                    assemblies.Add(assembly.FullName, assembly);
                }
            }
            return assemblies;
        }

        private static void AppendScript(ScriptInfo mainScript, string scriptPath, int level) {
            string workingDirectory = Utils.GetDirectoryName(scriptPath);

            ScriptInfo script = Utils.LoadScriptInfo(scriptPath);
            foreach (string defineItem in script.DefinedList) {
                string defineFilePath = Utils.GetFilePath(defineItem, workingDirectory);
                if (Utils.IsWindowsAssembly(defineFilePath)) {
                    mainScript.DefinedList.Add(defineFilePath);
                } else {
                    AppendScript(mainScript, defineFilePath, level + 1);
                }
            }

            if (level == 0) { // выполнение процедуры допустимо только в скрипте самого высокого уровня
                mainScript.ProcedureBlock.AppendLine(script.ProcedureBlock.ToString());
            }
            mainScript.ClassBlock.AppendLine(script.ClassBlock.ToString());
            mainScript.NamespaceBlock.AppendLine(script.NamespaceBlock.ToString());
            mainScript.UsingList.AddRange(script.UsingList);
        }

        private static string[] GetReferencedAssemblies(ScriptInfo scriptContent) {
            List<string> definedAssemblies = new List<string> {
                "System.dll", // библиотека для работы множества основных функций
            };
            definedAssemblies.Add(Assembly.GetExecutingAssembly().Location); // для взаимодействия с программой, запускающей скрипт
            foreach (string definedAssemblyPath in scriptContent.DefinedList) { // дополнительные библиотеки, указанные в #define
                definedAssemblies.Add(definedAssemblyPath);
            }
            return definedAssemblies.ToArray();
        }
    }
}
