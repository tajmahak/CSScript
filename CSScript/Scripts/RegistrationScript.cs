using CSScript.Core;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security.Principal;

namespace CSScript.Scripts
{
    public class RegistrationScript : ScriptContainer
    {
        public RegistrationScript(IScriptContext context) : base(context) {
        }

        public override void Start() {
            Validate.IsTrue(HasAdministativePrivilegies(), "Для работы с реестром необходимы права администратора.");

            Context.WriteLine("Регистрация программы в реестре...");
            RegistryManager.RegisterFileAssociation();

            string executingPath = Assembly.GetExecutingAssembly().Location;
            string path = Path.GetDirectoryName(executingPath);
            string shellExtensionAssemblyPath = Path.Combine(path, "CSScript.ShellExtension.dll");

            if (File.Exists(shellExtensionAssemblyPath)) {
                Context.WriteLine("Регистрация расширения оболочки...");
                Assembly shellExtensionAssembly = Assembly.LoadFrom(shellExtensionAssemblyPath);
                RegistryManager.RegisterShellExtension(shellExtensionAssembly);
            }
            Context.WriteLine("Перезапуск 'Проводник'...");
            RestartWindowsExplorer();

            Context.WriteLine("Успешно", Colors.Success);
        }

        public static bool HasAdministativePrivilegies() {
            bool isElevated;
            using (WindowsIdentity identity = WindowsIdentity.GetCurrent()) {
                WindowsPrincipal principal = new WindowsPrincipal(identity);
                isElevated = principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            return isElevated;
        }

        public static void RestartWindowsExplorer() {
            Process[] explorer = Process.GetProcessesByName("explorer");
            foreach (Process process in explorer) {
                process.Kill();
            }
            // Process.Start("explorer.exe"); - запускается автоматически
        }
    }
}
