using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CSScript.Core
{
    public static class Utils
    {
        public static string GetFilePath(string filePath, string workingDirectory) {
            return Path.IsPathRooted(filePath) ? filePath : Path.Combine(workingDirectory, filePath);
        }

        public static string GetDirectoryName(string filePath) {
            string path = Path.GetFullPath(filePath);
            return Path.GetDirectoryName(path);
        }


        internal static ScriptInfo LoadScriptInfo(string scriptPath) {
            string scriptText = File.ReadAllText(scriptPath, Encoding.UTF8);
            return ScriptInfo.FromFile(scriptPath, scriptText);
        }

        internal static bool IsWindowsAssembly(string filePath) {
            string extension = Path.GetExtension(filePath).ToLower();
            return extension.Equals(".exe") || extension.Equals(".dll");
        }

        internal static void DeleteDuplicates<T>(List<T> list, Func<T, T, bool> comparison) {
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
    }
}
