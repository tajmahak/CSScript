#if DEBUG

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;

namespace CSScript
{
    internal class DebugScript : ScriptRuntime
    {
        public override int StartScript(string arg)
        {
            string projectFolder = @"D:\Develop\VisualStudio\Projects";

            string[] directories = Directory.GetDirectories(projectFolder);
            foreach (string directory in directories)
            {
                int a = StartManaged("libs\\7z.exe", Get7ZipArgs(directory + "\\*", directory + ".7z"), outputColor: Color.Yellow, padCount: 10);
            }

            return 0;
        }












        // --- СКРИПТОВЫЕ ФУНКЦИИ (версия 1.05) ---

        // Запуск неконтролируемого процесса (при аварийном завершении работы скрипта процесс продолжит работу)
        private int Start(string program, string args = null, bool printOutput = true, Color? outputColor = null, int padCount = 0)
        {
            Process process = new Process();
            return __StartProcess(process, program, args, printOutput, outputColor, padCount);
        }

        // Запуск контролируемого процесса (при аварийном завершении работы скрипта процесс принудительно завершится) 
        private int StartManaged(string program, string args = null, bool printOutput = true, Color? outputColor = null, int padCount = 0)
        {
            Process process = CreateManagedProcess();
            return __StartProcess(process, program, args, printOutput, outputColor, padCount);
        }

        // Упаковка в архив 7-Zip
        private string Get7ZipArgs(string input, string output, int compressLevel = 9, string password = null)
        {
            StringBuilder args = new StringBuilder();
            args.Append("a"); // Добавление файлов в архив. Если архивного файла не существует, создает его
            args.Append(" \"" + output + "\"");
            args.Append(" -ssw"); // Включить файл в архив, даже если он в данный момент используется. Для резервного копирования очень полезный ключ
            args.Append(" -mx" + compressLevel); // Уровень компрессии. 0 - без компрессии (быстро), 9 - самая большая компрессия (медленно)
            if (!string.IsNullOrEmpty(password))
            {
                args.Append(" -p" + password); // Пароль для архива
                args.Append(" -mhe"); // Шифровать имена файлов
            }
            args.Append(" \"" + input + "\"");
            return args.ToString();
        }

        // Создание папки
        private bool CreateDir(string path, bool isFilePath = false)
        {
            string dirPath = isFilePath ? Path.GetDirectoryName(path) : path;
            if (!Directory.Exists(dirPath))
            {
                Directory.CreateDirectory(dirPath);
                return true;
            }
            return false;
        }

        // Добавление префикса к имени файла
        private string AddNamePrefix(string path, string prefix)
        {
            string dirPath = Path.GetDirectoryName(path);
            string fileName = Path.GetFileNameWithoutExtension(path);
            string ext = Path.GetExtension(path);
            return dirPath + "\\" + prefix + fileName + ext;
        }

        // Добавление суффикса к имени файла
        private string AddNameSuffix(string path, string suffix)
        {
            string dirPath = Path.GetDirectoryName(path);
            string fileName = Path.GetFileNameWithoutExtension(path);
            string ext = Path.GetExtension(path);
            return dirPath + "\\" + fileName + suffix + ext;
        }

        // Переименование файла (без учёта расширения)
        private string RenameFile(string path, string newName)
        {
            string dirPath = Path.GetDirectoryName(path);
            string ext = Path.GetExtension(path);
            return dirPath + "\\" + newName + ext;
        }

        // Переименование файла (с учётом расширения)
        private string RenameFileAndExtension(string path, string newName)
        {
            string dirPath = Path.GetDirectoryName(path);
            return dirPath + "\\" + newName;
        }

        // Удаление старых файлов в папке (по дате изменения)
        private void DeleteOldFiles(string path, int remainCount, string searchPattern = null)
        {
            if (string.IsNullOrEmpty(searchPattern))
            {
                searchPattern = "*";
            }
            string[] files = Directory.GetFiles(path, searchPattern, SearchOption.TopDirectoryOnly);
            string[] oldFiles = GetOldFiles(files, remainCount);
            foreach (string file in oldFiles)
            {
                try
                {
                    File.Delete(file);
                }
                catch
                {
                    string fileName = Path.GetFileName(file);
                    WriteLineLog("Не удалось удалить файл '" + fileName + "'", ScriptRuntime.Settings.ErrorColor);
                }
            }
        }

        // Получение списка старых файлов в папке (по дате изменения)
        private string[] GetOldFiles(string[] files, int remainCount)
        {
            List<KeyValuePair<string, DateTime>> fileList = new List<KeyValuePair<string, DateTime>>();
            foreach (string file in files)
            {
                DateTime fileDate = new FileInfo(file).LastWriteTime;
                KeyValuePair<string, DateTime> fileItem = new KeyValuePair<string, DateTime>(file, fileDate);
                fileList.Add(fileItem);
            }
            fileList.Sort((x, y) => y.Value.CompareTo(x.Value));
            fileList.RemoveRange(0, remainCount);
            string[] newFileList = new string[fileList.Count];
            for (int i = 0; i < fileList.Count; i++)
            {
                newFileList[i] = fileList[i].Key;
            }
            return newFileList;
        }

        private int __StartProcess(Process process, string program, string args, bool printOutput, Color? outputColor, int padCount)
        {
            if (!File.Exists(program))
            {
                throw new Exception("Не удаётся найти '" + program + "'");
            }
            using (process)
            {
                process.StartInfo = new ProcessStartInfo()
                {
                    FileName = program,
                    Arguments = args,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true,
                };
                process.OutputDataReceived += (sender, e) =>
                {
                    if (printOutput)
                    {
                        WriteLineLog(new string(' ', padCount) + e.Data, outputColor);
                    }
                };
                process.Start();
                process.BeginOutputReadLine();
                process.WaitForExit();
                return process.ExitCode;
            }
        }



    }
}

#endif
