using System;
using System.IO;
using System.Reflection;

namespace CSScript
{
    internal static class PathManager
    {
        public static string FromLocalDirectoryPath(string fileName)
        {
            // Получение пути из папки с исполняемой сборкой
            string executingPath = Assembly.GetExecutingAssembly().Location;
            string path = Path.GetDirectoryName(executingPath);
            return Path.Combine(path, fileName);
        }

        public static string GetWorkDirectoryPath(string path)
        {
            return Path.GetDirectoryName(path);
        }

        public static string GetAndCheckFullPath(string filePath, string directoryPath)
        {
            if (string.IsNullOrEmpty(filePath)) {
                throw new Exception("Отсутствует путь к файлу.");
            }

            if (!Path.IsPathRooted(filePath)) {
                filePath = Path.Combine(directoryPath, filePath);
            }

            if (!File.Exists(filePath)) {
                throw new Exception($"Файл '{filePath}' не найден.");
            }

            return filePath;
        }

        public static string GetAssemblyPath(string assemblyPath, string directoryPath)
        {
            if (!Path.IsPathRooted(assemblyPath)) {
                // библиотека, указанная по относительному пути, находится либо в рабочей папке, либо в GAC
                string fullPath = Path.Combine(directoryPath, assemblyPath);
                if (File.Exists(fullPath)) {
                    return fullPath;
                }
            }
            return assemblyPath;
        }
    }
}
