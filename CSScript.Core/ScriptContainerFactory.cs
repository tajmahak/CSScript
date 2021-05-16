using System.Globalization;
using System.Reflection;

namespace CSScript.Core
{
    public static class ScriptContainerFactory
    {
        public static ScriptContainer Create(Assembly compiledAssembly, IScriptContext context) {
            ScriptContainer scriptContainer = (ScriptContainer)compiledAssembly.CreateInstance(
                Constants.CompiledScriptNamespace + "." + Constants.CompiledScriptName,
                false,
                BindingFlags.Public | BindingFlags.Instance,
                null,
                new object[] { context },
                CultureInfo.CurrentCulture,
                null);
            return scriptContainer;
        }
    }
}
