using Microsoft.CSharp;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;

namespace CSScript.Core
{
    public class ScriptHandler
    {
        private readonly ProcessManager processManager;
        public MessageManager Messages { get; private set; }


        public ScriptHandler()
        {
            processManager = new ProcessManager();
            Messages = new MessageManager();
        }


        public delegate void ScriptFinishedEventHandler(ScriptContainer scriptContainer, bool success);
        public event ScriptFinishedEventHandler ScriptFinished;


        public IScriptEnvironment CreateScriptEnvironment(string scriptPath, string[] scriptArgs)
        {
            return new ScriptEnvironment(this, scriptPath, scriptArgs);
        }


        public CompilerResults CompileScript(string scriptPath)
        {
            ScriptContent scriptContent = LoadScriptContent(scriptPath);
            string sourceCode = CreateSourceCode(scriptContent);

            string[] referencedAssemblies = assemblyManager.GetReferencedAssemblies(scriptInfo, true);

            using (CSharpCodeProvider provider = new CSharpCodeProvider()) {
                CompilerParameters compileParameters = new CompilerParameters(referencedAssemblies) {
                    GenerateInMemory = true,
                    GenerateExecutable = false,
                };
                return provider.CompileAssemblyFromSource(compileParameters, sourceCode);
            }
        }




        private ScriptContent LoadScriptContent(string scriptPath)
        {
            ScriptContent mainScript = new ScriptContent(scriptPath);
            AppendScript(mainScript, scriptPath, 0);

            Utils.DeleteDuplicates(mainScript.DefinedScriptList, (a, b) => a.Equals(b));
            Utils.DeleteDuplicates(mainScript.UsingList, (a, b) => a.Equals(b));

            return mainScript;
        }

        private void AppendScript(ScriptContent mainScript, string scriptPath, int level)
        {
            string workingDirectory = Utils.GetDirectory(scriptPath);

            ScriptContent script = Utils.LoadScriptContent(scriptPath);
            foreach (string defineItem in script.DefinedScriptList) {
                string defineFilePath = Utils.GetFilePath(defineItem, workingDirectory);
                if (Utils.IsWindowsAssembly(defineFilePath)) {
                    mainScript.DefinedScriptList.Add(defineFilePath);
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

        private string CreateSourceCode(ScriptContent scriptContent)
        {
            string compiledScriptName = "CompiledScriptContainer";
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







        public void Execute(ScriptContainer scriptContainer, bool throwException)
        {
            try {
                scriptContainer.Execute();
            }

            catch (Exception ex) {
                if (throwException) {
                    throw ex;
                }
                else {
                    scriptContainer.env.ExitCode = 1;
                    Messages.WriteLine();
                    Messages.WriteException(ex);
                    ScriptFinished?.Invoke(scriptContainer, false);
                }
            }

            finally {
                Messages.WriteLine();
                Messages.WriteExitCode(scriptContainer.env.ExitCode);
                ScriptFinished?.Invoke(scriptContainer, true);
            }
        }


        internal Process CreateManagedProcess()
        {
            return processManager.CreateManagedProcess();
        }

        internal string GetInputText(string caption)
        {
            return null;
        }


    }
}
