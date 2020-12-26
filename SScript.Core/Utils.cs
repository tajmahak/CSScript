using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CSScript.Core
{
    internal static class Utils
    {
        public static string GetFilePath(string filePath, string workingDirectory)
        {
            if (Path.IsPathRooted(filePath)) {
                return filePath;
            }
            else {
                return Path.Combine(workingDirectory, filePath);
            }
        }

        public static string GetDirectory(string filePath)
        {
            string path = Path.GetFullPath(filePath);
            return Path.GetDirectoryName(path);
        }


        public static ScriptContent LoadScriptContent(string scriptPath)
        {
            string scriptText = File.ReadAllText(scriptPath, Encoding.UTF8);
            return ScriptContent.FromFile(scriptPath, scriptText);
        }

        public static bool IsWindowsAssembly(string filePath)
        {
            string extension = Path.GetExtension(filePath).ToLower();
            return extension.Equals(".exe") || extension.Equals(".dll");
        }

        public static void DeleteDuplicates<T>(List<T> list, Func<T, T, bool> comparison)
        {
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

        public static string GetNamespaceName(Type type)
        {
            string fullName = type.FullName;
            int index = fullName.LastIndexOf('.');
            return fullName.Remove(index);
        }
    }
}
