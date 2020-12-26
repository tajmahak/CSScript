using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace CSScript
{
    /// <summary>
    /// Представляет средство для работы с динамически подгружаемыми сборками.
    /// </summary>
    public class AssemblyManager
    {
        private readonly Dictionary<string, Assembly> resolvedAssemblies = new Dictionary<string, Assembly>();

        public void LoadAssembliesForResolve(ScriptInfo scriptInfo)
        {
            string[] assemblies = GetReferencedAssemblies(scriptInfo, false);
            foreach (string assembly in assemblies) {
                if (File.Exists(assembly)) {
                    // загружаются только те сборки, которые рантайм не может подгрузить автоматически
                    Assembly loadedAssembly = Assembly.LoadFrom(assembly);
                    resolvedAssemblies.Add(loadedAssembly.FullName, loadedAssembly);
                }
            }
        }

        public Assembly ResolveAssembly(string assemblyName)
        {
            resolvedAssemblies.TryGetValue(assemblyName, out Assembly assembly);
            return assembly;
        }

        public string[] GetReferencedAssemblies(ScriptInfo scriptInfo, bool includeCurrentAssembly)
        {
            List<string> definedAssemblies = new List<string> {
                "System.dll", // библиотека для работы множества основных функций
                "System.Drawing.dll" // для работы команд вывода информации в лог
            };
            if (includeCurrentAssembly) {
                definedAssemblies.Add(Assembly.GetExecutingAssembly().Location); // для взаимодействия с программой, запускающей скрипт
            }
            foreach (string definedAssemblyPath in scriptInfo.DefinedAssemblyList) // дополнительные библиотеки, указанные в #define
            {
                definedAssemblies.Add(definedAssemblyPath);
            }
            return definedAssemblies.ToArray();
        }
    }
}
