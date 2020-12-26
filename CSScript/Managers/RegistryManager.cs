using Microsoft.Win32;
using System;
using System.Reflection;
using System.Runtime.InteropServices;

namespace CSScript
{
    public static class RegistryManager
    {
        public static void RegisterFileAssociation()
        {
            RegisterFileAssociation(Registry.ClassesRoot);
            RegisterFileAssociation(Registry.LocalMachine.OpenSubKey("SOFTWARE\\Classes", true));
        }

        public static void UnregisterFileAssociation()
        {
            UnregisterFileAssociation(Registry.ClassesRoot);
            UnregisterFileAssociation(Registry.LocalMachine.OpenSubKey("SOFTWARE\\Classes", true));
        }

        public static void RegisterShellExtension(Assembly extAssembly)
        {
            foreach (Type type in extAssembly.GetTypes()) {
                object[] attributes = type.GetCustomAttributes(typeof(ComVisibleAttribute), false);
                if (attributes.Length == 0) {
                    continue;
                }
                if (type.Name.Contains("DropHandler")) {
                    InstallDropHandlerServer(Registry.ClassesRoot.OpenSubKey("CLSID", true), type);
                    InstallDropHandlerServer(Registry.LocalMachine.OpenSubKey("SOFTWARE\\Classes\\CLSID", true), type);

                    Assembly executingAssembly = Assembly.GetExecutingAssembly();
                    string assemblyName = executingAssembly.GetName(false).Name;

                    RegistryKey key = Registry.ClassesRoot.CreateSubKey(assemblyName + "\\shellex");
                    RegisterDropHandlerServer(key, type);

                    key = Registry.LocalMachine.CreateSubKey("SOFTWARE\\Classes\\" + assemblyName + "\\shellex");
                    RegisterDropHandlerServer(key, type);
                }
            }
        }

        public static void UnregisterShellExtension(Assembly extAssembly)
        {
            foreach (Type type in extAssembly.GetTypes()) {
                object[] attributes = type.GetCustomAttributes(typeof(ComVisibleAttribute), false);
                if (attributes.Length == 0) {
                    continue;
                }
                if (type.Name.Contains("DropHandler")) {
                    UninstallDropHandlerServer(Registry.ClassesRoot.OpenSubKey("CLSID", true), type);
                    UninstallDropHandlerServer(Registry.LocalMachine.OpenSubKey("SOFTWARE\\Classes\\CLSID", true), type);
                }
            }
        }


        private static void RegisterFileAssociation(RegistryKey parentKey)
        {
            Assembly executingAssembly = Assembly.GetExecutingAssembly();
            string assemblyName = executingAssembly.GetName().Name;

            RegistryKey key = parentKey.CreateSubKey(".cssc");
            key.SetValue(string.Empty, assemblyName);

            key = parentKey.CreateSubKey(assemblyName + "\\DefaultIcon");
            key.SetValue(string.Empty, executingAssembly.Location);

            key = parentKey.CreateSubKey(assemblyName + "\\shell\\open\\command");
            key.SetValue(string.Empty, $"\"{executingAssembly.Location}\" \"%1\" /a %*");
        }

        private static void UnregisterFileAssociation(RegistryKey parentKey)
        {
            Assembly executingAssembly = Assembly.GetExecutingAssembly();
            string assemblyName = executingAssembly.GetName().Name;

            parentKey.DeleteSubKeyTree(".cssc", false);
            parentKey.DeleteSubKeyTree(assemblyName, false);
        }

        private static void InstallDropHandlerServer(RegistryKey parentKey, Type dropHandlerType)
        {
            RegistryKey key = parentKey.CreateSubKey($"{{{dropHandlerType.GUID}}}");
            key.SetValue(string.Empty, dropHandlerType.Name);

            key = parentKey.CreateSubKey($"{{{dropHandlerType.GUID}}}\\InprocServer32");
            key.SetValue(string.Empty, "mscoree.dll");
            key.SetValue("Assembly", dropHandlerType.Assembly.FullName);
            key.SetValue("Class", dropHandlerType.FullName);
            key.SetValue("RuntimeVersion", dropHandlerType.Assembly.ImageRuntimeVersion);
            key.SetValue("ThreadingModel", "Both");
            key.SetValue("CodeBase", "file:///" + dropHandlerType.Assembly.Location.Replace("\\", "/"));

            Version version = Assembly.GetExecutingAssembly().GetName().Version;
            key = parentKey.CreateSubKey($"{{{dropHandlerType.GUID}}}\\InprocServer32\\" + version);
            key.SetValue("Assembly", dropHandlerType.Assembly.FullName);
            key.SetValue("Class", dropHandlerType.FullName);
            key.SetValue("RuntimeVersion", dropHandlerType.Assembly.ImageRuntimeVersion);
            key.SetValue("CodeBase", "file:///" + dropHandlerType.Assembly.Location.Replace("\\", "/"));
        }

        private static void UninstallDropHandlerServer(RegistryKey parentKey, Type dropHandlerType)
        {
            parentKey.DeleteSubKeyTree($"{{{dropHandlerType.GUID}}}", false);
        }

        private static void RegisterDropHandlerServer(RegistryKey parentKey, Type dropHandlerType)
        {
            RegistryKey key = parentKey.CreateSubKey("DropHandler");
            key.SetValue(string.Empty, $"{{{dropHandlerType.GUID}}}");
        }
    }
}
