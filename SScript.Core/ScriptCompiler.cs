using Microsoft.CSharp;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;

namespace CSScript.Core
{
    public static class ScriptCompiler
    {
        private const string compiledScriptName = "CompiledScriptContainer";


        public static CompilerResults CompileScript(string scriptPath)
        {
            ScriptContent scriptContent = LoadScriptContent(scriptPath);
            string sourceCode = CreateSourceCode(scriptContent);
            string[] referencedAssemblies = GetReferencedAssemblies(scriptContent);
            using (CSharpCodeProvider provider = new CSharpCodeProvider()) {
                CompilerParameters compileParameters = new CompilerParameters(referencedAssemblies) {
                    GenerateInMemory = true,
                    GenerateExecutable = false,
                };
                return provider.CompileAssemblyFromSource(compileParameters, sourceCode);
            }
        }

        public static ScriptContainer CreateScriptContainer(CompilerResults compilerResults, IScriptEnvironment environment)
        {
            if (compilerResults.Errors.Count > 0) {
                throw new Exception();
            }

            string typeName = Utils.GetNamespaceName(typeof(ScriptContainer)) + "." + compiledScriptName;
            Assembly compiledAssembly = compilerResults.CompiledAssembly;
            object instance = compiledAssembly.CreateInstance(typeName,
                false,
                BindingFlags.Public | BindingFlags.Instance,
                null,
                new object[] { environment },
                CultureInfo.CurrentCulture,
                null);
            return (ScriptContainer)instance;
        }


        private static ScriptContent LoadScriptContent(string scriptPath)
        {
            ScriptContent mainScript = new ScriptContent(scriptPath);
            AppendScript(mainScript, scriptPath, 0);

            Utils.DeleteDuplicates(mainScript.DefinedList, (a, b) => a.Equals(b));
            Utils.DeleteDuplicates(mainScript.UsingList, (a, b) => a.Equals(b));

            return mainScript;
        }

        private static void AppendScript(ScriptContent mainScript, string scriptPath, int level)
        {
            string workingDirectory = Utils.GetDirectory(scriptPath);

            ScriptContent script = Utils.LoadScriptContent(scriptPath);
            foreach (string defineItem in script.DefinedList) {
                string defineFilePath = Utils.GetFilePath(defineItem, workingDirectory);
                if (Utils.IsWindowsAssembly(defineFilePath)) {
                    mainScript.DefinedList.Add(defineFilePath);
                }
                else {
                    AppendScript(mainScript, defineFilePath, level + 1);
                }
            }

            mainScript.ClassBlock.AppendLine(script.ClassBlock.ToString());
            if (level == 0) { // выполнение процедуры допустимо только в скрипте самого высокого уровня
                mainScript.ProcedureBlock.AppendLine(script.ProcedureBlock.ToString());
            }
            mainScript.NamespaceBlock.AppendLine(script.NamespaceBlock.ToString());
            mainScript.UsingList.AddRange(script.UsingList);
        }

        private static string CreateSourceCode(ScriptContent scriptContent)
        {
            Type scriptContainerType = typeof(ScriptContainer);

            StringBuilder code = new StringBuilder();
            foreach (string usingItem in scriptContent.UsingList) {
                code.AppendLine("using " + usingItem + ";");
            }
            code.AppendLine();
            code.AppendLine("namespace " + Utils.GetNamespaceName(scriptContainerType) + " {");
            code.AppendLine();
            code.AppendLine("public class " + compiledScriptName + " : " + scriptContainerType.FullName + " {");

            // Создание конструктора
            ConstructorInfo[] consructors = scriptContainerType.GetConstructors();
            if (consructors.Length == 1) {
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
            }
            else {
                throw new NotSupportedException();
            }

            // Переопределение метода
            MethodInfo[] abstractMethods = scriptContainerType.GetMethods()
                .Where(x => x.IsAbstract && x.ReturnType == typeof(void))
                .ToArray();
            if (abstractMethods.Length == 1) {
                MethodInfo abstractMethod = abstractMethods[0];
                code.AppendLine();
                code.Append("public override void " + abstractMethod.Name + "(");

                ParameterInfo[] abstractMethodParams = abstractMethod.GetParameters();
                List<string> stringList = new List<string>(abstractMethodParams.Length);
                foreach (ParameterInfo abstractMethodParam in abstractMethodParams) {
                    stringList.Add(abstractMethodParam.ParameterType.FullName + " " + abstractMethodParam.Name);
                }
                code.Append(string.Join(", ", stringList) + ") {");
                code.AppendLine();
                code.AppendLine(scriptContent.ProcedureBlock.ToString());
                code.AppendLine("}");
            }
            else {
                throw new NotSupportedException();
            }

            code.AppendLine();
            code.AppendLine(scriptContent.ClassBlock.ToString());
            code.AppendLine("}");

            code.AppendLine();
            code.AppendLine(scriptContent.NamespaceBlock.ToString());
            code.AppendLine("}");

            return code.ToString();
        }

        private static string[] GetReferencedAssemblies(ScriptContent scriptContent)
        {
            List<string> definedAssemblies = new List<string> {
                "System.dll", // библиотека для работы множества основных функций
                "System.Drawing.dll" // для работы команд вывода информации в лог
            };
            definedAssemblies.Add(Assembly.GetExecutingAssembly().Location); // для взаимодействия с программой, запускающей скрипт
            foreach (string definedAssemblyPath in scriptContent.DefinedList) // дополнительные библиотеки, указанные в #define
            {
                definedAssemblies.Add(definedAssemblyPath);
            }
            return definedAssemblies.ToArray();
        }
    }
}
