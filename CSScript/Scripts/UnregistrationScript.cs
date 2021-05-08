using CSScript.Core;
using System.IO;
using System.Reflection;

namespace CSScript.Scripts
{
    public class UnregistrationScript : ScriptContainer
    {
        public UnregistrationScript(IScriptContext context) : base(context) {
        }

        public override void Start() {
            Validate.IsTrue(RegistrationScript.HasAdministativePrivilegies(), "Для работы с реестром необходимы права администратора.");

            Context.WriteLine("Удаление регистрации программы в реестре...");
            RegistryManager.UnregisterFileAssociation();

            string shellExtensionAssemblyPath = "CSScript.ShellExtension.dll";
            if (File.Exists(shellExtensionAssemblyPath)) {
                Context.WriteLine("Удаление регистрации расширения оболочки...");
                Assembly shellExtensionAssembly = Assembly.LoadFrom(shellExtensionAssemblyPath);
                RegistryManager.UnregisterShellExtension(shellExtensionAssembly);
            }
            Context.WriteLine("Перезапуск 'Проводник'...");
            RegistrationScript.RestartWindowsExplorer();

            Context.WriteLine("Успешно", Colors.Success);
        }
    }
}
