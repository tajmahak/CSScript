using Microsoft.CSharp;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace CSScript.Core.Manage
{
    public class ScriptBuilder
    {
        public StringBuilder InitBlock { get; private set; } = new StringBuilder();

        public StringBuilder ProcedureBlock { get; private set; } = new StringBuilder();

        public StringBuilder ClassBlock { get; private set; } = new StringBuilder();

        public StringBuilder NamespaceBlock { get; private set; } = new StringBuilder();

        public StringBuilder SpaceBlock { get; private set; } = new StringBuilder();

        private readonly Type scriptContainerType = typeof(ScriptContainer);

        private readonly HashSet<string> assemblyList = new HashSet<string>();

        private readonly HashSet<string> usingList = new HashSet<string>();


        public void AddAssembly(string assembly) {
            assemblyList.Add(assembly);
        }

        public void AddUsing(string usingItem) {
            usingList.Add(usingItem);
        }

        public string GetSourceCode() {
            StringBuilder srcCode = new StringBuilder();
            foreach (string usingItem in usingList) {
                srcCode.AppendLine("using " + usingItem + ";");
            }
            srcCode.AppendLine();
            srcCode.AppendLine("namespace " + Constants.CompiledScriptNamespace + " {");
            srcCode.AppendLine();
            srcCode.AppendLine("public class " + Constants.CompiledScriptName + " : " + scriptContainerType.FullName + " {");

            // Создание конструктора
            ConstructorInfo[] consructors = scriptContainerType.GetConstructors();
            Validate.IsTrue(consructors.Length == 1);
            srcCode.AppendLine();
            srcCode.Append("public " + Constants.CompiledScriptName + "(");
            ConstructorInfo constructor = consructors[0];
            ParameterInfo[] constructorParams = constructor.GetParameters();

            List<string> stringList = new List<string>(constructorParams.Length);
            foreach (ParameterInfo constructorParam in constructorParams) {
                stringList.Add(constructorParam.ParameterType.FullName + " " + constructorParam.Name);
            }
            srcCode.Append(string.Join(", ", stringList));

            stringList.Clear();
            foreach (ParameterInfo constructorParam in constructorParams) {
                stringList.Add(constructorParam.Name);
            }
            srcCode.Append(") : base(" + string.Join(", ", stringList) + ") { }");
            srcCode.AppendLine();

            // Переопределение метода
            MethodInfo[] abstractMethods = scriptContainerType.GetMethods()
                .Where(x => x.IsAbstract && x.ReturnType == typeof(void))
                .ToArray();
            Validate.IsTrue(abstractMethods.Length == 1);
            MethodInfo abstractMethod = abstractMethods[0];
            srcCode.AppendLine();
            srcCode.Append("public override void " + abstractMethod.Name + "(");

            ParameterInfo[] abstractMethodParams = abstractMethod.GetParameters();
            stringList.Clear();
            foreach (ParameterInfo abstractMethodParam in abstractMethodParams) {
                stringList.Add(abstractMethodParam.ParameterType.FullName + " " + abstractMethodParam.Name);
            }
            srcCode.Append(string.Join(", ", stringList) + ") {");
            srcCode.AppendLine();
            srcCode.AppendLine("// === init block ===");
            srcCode.AppendLine(InitBlock.ToString().Trim());
            srcCode.AppendLine("// === procedure block ===");
            srcCode.AppendLine(ProcedureBlock.ToString().Trim());
            srcCode.AppendLine("}");

            srcCode.AppendLine();
            srcCode.AppendLine("// === class block ===");
            srcCode.AppendLine(ClassBlock.ToString().Trim());
            srcCode.AppendLine("}");

            srcCode.AppendLine();
            srcCode.AppendLine("// === namespace block ===");
            srcCode.AppendLine(NamespaceBlock.ToString().Trim());
            srcCode.AppendLine("}");

            srcCode.AppendLine();
            srcCode.AppendLine("// === space block ===");
            srcCode.AppendLine(SpaceBlock.ToString().Trim());

            return srcCode.ToString();
        }

        public CompilerParameters CreateCompilerParameters() {
            List<string> assemblies = new List<string>(assemblyList) {
                scriptContainerType.Module.FullyQualifiedName
            };
            CompilerParameters compilerParameters = new CompilerParameters(assemblies.ToArray()) {
                GenerateInMemory = true,
                GenerateExecutable = false,
                CompilerOptions = "/optimize",
            };
            return compilerParameters;
        }

        public CompilerResults Compile() {
            return Compile(CreateCompilerParameters());
        }

        public CompilerResults Compile(CompilerParameters compilerParameters) {
            // Компиляция C# 6 с помощью Roslyn на Net Framework 4.5
            // https://stackoverflow.com/questions/31639602/using-c-sharp-6-features-with-codedomprovider-roslyn
            // https://www.nuget.org/packages/Microsoft.CodeDom.Providers.DotNetCompilerPlatform/
            // CodeDomProvider objCodeCompiler = new Microsoft.CodeDom.Providers.DotNetCompilerPlatform.CSharpCodeProvider();

            string sourceCode = GetSourceCode();
            using (CSharpCodeProvider provider = new CSharpCodeProvider()) {
                return provider.CompileAssemblyFromSource(compilerParameters, sourceCode);
            }
        }
    }
}
